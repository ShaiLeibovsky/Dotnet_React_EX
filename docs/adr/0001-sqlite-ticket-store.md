# ADR-0001: SQLite behind the ticket store interface, with the JSON store kept as default

- Status: accepted
- Date: 2026-09-27
- Issue: [#7](https://github.com/ShaiLeibovsky/Dotnet_React_EX/issues/7)

## Context

Tickets were persisted only as a JSON file, guarded by an in-process lock. The
exercise this repo answers asks for server-side JSON storage, so that store cannot
simply be deleted — but a file is the wrong substrate for querying, concurrent
writers or more than one API instance.

`ITicketStore` already isolated persistence: the endpoints, the ticket service and
the DTOs never touch storage. That made a second implementation a strictly additive
change, and gave a cheap way to prove the boundary is real rather than decorative.

The provided `dataset.json` carries an `imageUrl` on every row that the `Ticket`
entity did not model, so the first write silently dropped it.

## Decision

Add `SqliteTicketStore`, an EF Core implementation of the unchanged `ITicketStore`,
backed by `TicketDbContext` and a committed migration. `TicketStore:Provider`
selects the implementation and defaults to `Json`. The SQLite store migrates on
startup and seeds from the dataset file when the ticket table is empty. `Ticket`
gains `ImageUrl`, so seed data survives a write through either store — the field is
storage-only for now, because `TicketDto` is part of the frozen frontend contract.

The whole API test suite is parameterised over both providers and runs twice.

### Why SQLite over a hosted engine

The exercise must run from a fresh clone with `dotnet run` and no infrastructure.
SQLite is a file plus a NuGet package: no server, no container, no connection
secret. Postgres or SQL Server would each add a service a reviewer has to install
and start before the app works, for a single-table schema and one writer.

### Why EF Core over Dapper

Migrations are the reason. The acceptance criteria want a committed schema history,
which EF Core generates and applies; with Dapper that history would be hand-written
SQL plus a runner. EF Core also keeps the store's queries in LINQ, so the same
expressions carry over to a different provider, and its change tracking makes the
read-modify-write `UpdateAsync` contract a load-mutate-save rather than a
hand-rolled diff. The cost is a heavier dependency and less control over emitted
SQL, which a single-table schema does not need.

### Why the JSON store is retained rather than replaced

The exercise's stated storage requirement is a server-side JSON file, so a fresh
clone must still satisfy it. Keeping both implementations also makes the
persistence boundary falsifiable: the suite passes against both, which it could not
do if the service layer had leaked storage details.

### What moving to Postgres would cost

Swap the provider package for `Npgsql.EntityFrameworkCore.PostgreSQL`, add a third
`TicketStore:Provider` value with a connection string, and regenerate the migration
against the new provider (SQLite migrations are not portable). The store's LINQ,
the context and everything above the boundary stay as they are. The real cost is
operational rather than code: a running server, credentials in configuration, and
the tests needing a database per run instead of a temp file.

## Consequences

- `DateTime` values need an explicit UTC value converter: SQLite stores timestamps
  as text and reads them back with `DateTimeKind.Unspecified`, which would have
  changed the wire format the frontend already parses.
- The JSON store now orders its list by `CreatedAt` descending rather than by file
  order, so both stores return the same sequence and the list order stops depending
  on which one is configured.
- `TicketDbContext` is only registered when SQLite is selected, so EF tooling needs
  the provider passed through at design time:
  `dotnet ef migrations add <Name> -- --TicketStore:Provider=Sqlite`.
- Every schema change now needs a generated migration committed alongside it.
