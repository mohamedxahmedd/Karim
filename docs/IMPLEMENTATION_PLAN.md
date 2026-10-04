# Implementation Plan

Requirements source: master implementation prompt §0–§27 (the prompt text ends inside §27
"Admin Dashboard"). Users/roles management, settings and an audit log are implemented because
§2 (authorization, account activation) and §4 (Users, Settings features) require them.

| Phase | Scope | Exit criteria |
|---|---|---|
| 1 | Solution scaffold, vendored client libs, docs | `dotnet build` passes |
| 2 | Domain entities & enums; Application abstractions, common (Result, paging, Lang, slugs) | builds |
| 3 | Infrastructure: DbContext, configurations, Identity, storage, sanitizer, seeding, SQL Server migration | migration generated; SQL script produced |
| 4 | Web foundation: Program.cs, localization, routing, security headers, rate limiting, output cache, layouts (public + admin), tag helpers, error pages | app starts on SQLite; `/ar` and `/en` render |
| 5 | Admin auth: login/logout/lockout/access denied/change password; dashboard | integration tests for auth |
| 6 | CMS features (admin + public): Settings, Menus, Home (hero, sections, stats, links), Pages (About, Vision, Responsibilities, Airspace Mgmt, ATFM), Services | CRUD + publish verified |
| 7 | News + categories, Announcements (+ expiry job) | scheduled/draft visibility tests |
| 8 | Reports + categories, Training, Legislation (document downloads gated by status) | download gating tests |
| 9 | Media library, photo albums, videos, Leadership, Departments (org chart) | cycle-prevention tests |
| 10 | Contact form (+ admin inbox), Users & roles, audit log | abuse-protection + permission tests |
| 11 | Demo airspace dashboard, site search, sitemap/robots | demo API test |
| 12 | Docker, README, final test pass, code review | all tests green |

Each phase ends with `dotnet build`, relevant tests, `git diff` review and an update to
`docs/PROJECT_STATE.md`.
