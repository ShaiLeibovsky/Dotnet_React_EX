# ADR-0004: Backend folders group by feature, not by kind

- Status: accepted
- Date: 2026-09-29

## Context

The backend was laid out by kind: `Services/` held 19 files, with `Dtos/`,
`Entities/` and `Endpoints/` beside it. Finding the implementations of an
interface meant scanning one flat alphabetical list, where `ITicketStore` sat
between `ITicketService` and `JsonTicketStore` by accident of spelling rather
than by relation. Reading one feature end to end meant opening four folders.

## Decision

Backend source is grouped by feature at the project root: `Tickets/`, `Auth/`,
`Summaries/`, `Notifications/`. A feature folder holds everything that feature
owns — entity, DTOs, endpoints, service interface and implementation, and its
options class. Namespaces mirror the folders (`TicketApi.Tickets`,
`TicketApi.Tickets.Stores`). The test project mirrors the same folders.

### 1. Feature folders at the root, not inside `Services/`

Nesting feature folders under `Services/` would leave `Dtos/`, `Entities/` and
`Endpoints/` split by kind one level up, so a feature would still be spread
across four places. The kind-folders are deleted rather than pushed down.

### 2. An interface lives beside its implementations, one level deep

`Tickets/Stores/` holds `ITicketStore` next to `JsonTicketStore` and
`SqliteTicketStore`. A folder per implementation was considered and rejected:
each would hold a single file, adding depth without grouping anything. A
subfolder is earned when an implementation grows past one file.

### 3. Options classes sit with what they configure

`AuthOptions` in `Auth/`, `TicketStoreOptions` in `Tickets/Stores/`. A shared
`Configuration/` folder would be a kind-folder under another name.

### 4. `Shared/` holds only what genuinely spans features

`ValidationException` is thrown by the ticket service and caught by the endpoint
filters, so it is shared. Everything else moved to the one feature that owns it —
`CustomerNotification` to `Notifications/`, `DevelopmentSigningKey` to `Auth/`.
`Shared/` is not a home for things that resist classification.

### 5. `Data/` stays infrastructure, outside the feature folders

`TicketDbContext`, the seeding in `TicketDatabase` and the migrations serve both
`Tickets` and `Auth`. Migrations also carry generated namespaces and a model
snapshot that EF regenerates, so they are left where the tooling puts them.

### 6. DI registration stays inline in `Program.cs`

Per-feature `AddTickets()` extensions were considered and rejected: four new
files, each hiding a couple of `AddSingleton` calls. One readable registration
list beats a treasure hunt. Revisit if `Program.cs` outgrows a screen.

## Consequences

- The entity type names EF records in the migration snapshot changed
  (`TicketApi.Entities.Ticket` → `TicketApi.Tickets.Ticket`). The names were
  rewritten in the committed migrations; tables and columns are untouched, so no
  new migration and no database change.
- `TicketService` needs `using ValidationException = TicketApi.Shared.ValidationException;`
  — the type no longer shares a namespace with its caller, so it now collides with
  `System.ComponentModel.DataAnnotations.ValidationException`.
- A new file has one obvious home, and the answer does not depend on whether the
  author thought of it as a service or a helper.
