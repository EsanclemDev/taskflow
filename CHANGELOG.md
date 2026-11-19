# Changelog

All notable changes to TaskFlow are documented here.

## [0.3.0] — Beta / Develop — 2026-11-19

### Added

- New task management dashboard with summary statistics and improved navigation.
- Kanban board with drag-and-drop task management.
- Extended task workflow with `pending`, `in_progress`, and `completed` statuses.
- Task priorities, categories, and optional due dates.
- Task creator and assignee tracking.
- Improved task creation, editing, and detail views.
- Demo account functionality for visitors to explore TaskFlow without registering.
- Automatic creation of isolated demo users with 12 predefined tasks.
- Demo task reset functionality.
- Demo account expiration after 24 hours and expired-account cleanup.
- Rate limiting for demo account creation.

### Changed

- Redesigned task management interface using Angular Material.
- Updated task data models and API contracts to support the expanded workflow.
- Extended PostgreSQL persistence with task metadata and demo account support.
- Improved separation between beta and production deployment environments.
- Updated the beta frontend to communicate with the backend through a same-origin Cloudflare Pages proxy.

### Security

- Preserved per-user task ownership and authorization checks.
- Isolated demo user data from registered user accounts.
- Added protections against excessive demo account creation.
- Configured Cloudflare Pages API proxy to use the `API_ORIGIN` environment variable.
- Added HTTPS origin validation and request-origin checks in the API proxy.
- Preserved server-side session revocation and cross-tab logout synchronization.

### Database

Added the following Entity Framework Core migrations:

- `20261118133802_AddTaskWorkflowStatus`
- `20261118142635_AddTaskCreatorAndAssignee`
- `20261118145240_AddTaskDetails`
- `20261118163122_AddDemoUsers`

Applied the pending migrations to the Neon `develop` database branch.

### Testing and CI/CD

- Expanded backend integration and authentication tests.
- Verified 61 backend tests passing.
- Configured GitHub Actions to run PostgreSQL-dependent tests using an isolated PostgreSQL service.
- Fixed test infrastructure and database provider configuration issues.
- Verified the backend test workflow passes on `develop`.

### Deployment

- Integrated v0.3.0 into the `develop` branch.
- Updated the beta database schema in Neon.
- Maintained production isolation: v0.3.0 has not yet been merged into `main`.

### Documentation

- Updated the changelog with v0.3.0 features, database migrations, security changes, and deployment status.
- Updated the README to describe the dashboard, Kanban board, demo mode, and beta architecture.

## [0.2.0] — Release candidate

### Added

- User registration, login, and current-user endpoint using ASP.NET Core Identity.
- Opaque bearer access tokens and refresh-token-based session renewal.
- Refresh-token rotation and session-family revocation.
- Server-side active-session middleware.
- Logout synchronization across browser tabs.
- User ownership checks for all task CRUD endpoints.
- Persistent ASP.NET Core Data Protection keys.
- Backend authentication and PostgreSQL integration tests.
- GitHub Actions test workflow with isolated PostgreSQL service.
- Separate beta and production frontend environment configurations.
- Production hosting on Cloudflare Pages, Render, and Neon.

### Security

- Protected task endpoints with per-user database filtering.
- Secure, HttpOnly refresh cookie with configurable SameSite policy.
- Origin validation filter on refresh and logout endpoints.
- Identity password policy and account lockout configuration.

### Documentation

- Updated project README.
- Added architecture, authentication, and deployment guides.

> Replace “Release candidate” with the actual release date after system tests, documentation review, and production verification are complete.
