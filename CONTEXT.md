# Glossary

**Ticket** — a support request raised by a customer, worked by an admin.

**Status** — a ticket's four values run `New` → `In Progress` → `Resolved` → `Closed`.
`Resolved` means the fix is done and the resolution recorded; `Closed` means no further
work will happen, fixed or not.

**Resolution** — the admin's written account of how the ticket was settled, and the
thing the customer is emailed about when it changes.

**Summary** — a restatement of the description, generated rather than written by a
person.

**Admin** — a signed-in support staff member, and the only actor allowed to change
status or resolution. Enforced by the server: `PUT /api/tickets/{id}` requires a valid
token, while listing, reading and creating stay open to anyone. Every account in the
users table is an admin, so there is no role — see ADR-0002 section 1, no role column.

**Session** — an admin's signed token plus the email it was issued to, held in browser
local storage and valid for eight hours.

**Module** — a backend folder under `Modules/` that owns one feature end to end: its
endpoints, service and interface at the root, with `Dto/`, `Entities/` and the rest one
level down, and the namespace matching the folder. `Tickets`, `Auth`, `Summaries` and
`Notifications` are the four — see ADR-0004 section 1, modules under a Modules root.
