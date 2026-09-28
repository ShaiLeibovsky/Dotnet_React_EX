# ADR-0003: Customer ticket images stored on disk and served as static files

- Status: accepted
- Date: 2026-09-28
- Issue: [#10](https://github.com/ShaiLeibovsky/Dotnet_React_EX/issues/10)

## Context

`dataset.json` carries an `imageUrl` on every row, and ADR-0001 added `ImageUrl` to
the `Ticket` entity so seeded values survive a write. The field was storage-only:
no customer could supply an image and no screen showed one.

A customer opening a ticket about a broken appliance has a photo, and support reads
the ticket faster with it than without. Accepting a file from an unauthenticated
form is a trust boundary, so the bytes have to be checked before they reach disk.

## Decision

### 1. What counts as an acceptable image

`TicketImageStore` rejects an upload over 5 MB, and rejects anything whose leading
bytes are not a PNG, JPEG, GIF or WebP signature. The extension the file arrives
with, and the `Content-Type` the browser declares, are both ignored — a `.png`
suffix on a Windows executable is exactly the case the sniff exists to catch. The
stored file is renamed to a GUID plus the extension the signature implies, so a
caller cannot choose a path, a name or a served content type.

5 MB holds a phone photo without resizing and is small enough that a rejected
upload costs one request rather than a slow one. Both rules answer with a 400
`ValidationProblemDetails` naming the `Image` field, the same shape the other
create-ticket validation errors already use, so the dialog renders the server's
message unchanged.

### 2. Disk and static files over a database column

Images live in the gitignored `TicketStore:UploadDirectory` and are served by
`UseStaticFiles` under `/uploads`, matching the relative `imageUrl` form the
dataset already uses. A BLOB column would put megabytes through EF Core change
tracking and force every read of a ticket to decide whether to hydrate them, for
no gain at this scale. Responses carry `X-Content-Type-Options: nosniff` and
`ServeUnknownFileTypes` stays off, so a file can only ever be served as one of the
four image types it was verified to be.

The cost is that the directory is per-instance state: a second API instance would
need shared storage, at which point the store becomes an object-storage client and
`imageUrl` becomes absolute.

### 3. POST /api/tickets accepts both JSON and multipart

The endpoint reads a `NewTicketPayload` from either content type: multipart when a
file is attached, JSON otherwise. The frontend always sends multipart, with or
without a file. Keeping the JSON path costs a branch and keeps every existing
client — and the Swagger-driven manual check — working unchanged.

### 4. TicketDto is no longer frozen

ADR-0001 held `ImageUrl` back from `TicketDto` on the grounds that the DTO mirrors
a fixed frontend contract. Showing the image requires breaking that, so the field
is added to both the DTO and the TypeScript `Ticket` type together. The two are
edited as one contract from here on.

## Consequences

- `ITicketService.CreateAsync` takes a `NewTicketPayload` rather than a
  `CreateTicketRequest`, because writing the file is part of creating the ticket
  and has to fail the request when the file is rejected.
- Uploaded files outlive the tickets that reference them: nothing deletes an image,
  because nothing deletes a ticket yet.
- Seeded tickets reference `uploads/*.jpg` files that the repo does not ship, so
  their images 404 until a real file is placed there.
