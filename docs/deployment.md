# TaskFlow — Development and Deployment (v0.2.0)

## Deployment topology

| Component | Provider | Address / configuration |
| --- | --- | --- |
| Production frontend | Cloudflare Pages | `https://taskflow-azh.pages.dev` |
| Production API | Render | `https://produccion-wdfb.onrender.com` |
| Production database | Neon | PostgreSQL; connection configured in Render |
| Beta API | Render | `https://taskflow-1-9ot2.onrender.com` |
| CI | GitHub Actions | .NET 10 and temporary PostgreSQL 16 |

## Local prerequisites

- .NET 10 SDK and EF Core CLI (`dotnet ef`)
- Node.js and npm
- Docker Desktop with Docker Compose
- Git

## Local database

Create `.env` from `.env.example` at repository root. Example **local-only** values:

```dotenv
POSTGRES_DB=taskmanager
POSTGRES_USER=devuser
POSTGRES_PASSWORD=change-me
```

Start PostgreSQL 17:

```bash
docker compose up -d
```

The Compose file publishes PostgreSQL only to `127.0.0.1:5432` and persists data in a Docker volume.

## Backend

Configure `ConnectionStrings__DefaultConnection` in the local environment (do not commit credentials):

```text
Host=localhost;Port=5432;Database=taskmanager;Username=devuser;Password=change-me
```

From the repository root:

```bash
dotnet ef database update --project backend/TaskManager.Api --startup-project backend/TaskManager.Api
dotnet run --project backend/TaskManager.Api --launch-profile https
```

The HTTPS launch profile listens on `https://localhost:7292`; the HTTP profile listens on `http://localhost:5297`.

## Frontend

```bash
cd frontend/task-manager-ui
npm ci
npm start
```

Open `http://localhost:4200`. Ensure `src/app/environments/environments.ts` uses `https://localhost:7292/api` if using the checked-in HTTPS launch profile.

## Production configuration

In Render, set the secret environment variable:

```text
ConnectionStrings__DefaultConnection
```

Use the **Neon production** PostgreSQL connection string in Npgsql format, for example:

```text
Host=<neon-host>;Port=5432;Database=<database>;Username=<user>;Password=<secret>;SSL Mode=Require
```

Never use the dedicated test database or expose credentials in frontend configuration. Apply EF Core migrations to the intended production database through a controlled deployment process; do not use `EnsureCreated` against an existing migrated production database.

The Angular production environment uses:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://produccion-wdfb.onrender.com/api'
};
```

Angular `angular.json` replaces the development environment file with `environments.prod.ts` in production builds. Beta builds replace it with `environments.beta.ts`, which uses `/api` and therefore requires host-side routing to the beta backend.

## CI checks

GitHub Actions builds and tests the backend on .NET 10. PostgreSQL-dependent tests use a temporary PostgreSQL 16 service configured for the test database. Do not point CI at Neon production.

## Production smoke tests

1. Confirm the API deployment is healthy and reachable.
2. Register a new account and log in.
3. Create, edit, complete, and delete a task.
4. Confirm another user cannot read or modify the first user's tasks.
5. Verify refresh behavior and logout across tabs.
6. Confirm a revoked access token is rejected by the API.
7. Check application and database logs for unexpected errors.

## Release workflow

1. Prepare and verify changes on `develop`.
2. Confirm GitHub Actions tests pass.
3. Merge `develop` into `main` and push.
4. Verify production deployment and smoke tests.
5. Tag the verified commit as `v0.2.0` and publish release notes.
