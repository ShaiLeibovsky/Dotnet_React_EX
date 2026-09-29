# ADR-0005: Customer notifications are sent after the response, not before it

- Status: accepted
- Date: 2026-09-29

## Context

ADR-0003 section 5 moved summary generation off `POST /api/tickets`, leaving the create
path with one remaining outbound call the customer waits for: the "we got your ticket"
notification. `PUT /api/tickets/{id}` waits for up to two more. With `EmailNotifier`
registered, each is an SMTP conversation -- connect, STARTTLS, auth, send -- against a
third-party mailbox, inside the request.

The asymmetry was the trigger: the same create request already refuses to wait for the
summary, whose failure is silently tolerated, but waited for a notification whose failure
is silently tolerated too. `EmailNotifier.SendAsync` catches every exception and logs it,
so no response ever depended on a send succeeding -- only on it finishing.

## Decision

### 1. Notifying after the response

`TicketService` no longer holds an `ICustomerNotifier`. It puts what it owes the customer
on `NotificationQueue` and answers; `NotificationDelivery`, a `BackgroundService`, drains
that queue and calls the notifier. `CreateAsync` queues one notification, `UpdateAsync`
queues one per field that actually changed, as before.

`NotificationQueue` mirrors `ICustomerNotifier` one enqueue per send, rather than exposing
one `Enqueue` taking a prepared call. The call sites then read as what they notify about --
`EnqueueStatusChanged(updated, previousStatus, handlingAdminEmail)` -- and the arguments a
notification needs stay checked by the compiler.

The queued item holds the `Ticket` alongside the call, so a failed send can be logged
against the ticket it was about. It holds the ticket itself rather than an id, unlike
`SummaryQueue` -- ADR-0003 section 5. That queue holds ids because its worker writes the
ticket back and must not work from a stale copy. This worker only reads, and what it reads
is the ticket as the request left it, which is what the notification describes.

### 2. A queue and a worker, not a detached task

`Task.Run` without an await would have been three words of change per call site, and both
notifier implementations already swallow and log their own failures, so no response ever
depended on the wait. It was rejected on three counts the worker fixes:

- A detached task is unbounded. A burst of creates is a burst of simultaneous SMTP
  conversations against the same mailbox, with nothing to bound them.
- A detached body that throws *outside* the notifier's own `try` -- `EmailNotifier` builds
  its `CustomerNotification`, and so computes the tracking link, before `SendAsync` begins
  -- raises an unobserved task exception, which .NET swallows without logging. The worker
  catches around the whole send, so a misconfigured link surfaces as a logged error instead
  of silence.
- Detached sends have no order. A save that changes status *and* resolution owes two
  notifications about one ticket, and the customer should read them in the order the admin
  made the changes. The worker sends one at a time, in queue order, which is the order
  `UpdateAsync` enqueued them.

Sending one at a time is also the ceiling: a burst of creates is delivered serially, so the
last customer in the burst waits for every send before theirs. Raising it means concurrent
sends partitioned by ticket, to keep the ordering above; until the delay is measured against
a real mailbox, one at a time is the smaller thing that is correct.

Reusing `SummaryQueue` rather than adding a second queue was also rejected: it is a single
reader too, and one item there can take the full 25 second retry budget of ADR-0003 section
4. A notification behind two slow summaries would arrive a minute late, trading the latency
off the customer's request straight onto their inbox.

### 3. What the queue still gives up

The queue is in-memory. A restart loses every notification still waiting, and there is no
record that one was owed, no retry, and no signal to the customer beyond the logged error.
This is the same ceiling ADR-0003 section 5 accepted for summaries, applied to a
notification, and it is the one place the two differ in cost: an unsent summary is
invisible, an unsent notification is a customer who believes their ticket vanished.

The upgrade path, if that matters, is an outbox: the notification is written in the same
transaction as the ticket and delivered by a worker that marks it sent. That needs a table,
a claim protocol and a retry count, which is the work this ADR is deferring rather than the
work it is avoiding.

## Consequences

- `POST /api/tickets` and `PUT /api/tickets/{id}` return at store speed. No outbound call is
  left on either.
- Notifications are sent outside any request scope, so `ICustomerNotifier` and everything it
  resolves must stay singleton-safe. Neither implementation touches a scoped service today;
  one that did would need `IServiceScopeFactory`, as `SummaryBackfill` uses.
- A test that acts and then asserts on a notification must wait for the send.
  `RecordingNotifier.SentAsync(count)` is that wait; the SMTP tests already polled their fake
  server with a deadline and needed no change.
- A notification can now be delivered after the response reporting the change it describes,
  so a customer can, in principle, open the tracking link before the mail carrying it
  arrives.
- A shutdown drops every notification still queued. The send already running finishes,
  because it is not bound to the host token.
