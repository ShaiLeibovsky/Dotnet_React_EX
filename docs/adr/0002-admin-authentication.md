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

### 1. A users table, but no role column

Authentication alone expresses the rule: holding a valid token *is* being an admin,
because the only accounts that exist are admin accounts. A role column would be a
second source of truth for a decision with one outcome, and the authorization policy
would have to read it on every request to learn nothing. Adding roles later is an
additive migration plus a policy, not a redesign.

### 2. The users table is always SQLite, whichever ticket store is configured

`TicketStore:Provider` still selects the ticket store (ADR-0001), but `TicketDbContext`
is now registered unconditionally so the admin account has a home even when tickets
live in the JSON file. Storing credentials in a JSON file next to the ticket data was
the alternative, and it would have meant a second hand-rolled persistence path for the
one piece of data where a unique index and a real query matter most.

### 3. No refresh tokens

Tokens live eight hours — one working day — and then the admin signs in again. A
refresh token would add a second credential to store, a revocation story, and a
rotation endpoint, to save one login per day for a single account. The cost of the
decision is that a stolen token is valid until it expires; there is no server-side
revocation.

### 4. Token storage in browser local storage

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

### 5. Hashing and secrets

`PasswordHasher<AdminUser>` from `Microsoft.Extensions.Identity.Core` — already in the
ASP.NET Core shared framework — provides the salted PBKDF2 hash, so no third-party
hashing package and no hand-rolled salt handling. One admin account is seeded at
startup from `Auth:AdminEmail` and `Auth:AdminPassword`, inserted only when that email
is absent, so no hash is committed or baked into a migration. The whole `Auth` section
comes from .NET user-secrets, with nothing in `appsettings.json` to fall back on:

```
cd backend
dotnet user-secrets set "Auth:SigningKey" "$(openssl rand -base64 48)"
dotnet user-secrets set "Auth:AdminEmail" "<a mailbox you read>"
dotnet user-secrets set "Auth:AdminPassword" "<your password>"
```

The email is not secret and a committed default would spare one command, but since
section 6 sends it to customers as a `Reply-To` a default is worse than nothing: a
deployment that sets the password and forgets the email would otherwise seed an account
whose replies bounce. With the section absent, a half-configured deployment seeds no
account and says so in the log.

### 6. The handling admin is a Reply-To, not the sender

An edit notification is sent from the configured SMTP mailbox (`Email:SmtpUser`, or
`Email:SmtpFrom`) and carries the editing admin's email as `Reply-To`, repeated as one
line of the body for clients that hide the header. A customer reply reaches the admin
who touched the ticket; the credential stays one service account.

Sending *as* the admin was the alternative and was rejected on two counts. Providers
generally require the `From` to match the authenticated account, and a `From` on a
domain the sending mailbox cannot sign fails SPF and DKIM alignment, so the mail is
rejected or filtered, and `Auth:AdminEmail` is whatever mailbox the operator configured
rather than an address on the sending domain. Per-admin sending would also mean a per-admin SMTP
credential; keeping those in the `Admins` row would put a recoverable secret in the
ticket database, next to the data it is least related to, where a leaked file becomes a
mailbox takeover rather than a disclosure of tickets.

The creation notification has no admin — `POST /api/tickets` is anonymous (ADR-0002
section 1 grants edit alone) — so `handlingAdminEmail` is nullable and no `Reply-To` is
set on that path.

### 7. The generated development signing key

With no `Auth:SigningKey` configured, the API generates 48 random bytes once and keeps
them in a gitignored `*.signing-key` file beside the SQLite database, owner-readable
only, reused on every later start. A fresh clone runs with no secret configured and a
token keeps working across restarts.

Generating a key per process was the previous behaviour and cost a login on every
restart, for a value nobody chooses — unlike the admin email and password, a signing key
has no right answer a human should be asked for. Persisting it to a file rather than to
user-secrets keeps the app from writing into the developer's secret store, and rather
than to the database keeps the credential out of the file holding the data it protects.

A configured `Auth:SigningKey` still wins, so a deployment sets one through the
environment and never touches the file. The generated path logs a warning naming itself,
because a deployment that reaches it is sharing a key with whatever else can read that
directory, and loses every session if the file is lost.

## Consequences

- Two processes starting at once against the same empty directory can each generate a
  key and the later write wins, invalidating the earlier process's tokens. A dev-only
  path with one process, so it is left unguarded.
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
- A customer reply to an edit notification lands in the handling admin's own mailbox,
  which is outside the app: nothing threads it back onto the ticket.
- Emails are normalised to trimmed lower case on both the seed and the login path, so a
  re-cased `Auth:AdminEmail` cannot seed a second account past the unique index, and an
  admin who capitalises their address still signs in.
