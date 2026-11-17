# TaskFlow — Authentication and Session Security (v0.2.0)

## Authentication model

TaskFlow uses ASP.NET Core Identity with the built-in bearer-token authentication scheme. Access tokens are protected bearer tokens, **not JWTs**. An access token expires after **60 minutes**; the configured refresh-token lifetime is **7 days**.

Identity enforces unique email addresses, passwords of at least eight characters containing uppercase, lowercase, digit, and non-alphanumeric characters, and a 15-minute lockout after five failed attempts.

## Endpoints

### `POST /api/auth/register`

Accepts the registration DTO (email and password), creates an Identity user, and returns HTTP `201` with a message and `UserId`. Duplicate emails and Identity validation failures return `400`.

### `POST /api/auth/login`

Checks credentials with lockout enabled. On success, creates a refresh-session family, issues a bearer access token, sets a refresh cookie, and returns:

```json
{
  "tokenType": "Bearer",
  "accessToken": "<opaque token>",
  "expiresIn": 3600
}
```

Invalid credentials return `401`; locked accounts return `423`.

### `GET /api/auth/me`

Requires a bearer access token. Returns the current user's `Id`, `Email`, and `CreatedAt`.

### `POST /api/auth/refresh`

Reads the `taskflow_refresh` cookie, validates the session and user, rotates the refresh token, issues a new access token, and replaces the cookie. Invalid, expired, already-rotated, or reused tokens are rejected with `401`. The endpoint uses `ValidateAuthOrigin`.

### `POST /api/auth/logout`

Reads the refresh cookie, revokes its session family, deletes the cookie, and returns `204 No Content`. The endpoint uses `ValidateAuthOrigin`.

## Cookie settings

The refresh cookie is named `taskflow_refresh` and is configured with:

- `HttpOnly = true`
- `Secure = true`
- `Path = /api/auth`
- `MaxAge = 7 days`
- `IsEssential = true`
- `SameSite` from `Auth:RefreshCookieSameSite`, defaulting to `None`

Cross-site cookies require appropriate browser policies and HTTPS. `SameSite=None` is intentionally paired with `Secure`.

## Session revocation

The authentication pipeline executes in this order:

```text
UseAuthentication()
    -> ActiveSessionMiddleware
    -> UseAuthorization()
    -> MapControllers()
```

The middleware checks that an authenticated access token belongs to an active session family. Logging out revokes the family server-side; a previously issued access token should then be rejected even before its nominal expiry.

The frontend also coordinates logout across browser tabs. This client-side synchronization complements, but does not replace, server-side revocation.

## Task authorization

`TasksController` has `[Authorize]` at class level. Every task operation uses the authenticated user's ID. Requests for another user's task return `404` rather than exposing that task.

## Security boundaries

- Never commit real database credentials, refresh tokens, or access tokens.
- Use a dedicated test database for PostgreSQL integration tests.
- Use HTTPS in production.
- Keep the `ValidateAuthOrigin` filter and CORS allowlist aligned with deployed frontend origins.
- Do not treat browser-tab logout alone as proof of token revocation; test the server's response to a revoked access token.

## Tests worth maintaining

- Successful login, refresh rotation, and logout.
- Expired, reused, or concurrently rotated refresh tokens.
- Revocation of a session family invalidates associated access tokens.
- Independent sessions remain valid when another session is revoked.
- Cross-user task access is denied.
