# Support Tickets — Backend (ASP.NET Core 8, Minimal API)

REST API for the customer-support ticket system. Persists to a JSON file
server-side and serves the React frontend in `../frontend`.

## Stack

- .NET 8 (LTS), ASP.NET Core **Minimal API**
- System.Text.Json (camelCase), Swashbuckle/OpenAPI
- JSON-file storage (no database)

## Architecture

```
backend/
  Program.cs            DI, CORS, JSON, exception handling, endpoint mapping
  Entities/             Ticket, TicketResponse (+ status/role constant sets)
  Dtos/                 Create/Update/AddResponse requests, TicketDto, mapping
  Services/
    ITicketStore / JsonTicketStore     thread-safe JSON persistence (the only file I/O)
    ITicketService / TicketService     validation + orchestration + mapping
    IEmailService / ConsoleEmailService notifications (console mock)
  Endpoints/TicketEndpoints.cs         /api/tickets route group
```

Layering: endpoints → `ITicketService` → `ITicketStore` / `IEmailService`.
Endpoints never touch the file or business rules directly.

## Run

```bash
cd backend
dotnet restore
dotnet run            # http://localhost:5080  (Swagger UI at /swagger in Development)
```

On first run the store seeds from the repo-root `dataset.json` into a local
`tickets.json` (gitignored), so the original dataset stays intact.

With the backend running, start the frontend (`cd ../frontend && bun run dev`) —
its "demo data" banner disappears and create/edit/respond persist to disk.

## Troubleshooting

- **`You must install or update .NET to run this application` (needs 8.0.0).**
  Only a newer runtime (e.g. .NET 10) is installed. The project sets
  `<RollForward>Major</RollForward>`, so it runs on the newer runtime as-is.
  For an exact-target run, install the .NET 8 runtime (`brew install dotnet@8`
  or https://dotnet.microsoft.com/download/dotnet/8.0).
- **Port already in use / `403` on `http://localhost:5000`.** macOS AirPlay
  Receiver occupies port 5000, so the dev port is **5080** here. (To free 5000
  instead: System Settings → General → AirDrop & Handoff → turn off AirPlay
  Receiver.) The frontend proxy in `frontend/vite.config.ts` targets 5080 to match.

## API

| Method | Path | Body | Success | Errors |
|--------|------|------|---------|--------|
| GET | `/api/tickets` | — | 200 `Ticket[]` | — |
| GET | `/api/tickets/{id}` | — | 200 `Ticket` | 404 |
| POST | `/api/tickets` | `{ name, email, description }` | 201 + Location | 400 |
| PUT | `/api/tickets/{id}` | `{ status, resolution }` | 200 `Ticket` | 400, 404 |
| POST | `/api/tickets/{id}/responses` | `{ author, role, body }` | 200 `Ticket` | 400, 404 |

Statuses: `New`, `In Progress`, `Resolved`, `Closed`. Roles: `customer`, `admin`.
Server sets `id`, timestamps, and defaults (`status=New`, empty summary/resolution/responses) on create.

## Notifications

`ConsoleEmailService` logs a simulated email on: ticket created, status changed,
resolution changed — each with a customer tracking link.

## Out of scope (future branches)

- `feature/jwt-auth` — admin login + JWT; protect PUT and POST /responses
- `feature/ai-summary` — AI-generated `summary` on create
- `feature/email-gmail-smtp` — real email via MailKit + Gmail SMTP

Seams are in place (`IEmailService`, `// BONUS` comments) so each lands without refactoring.
