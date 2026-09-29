# ADR-0003: Best-effort AI summaries, with a null implementation instead of a feature flag

- Status: accepted
- Date: 2026-09-27
- Issue: [#9](https://github.com/ShaiLeibovsky/Dotnet_React_EX/issues/9)

## Context

`Ticket` already carried a `Summary` the frontend renders conditionally, and nothing
filled it. Filling it means an outbound call to a third-party AI provider during
`POST /api/tickets` — a network hop, on a paid API, behind a credential a reviewer
cloning this repo does not have.

Ticket creation is the one operation a customer cannot retry usefully: they have
already written the description. A summary is a triage convenience for support.

## Decision

`ISummaryService` is asked for a summary after `POST /api/tickets` has answered, and
the result is written back to the stored ticket — section 5. `GeminiSummaryService`
calls the Gemini
`generateContent` endpoint through an `HttpClient` supplied by `IHttpClientFactory`
(`AddHttpClient<ISummaryService, GeminiSummaryService>`), which owns the key header,
base address and timeout. The key comes from `Summary:ApiKey`, set through
user-secrets.

### 1. Why best-effort rather than failing the create

The summary's value to the customer is zero: they never see it, and the description
they wrote is stored regardless. A provider outage, a rate limit or a timeout would
otherwise turn a working ticket form into a broken one for a field nobody asked for.
`SummaryBackfill` catches every exception from the summary call, logs a warning, and
leaves the ticket with the empty summary it was created with — the same state every
ticket had before this feature. The tests assert exactly that: a throwing provider
still yields a 201 and a blank summary.

"Fails" includes returning something unusable: a response without the expected
`candidates` shape throws while being read, and an answer longer than twice
`Summary:MaxWords` is rejected rather than stored, because the ticket table renders
the summary in one line. Both land in the same catch as a network failure.

The cost of this choice is that a silently misconfigured key looks like a working
application with no summaries. The warning log is the only signal, which is the
right trade for a bonus feature but would not be for a required one.

Cancellation is the one exception that is not swallowed: on host shutdown,
`OperationCanceledException` is rethrown rather than logged as a provider failure, so
the worker stops instead of draining the queue against a closing application.

### 2. Why a null implementation rather than a feature flag

Both make the feature optional. The difference is where the optionality lives:

- A flag puts an `if` at the call site in `TicketService`, which then has two code
  paths, and only one of them is exercised on a machine without a key.
- A null implementation puts it at the composition root: `Program.cs` registers
  `NullSummaryService` when `Summary:ApiKey` is empty. `TicketService` has one
  unconditional path, tested once, whichever way the application is configured.

The key's presence *is* the flag, so a separate `Summary:Enabled` would be a second
switch that can disagree with the first — configured key, feature off, no
explanation. Registering by key presence makes that state unrepresentable.

### 3. Why the summary is generated inline rather than in the background (superseded)

Superseded by section 5. The latency this section called acceptable stopped being
acceptable once a retried provider could hold the create open for 25 seconds.

Inline is one call in a method that already awaits a store write and an email. A
background job would need a queue, a worker, and a way for the frontend to learn the
summary arrived — for a field the UI already treats as optional. The timeout
(`Summary:TimeoutSeconds`, default 25s) bounds the cost to create. If that latency
ever matters, the seam to move behind is `ISummaryService`, unchanged.

### 4. Retrying an overloaded provider

The free tier sheds load with `503 Service Unavailable`, and the first key used against
this code hit it on consecutive ticket creates. A 503 is retryable, so the client gets
`AddStandardResilienceHandler`: retry, circuit breaker and the two timeouts, rather
than a hand-rolled loop.

The two timeouts split what `Summary:TimeoutSeconds` used to mean alone.
`Summary:AttemptTimeoutSeconds` (10s) bounds one call to Gemini, which is the number
that tracks how slow the provider is — observed responses run 5-7s.
`Summary:TimeoutSeconds` (25s) bounds the retries together. Since section 5 it is
spent by the backfill rather than by the customer's request.

`HttpClient.Timeout` is set to infinite because it would otherwise cancel the whole
pipeline mid-retry, and its budget cannot be expressed per attempt. The resilience
handler owns cancellation instead. `CircuitBreaker.SamplingDuration` is pinned to twice
the attempt timeout because the library rejects a sampling window shorter than that.

### 5. Summarising after the response

`POST /api/tickets` no longer waits for a summary. It stores the ticket, notifies the
customer -- without waiting for that either, since ADR-0005 section 1 -- puts the
ticket id on `SummaryQueue`, and answers. `SummaryBackfill`, a
`BackgroundService`, drains that queue, asks `ISummaryService`, and writes the summary
back through `ITicketStore.UpdateAsync`. A ticket whose summary fails keeps the empty
one it was created with, so the failure handling of section 1 is unchanged — only its
location moved.

The queue holds ids rather than descriptions so the backfill reads the ticket it is
about to update, and there is no second copy of the description to go stale.

The queue is in-memory and unbounded. Unbounded is safe because exactly one id is
enqueued per created ticket, by the request that created it. In-memory means a restart
loses every id still waiting, and those tickets keep an empty summary forever — there
is no sweep for them. That is the deliberate ceiling: a durable queue would need a
table, a claim protocol and a retry count, for a field that is already optional.

### 6. Waiting for the summary in the browser

Section 5 left the create response with no summary, so the detail page the customer
lands on after creating a ticket rendered without one and never changed. React Router
already owns the fetch through `ticketLoader`, so `useRevalidateUntilSummary` calls
`useRevalidator` on a 2 second interval rather than adding a second fetch path or a
data-fetching dependency.

The poll only runs while a summary is still expected: the ticket has none *and* it was
created less than 60 seconds ago. Both conditions matter. Without the first, every page
view polls forever; without the second, an old ticket whose summary failed — or every
ticket on an instance with no API key — polls forever too. 60s covers the whole retry
budget of section 4 with room to spare, and the cost of being wrong is one missing
summary until the customer reloads.

The list page does not poll. A summary that appears there does so on the next visit,
which is the normal loader fetch.

## Consequences

- `POST /api/tickets` returns at store-and-notify speed. Every ticket is created with
  an empty summary, and the summary appears on a later read, typically seconds later.
  A client that renders the create response alone will never show a summary.
- A summary arriving bumps the ticket's `UpdatedAt`, because `ITicketStore.UpdateAsync`
  timestamps every write. To a reader, a ticket nobody touched looks recently updated.
- A restart drops queued ids, so tickets created moments before it keep an empty
  summary permanently.
- Summaries are generated once, on create. Editing a description is not possible
  through the API, so there is no staleness to handle yet; a description edit would
  need to re-summarise.
- `TicketApiFactory` pins `Summary:ApiKey` to empty, so a developer's real key in
  user-secrets cannot leak into the test suite and make it call the provider. Tests
  that need the Gemini path pass a stub `HttpMessageHandler` instead; the factory
  then sets a fake key and replaces the primary handler on the client the factory
  hands out, so the whole registration, prompt and parsing path runs at the HTTP
  seam with no network call and no unit test below it.
- The prompt and response parsing are Gemini-shaped. A second provider means a
  second `ISummaryService`, not a change to this one.
- `Summary:ApiKey` doubles as the on/off switch, so removing it from user-secrets is
  how the feature is turned off.
