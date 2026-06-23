# Support Tickets — Frontend (React + Vite)

GitHub-style UI for the customer-support ticket system. Talks to the
ASP.NET Core backend **only via `/api`** endpoints; falls back to in-memory
demo data when the backend is not running.

## Run

```bash
npm install
npm run dev      # http://localhost:5173
```

Dev server proxies `/api` → `http://localhost:5000` (the .NET backend).
Change the target in `vite.config.js` if your backend runs elsewhere.

## Screens

| Route | Purpose |
|-------|---------|
| `/` | All tickets — table/list view, status filter, name/description search, **New ticket** modal |
| `/tickets/:id` | Single ticket by unique ID — customer details, description, AI summary, edit status + resolution, save |
| `/login` | Admin login (bonus). Logged-in users can edit; anyone can create/view. |

## Expected API (backend contract)

| Method | Path | Body | Returns |
|--------|------|------|---------|
| GET | `/api/tickets` | — | `Ticket[]` |
| GET | `/api/tickets/{id}` | — | `Ticket` |
| POST | `/api/tickets` | `{ name, email, description }` | created `Ticket` |
| PUT | `/api/tickets/{id}` | `{ status, resolution }` | updated `Ticket` |

`Ticket` shape: `{ id, name, email, description, summary, status, resolution, createdAt, updatedAt }`.

Statuses: `New`, `In Progress`, `Resolved`, `Closed`.

## Notes

- Email sending + AI summary are backend concerns; the UI shows the summary
  when present and toasts a "simulated email sent" notice on status/resolution change.
- Auth is a client-side stub (`src/auth.jsx`) — swap in a real `/api/auth/login`
  + JWT call when the backend is ready.
