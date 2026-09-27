# ADR-0002: JWT bearer authentication for admins, with a users table and no roles

- Status: accepted
- Date: 2026-09-27
- Issue: [#8](https://github.com/ShaiLeibovsky/Dotnet_React_EX/issues/8)

## Context

The admin-only rule existed in the frontend only: the ticket detail screen hid the
status dropdown and the resolution editor from anyone not signed in, while
`PUT /api/tickets/{id}` accepted an unauthenticated request from anyone who could
reach the API. Signing in accepted any email with any password and minted a
hard-coded `demo-token`.

The requirement is narrow: anyone may list, read and open a ticket; only a signed-in
admin may edit one. There is one kind of privileged actor, and the exercise names JWT
authentication as the bonus to implement.

## Decision

An `AdminUser` entity in `TicketDbContext` holds an email, a salted password hash and
a creation timestamp. `POST /api/auth/login` verifies the password and returns a
signed JWT plus the authenticated email; `PUT /api/tickets/{id}` carries
`RequireAuthorization()`. Everything else stays anonymous.

### A users table, but no role column

Authentication alone expresses the rule: holding a valid token *is* being an admin,
because the only accounts that exist are admin accounts. A role column would be a
second source of truth for a decision with one outcome, and the authorization policy
would have to read it on every request to learn nothing. Adding roles later is an
additive migration plus a policy, not a redesign.

### The users table is always SQLite, whichever ticket store is configured

`TicketStore:Provider` still selects the ticket store (ADR-0001), but `TicketDbContext`
is now registered unconditionally so the admin account has a home even when tickets
live in the JSON file. Storing credentials in a JSON file next to the ticket data was
the alternative, and it would have meant a second hand-rolled persistence path for the
one piece of data where a unique index and a real query matter most.

### No refresh tokens

Tokens live eight hours — one working day — and then the admin signs in again. A
refresh token would add a second credential to store, a revocation story, and a
rotation endpoint, to save one login per day for a single account. The cost of the
decision is that a stolen token is valid until it expires; there is no server-side
revocation.

### Token storage in browser local storage

The token is kept in `localStorage`, read by the request helper on every call. The
honest tradeoff: any script that executes on the page can read it, so a
cross-site-scripting hole becomes a token theft. An `httpOnly` cookie could not be read
by injected script, which is strictly better on that axis, but it moves the problem
rather than removing it — a cookie sent automatically needs CSRF protection
(`SameSite` plus a token for state-changing requests), a same-site or proxied
deployment, and a login endpoint that sets and clears cookies instead of returning a
value the client owns. With a bearer token the frontend stays a pure API client and
survives a page refresh with no server-side session. Given a single admin account and
a demo deployment, local storage is the smaller amount of machinery; a production
deployment with real staff accounts should move to `httpOnly` cookies.

### Hashing and secrets

`PasswordHasher<AdminUser>` from `Microsoft.Extensions.Identity.Core` — already in the
ASP.NET Core shared framework — provides the salted PBKDF2 hash, so no third-party
hashing package and no hand-rolled salt handling. One admin account is seeded at
startup from `Auth:AdminEmail` and `Auth:AdminPassword`, inserted only when that email
is absent, so no hash is committed or baked into a migration. The signing key and the
seeded password come from .NET user-secrets:

```
cd backend
dotnet user-secrets set "Auth:SigningKey" "$(openssl rand -base64 48)"
dotnet user-secrets set "Auth:AdminPassword" "<your password>"
```

## Consequences

- With no `Auth:SigningKey` configured, startup generates a random key so a fresh
  clone still runs; tokens then stop validating on every restart. Marked
  `ponytail:` in `Program.cs`.
- The SQLite file is now created and migrated on every run, including with the JSON
  ticket store selected. It stays gitignored.
- A rejected token is handled in one place: the request helper clears the stored
  session and sends the browser to the login screen, so an expired token reads as an
  explanation rather than a silent failure.
- Tests authenticate through the real login endpoint. `TicketApiFactory` supplies a
  signing key and admin credentials as host settings and exposes
  `CreateAdminClientAsync()`; the suite still runs against both ticket stores.
- Wrong password and unknown email return the same 401 problem document, and the
  unknown-email path hashes against a decoy so the two take the same time; neither the
  body nor the latency reveals which accounts exist.
- Emails are normalised to trimmed lower case on both the seed and the login path, so a
  re-cased `Auth:AdminEmail` cannot seed a second account past the unique index, and an
  admin who capitalises their address still signs in.
