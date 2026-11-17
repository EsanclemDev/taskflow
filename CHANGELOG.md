# Changelog

All notable changes to TaskFlow are documented here.

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
