# ADR-0004: Backend source grouped into feature modules

- Status: accepted
- Date: 2026-09-29

## Context

The backend was laid out by kind: `Services/` held 19 files, with `Dtos/`,
`Entities/` and `Endpoints/` beside it. Finding the implementations of an
interface meant scanning one flat alphabetical list, where `ITicketStore` sat
between `ITicketService` and `JsonTicketStore` by accident of spelling rather
than by relation. Reading one feature end to end meant opening four folders.

NestJS answers the same problem with `modules/`: one folder per feature, holding
that feature's controller, service, `dto/` and `entities/`. The shape is worth
borrowing; the framework-specific parts of it are not.

## Decision

Backend source lives in `Modules/`, one folder per feature — `Tickets`, `Auth`,
`Summaries`, `Notifications`. A module folder holds its endpoints (the
controller), its service interface and implementations, and subfolders for the
rest: `Dto/`, `Entities/`, `Util/`, `Stores/`, `Background/`. Namespaces mirror
folders exactly, down to the subfolder (`TicketApi.Modules.Tickets.Entities`).

Three folders stay outside `Modules/`: `Configuration/`, `Data/` and `Shared/`.
The test project mirrors the modules but not the `Modules/` segment itself.

### 1. Modules under a `Modules/` root, not at the project root

The root keeps four entries plus `Program.cs`, so what is a feature and what is
infrastructure is visible without reading names. Namespaces carry the segment
too — a folder whose namespace disagrees with it is a layout that has already
started rotting.

### 2. Only the service, its interface and the endpoints sit at module root

Everything else goes one level down into a folder named for what it is. A module
root that lists three files says what the module does; one that lists eleven says
nothing. `Stores/` (an interface with two implementations) and `Background/` (a
`BackgroundService`) are named for their kind rather than pushed into `Util/`,
because "util" is where things go to stop being findable.

### 3. Configuration is centralised, not per-module

`Configuration/` holds the four `*Options` classes and the `TicketStoreProvider`
enum. They are bound in one place in `Program.cs` from one `appsettings.json`, so
the thing a reader wants is the whole set at once — "what is configurable" is a
question about the app, not about a module.

### 4. `Data/` stays its own root folder

`TicketDbContext`, the seeding in `TicketDatabase` and the migrations serve both
`Tickets` and `Auth`, so no module owns them. Migrations also carry generated
namespaces and a model snapshot that EF regenerates, and keeping the folder put
keeps the tooling's defaults working.

### 5. `Shared/` holds only what genuinely spans modules

`ValidationException` is thrown by the ticket service and caught by the endpoint
filters, so it is shared. Everything else belongs to the one module that uses it —
`CustomerNotification` is a `Notifications` DTO, `DevelopmentSigningKey` an `Auth`
util. `Shared/` is not a home for things that resist classification.

### 6. No per-module registration file, and no DI extension methods

NestJS needs `users.module.ts` because its DI container is declared per module.
ASP.NET's is not: `Program.cs` already registers everything in one readable list,
and four `AddTickets()` extensions would each hide two `AddSingleton` calls behind
a jump. The `Modules/` folder is borrowed for its layout, not its wiring.

### 7. Tests mirror the modules, without the `Modules/` segment

`tests/TicketApi.Tests/` has `Tickets/`, `Auth/`, `Summaries/`, `Notifications/`
and `Support/` for shared fixtures. The test project has no infrastructure to
separate features from, so the extra segment would name nothing.

## Consequences

- The entity type names EF records in the migration snapshot changed
  (`TicketApi.Entities.Ticket` → `TicketApi.Modules.Tickets.Entities.Ticket`). The
  names were rewritten in the committed migrations; tables and columns are
  untouched, so no new migration and no database change.
- `TicketService` needs `using ValidationException = TicketApi.Shared.ValidationException;`
  — the type no longer shares a namespace with its caller, so it now collides with
  `System.ComponentModel.DataAnnotations.ValidationException`.
- Namespaces are long, and a file that touches a DTO, an entity and a store needs
  three `using` lines that used to be one. That is the price of folders that say
  where a type lives.
- A new file has one obvious home, and the answer does not depend on whether the
  author thought of it as a service or a helper.
