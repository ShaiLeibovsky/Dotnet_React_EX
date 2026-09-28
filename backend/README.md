# Support Tickets — Backend (ASP.NET Core 8, Minimal API)

REST API for the customer-support ticket system. Persists to a JSON file
server-side (or SQLite, by configuration) and serves the React frontend in `../frontend`.

## Stack

- .NET 8 (LTS), ASP.NET Core **Minimal API**
- System.Text.Json (camelCase), Swashbuckle/OpenAPI
- JSON-file storage by default, EF Core + SQLite as an alternative store

## Architecture

```
backend/
  Program.cs            DI, CORS, JSON, exception handling, endpoint mapping
  Entities/             Ticket (+ status constant set)
  Dtos/                 Create/Update requests, TicketDto, mapping
  Services/
    ITicketStore / JsonTicketStore     thread-safe JSON-file persistence
    ITicketStore / SqliteTicketStore   EF Core persistence, selected by configuration
    ITicketService / TicketService     validation + orchestration + mapping
    ICustomerNotifier / LogNotifier    notification channel that only logs (the default)
    ICustomerNotifier / EmailNotifier  notification channel over SMTP, when configured
    ISummaryService / GeminiSummaryService  AI summary on create (Null impl with no key)
  Data/                 TicketDbContext, migrations, startup migrate + seed
  Endpoints/TicketEndpoints.cs         /api/tickets route group
```

Layering: endpoints → `ITicketService` → `ITicketStore` / `ICustomerNotifier`.
Endpoints never touch storage or business rules directly.

## Run

```bash
cd backend
dotnet restore
dotnet run            # http://localhost:5006  (Swagger UI at /swagger in Development)
```

On first run the store seeds from the repo-root `dataset.json` into a local
`tickets.json` (gitignored), so the original dataset stays intact.

## Storage

`TicketStore:Provider` selects the store: `Json` (default) or `Sqlite`. The SQLite
store migrates its schema on startup and seeds from `dataset.json` when the ticket
table is empty, into the gitignored `TicketStore:DatabasePath` file.

```bash
dotnet run -- --TicketStore:Provider=Sqlite
```

EF tooling needs the same switch, since the context is only registered for SQLite:

```bash
dotnet ef migrations add <Name> -- --TicketStore:Provider=Sqlite
```

With the backend running, start the frontend (`cd ../frontend && bun run dev`) —
create and edit persist to disk. With the backend stopped, the frontend screens
show an error and a retry control.

## Troubleshooting

- **`You must install or update .NET to run this application` (needs 8.0.0).**
  Only a newer runtime (e.g. .NET 10) is installed. The project sets
  `<RollForward>Major</RollForward>`, so it runs on the newer runtime as-is.
  For an exact-target run, install the .NET 8 runtime (`brew install dotnet@8`
  or https://dotnet.microsoft.com/download/dotnet/8.0).

## API

| Method | Path | Body | Success | Errors |
|--------|------|------|---------|--------|
| GET | `/api/tickets` | — | 200 `Ticket[]` | — |
| GET | `/api/tickets/{id}` | — | 200 `Ticket` | 404 |
| POST | `/api/tickets` | `{ name, email, description }` | 201 + Location | 400 |
| PUT | `/api/tickets/{id}` | `{ status, resolution }` | 200 `Ticket` | 400, 404 |

Statuses: `New`, `In Progress`, `Resolved`, `Closed`.
Server sets `id`, timestamps, and defaults (`status=New`, empty summary/resolution) on create.

## Notifications

`LogNotifier` logs each notification on: ticket created, status changed,
resolution changed — each with a customer tracking link.

`EmailNotifier` sends the same three by email, and replaces `LogNotifier` whenever
`Email:SmtpHost`, `Email:SmtpUser` and `Email:SmtpPassword` are all configured (user-secrets
in Development, environment variables elsewhere). The chosen one is logged on startup. A send
failure is logged and never fails the request that triggered it. See the root README for the
reviewer-facing setup.

## AI summary

On create, `ISummaryService` is asked for a one-sentence summary of the description,
stored on the ticket and shown in the ticket table and detail view. With no API key
configured, `NullSummaryService` is registered and tickets are created with no
summary — a fresh clone runs correctly with no credentials. Generation is
best-effort: a provider failure — or an answer too long to be a summary — is logged
and the ticket is still created.

Supply your own [Gemini API key](https://aistudio.google.com/apikey) through
user-secrets, so it is never committed:

```bash
cd backend
dotnet user-secrets set "Summary:ApiKey" "<your-gemini-api-key>"
dotnet run
```

`Summary:Model`, `Summary:TimeoutSeconds` and `Summary:MaxWords` are in
`appsettings.json`. To turn the feature off again, `dotnet user-secrets remove "Summary:ApiKey"`.

