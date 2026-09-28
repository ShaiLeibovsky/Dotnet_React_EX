# Support Tickets — Frontend

GitHub-issues-style UI for the customer-support ticket system. Customers open
tickets and await a resolution; admins set status and record the resolution.
Talks to the ASP.NET Core backend **only via `/api`** endpoints. When the backend
is unreachable, screens show an error with a retry control.

## Stack

- **React + Vite**, **Bun** as runtime and package manager
- **TypeScript** (strict)
- **Tailwind CSS v4** (`@tailwindcss/vite`)
- **shadcn/ui** (new-york style, neutral base, lucide icons)
- **React Router** for pages, **sonner** for toasts

## Run

```bash
bun install
bun run dev        # http://localhost:5173
bun run build      # tsc --noEmit && vite build
bun run typecheck
```

Dev server proxies `/api` → `http://localhost:5000` (the .NET backend).
Change the target in `vite.config.ts` if your backend runs elsewhere.

## Project structure

```
src/
  api/         ticketsApi.ts        — sole data-access layer (no fetch in components)
  components/
    layout/    Header.tsx
    tickets/   StatusBadge, NewTicketDialog
    ui/        shadcn/ui primitives (generated)
  context/     AuthContext.tsx      — admin-login POC (useAuth)
  lib/         utils.ts (cn), format.ts (shortId/date)
  pages/       one folder per page — ticketsPage/, ticketDetailPage/, loginPage/
  types/       ticket.ts            — domain types + API DTOs
  index.css    Tailwind v4 + theme tokens
```

## Screens

| Route          | Purpose                                                                                                                       |
| -------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| `/`            | All tickets — table view, status filter, name/description search, **New ticket** dialog                                       |
| `/tickets/:id` | Single ticket by unique ID — customer details, description, AI summary, edit status + resolution (admin) |
| `/login`       | Admin login POC. Signed-in admins can edit; anyone can create/view.                                                           |

## Expected API (backend contract)

| Method | Path                          | Body                           | Returns          |
| ------ | ----------------------------- | ------------------------------ | ---------------- |
| GET    | `/api/tickets`                | —                              | `Ticket[]`       |
| GET    | `/api/tickets/{id}`           | —                              | `Ticket`         |
| POST   | `/api/tickets`                | `{ name, email, description }` | created `Ticket` |
| PUT    | `/api/tickets/{id}`           | `{ status, resolution }`       | updated `Ticket` |

`Ticket`: `{ id, name, email, description, summary, status, resolution, createdAt, updatedAt }`.
Statuses: `New`, `In Progress`, `Resolved`, `Closed`.

## Notes

- Email sending + AI summary are backend concerns. The UI shows the summary when
  present and toasts a "simulated email sent" notice on status/resolution change.
- Auth is a client-side POC (`src/context/AuthContext.tsx`) — swap in a real
  `/api/auth/login` + JWT call when the backend is ready.
