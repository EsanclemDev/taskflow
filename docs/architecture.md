# TaskFlow — Architecture (v0.2.0)

## Overview

TaskFlow is a task-management application with an Angular 21 frontend, an ASP.NET Core 10 REST API, and PostgreSQL persistence through Entity Framework Core and Npgsql.

```text
Angular 21 (Material, Signals, RxJS)
             |
          HTTPS/REST
             |
ASP.NET Core 10 API
  |-- AuthController
  |-- TasksController
  |-- ASP.NET Core Identity
  |-- ActiveSessionMiddleware
  |-- AccessTokenService / RefreshTokenService
             |
      EF Core / Npgsql
             |
         PostgreSQL
```

## Responsibilities

- **Frontend:** authentication screens, task dashboard, reactive UI, and logout coordination across browser tabs.
- **Controllers:** HTTP endpoints for registration, login, session lifecycle, and task CRUD.
- **Services:** access-token issuance and refresh-session lifecycle.
- **Middleware:** verifies that an authenticated access token belongs to an active session family before authorization.
- **Persistence:** EF Core stores tasks, Identity users, refresh sessions, and Data Protection keys.

## Authorization and task ownership

`TasksController` requires authentication at controller level. Every read, update, and delete query filters by both task ID (where applicable) and the current user's `UserId`. New tasks are assigned to the authenticated user. A task owned by another user is not returned by ID-based queries.

## API surface

| Method | Path | Purpose | Authentication |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | Register user | No |
| POST | `/api/auth/login` | Authenticate and establish session | No |
| GET | `/api/auth/me` | Current user | Bearer token |
| POST | `/api/auth/refresh` | Rotate refresh token and issue access token | Refresh cookie; origin filter |
| POST | `/api/auth/logout` | Revoke session family | Refresh cookie; origin filter |
| GET | `/api/tasks` | List current user's tasks | Bearer token |
| GET | `/api/tasks/{id}` | Retrieve owned task | Bearer token |
| POST | `/api/tasks` | Create task | Bearer token |
| PUT | `/api/tasks/{id}` | Update owned task | Bearer token |
| DELETE | `/api/tasks/{id}` | Delete owned task | Bearer token |

## Environments

| Environment | Frontend API configuration | Database |
| --- | --- | --- |
| Development | `https://localhost:7292/api` after local port alignment | Docker Compose PostgreSQL 17 |
| Beta | `/api` (requires host-side proxy/routing) | Environment-specific |
| Production | `https://produccion-wdfb.onrender.com/api` | Neon PostgreSQL |

The backend's HTTPS launch profile is `https://localhost:7292` and HTTP profile is `http://localhost:5297`. The original Angular development environment referenced port 7000; align it with the launch profile if not already done.

## Tests and delivery

Backend tests run through GitHub Actions on .NET 10 with a temporary PostgreSQL 16 service. Production frontend is hosted on Cloudflare Pages, API on Render, and database on Neon. Test infrastructure must not use the production database.

## Design notes

- Swagger UI is enabled only in the `Development` environment.
- CORS allows configured frontend origins and credentials.
- Access tokens are ASP.NET Core Identity bearer tokens, **not JWTs**.
- Data Protection keys are persisted through EF Core so token protection can survive application restarts.
