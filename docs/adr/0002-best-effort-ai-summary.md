# ADR-0002: Best-effort AI summaries, with a null implementation instead of a feature flag

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

`ISummaryService` is asked for a summary during `TicketService.CreateAsync`, and the
result is persisted with the ticket. `GeminiSummaryService` calls the Gemini
`generateContent` endpoint through an `HttpClient` supplied by `IHttpClientFactory`
(`AddHttpClient<ISummaryService, GeminiSummaryService>`), which owns the key header,
base address and timeout. The key comes from `Summary:ApiKey`, set through
user-secrets.

### Why best-effort rather than failing the create

The summary's value to the customer is zero: they never see it, and the description
they wrote is stored regardless. A provider outage, a rate limit or a timeout would
otherwise turn a working ticket form into a broken one for a field nobody asked for.
`TicketService` catches every exception from the summary call, logs a warning, and
creates the ticket with an empty summary — the same state every ticket had before
this feature. The tests assert exactly that: a throwing provider still yields a 201.

"Fails" includes returning something unusable: a response without the expected
`candidates` shape throws while being read, and an answer longer than twice
`Summary:MaxWords` is rejected rather than stored, because the ticket table renders
the summary in one line. Both land in the same catch as a network failure.

The cost of this choice is that a silently misconfigured key looks like a working
application with no summaries. The warning log is the only signal, which is the
right trade for a bonus feature but would not be for a required one.

Cancellation is the one exception that is not swallowed: if the client disconnects
mid-create, `OperationCanceledException` is rethrown rather than logged as a
provider failure.

### Why a null implementation rather than a feature flag

Both make the feature optional. The difference is where the optionality lives:

- A flag puts an `if` at the call site in `TicketService`, which then has two code
  paths, and only one of them is exercised on a machine without a key.
- A null implementation puts it at the composition root: `Program.cs` registers
  `NullSummaryService` when `Summary:ApiKey` is empty. `TicketService` has one
  unconditional path, tested once, whichever way the application is configured.

The key's presence *is* the flag, so a separate `Summary:Enabled` would be a second
switch that can disagree with the first — configured key, feature off, no
explanation. Registering by key presence makes that state unrepresentable.

### Why the summary is generated inline rather than in the background

Inline is one call in a method that already awaits a store write and an email. A
background job would need a queue, a worker, and a way for the frontend to learn the
summary arrived — for a field the UI already treats as optional. The timeout
(`Summary:TimeoutSeconds`, default 10s) bounds the cost to create. If that latency
ever matters, the seam to move behind is `ISummaryService`, unchanged.

## Consequences

- `POST /api/tickets` is as slow as the provider, up to the configured timeout.
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
