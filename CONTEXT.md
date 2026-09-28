# Glossary

**Ticket** — a support request raised by a customer, worked by an admin.

**Status** — a ticket's four values run `New` → `In Progress` → `Resolved` → `Closed`.
`Resolved` means the fix is done and the resolution recorded; `Closed` means no further
work will happen, fixed or not.

**Resolution** — the admin's written account of how the ticket was settled, and the
thing the customer is emailed about when it changes.

**Image** — the one photo or screenshot a customer may attach while opening a ticket,
held on the ticket as an `imageUrl` relative to the API host.

**Summary** — a restatement of the description, generated rather than written by a
person.

**Admin** — a signed-in support staff member, and the only role meant to change status
or resolution. Enforced in the UI only; `PUT /api/tickets/{id}` accepts anyone.
