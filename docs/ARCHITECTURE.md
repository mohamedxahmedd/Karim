# Architecture — National Airspace Management Center (NAMC) Website & CMS

## 1. Overview

A single **ASP.NET Core 8 MVC** application serves two logically separated experiences:

| Experience | Location | Audience | Auth |
|---|---|---|---|
| Public website | root controllers (`/{culture}/...`) | everyone | none |
| Admin CMS | `Areas/Admin` (`/Admin/...`) | staff | ASP.NET Core Identity cookie + role/permission policies |

It is a server-rendered application (Razor views, partials, view components, tag helpers)
with progressive enhancement through small vanilla-JS modules. There is no SPA, no JWT.

## 2. Solution layout (pragmatic Clean Architecture / modular monolith)

```
src/
  NationalAirspace.Domain          entities, enums, domain rules (no dependencies)
  NationalAirspace.Application     use-case services per feature, DTOs, validation,
                                   abstractions (IApplicationDbContext, IFileStorage, IClock …)
  NationalAirspace.Infrastructure  EF Core DbContext + migrations, Identity, file storage,
                                   HTML sanitizer, seeding, background jobs
  NationalAirspace.Web             MVC controllers, views, view components, tag helpers,
                                   localization resources, middleware, wwwroot
tests/
  NationalAirspace.UnitTests         domain + application services (SQLite in-memory)
  NationalAirspace.IntegrationTests  full HTTP pipeline via WebApplicationFactory
```

Dependency direction: `Domain ← Application ← Infrastructure ← Web`.

### Deliberate simplifications
* **No generic repository.** Application services use `IApplicationDbContext` (EF `DbSet<T>`s)
  directly; EF Core already is the unit of work.
* **No MediatR/CQRS.** Each feature has a focused *admin service* (commands + admin queries)
  and, where it has a public face, a *public query service* returning already-localized DTOs.
* **Application references `Microsoft.EntityFrameworkCore` (abstractions only)** so services
  can compose LINQ queries. The SQL Server / SQLite providers live in Infrastructure only.

## 3. Feature organization

`Application/Features/{Feature}/` contains the DTOs, input models (with DataAnnotations
validation), and services for that feature: News, Announcements, Services, Reports, Training,
Media (library, albums, videos), Leadership, Departments, Legislation, Pages, Home (hero,
sections, statistics, links), Menus, Contact, Settings, Users, Dashboard, Audit, DemoAirspace.

## 4. Data

* SQL Server via EF Core 8 code-first migrations (`Infrastructure/Persistence/Migrations`).
* `Database:Provider` = `SqlServer` (default, production) or `Sqlite` (local demo / tests).
  SQLite uses `EnsureCreated`; SQL Server uses `Migrate()`.
* Guid keys, UTC timestamps (`CreatedAtUtc`, `UpdatedAtUtc` set centrally in `SaveChanges`),
  a `ConcurrencyStamp` optimistic-concurrency token on editable content.
* All media foreign keys use `NO ACTION`; the media service refuses to delete files in use
  (avoids SQL Server multiple-cascade-path errors and dangling content).
* Bilingual content: every content entity stores `…Ar` and `…En` columns.

## 5. Localization

* Arabic (`ar`, RTL, default) and English (`en`, LTR).
* Public URLs carry the culture: `/ar/news/…`, `/en/news/…` (`RouteDataRequestCultureProvider`).
  `/` redirects to the remembered culture (cookie) or Arabic. `/lang/{code}?returnUrl=` sets
  the cookie and redirects (local URLs only).
* Admin URLs have no culture segment; the culture comes from the same cookie.
* UI strings: `IStringLocalizer<SharedResource>` with `Resources/SharedResource.resx` (en)
  and `SharedResource.ar.resx`. A test asserts both files have identical keys.
* Content: public query services take a `Lang` argument and return localized DTOs using
  `Lang.Pick(ar, en)` (falls back to the other language when one is empty). Views never
  branch on culture.

## 6. Security

* ASP.NET Core Identity (Guid keys), cookie auth: HttpOnly, `Secure` in production,
  SameSite=Lax, 2 h sliding expiry, security-stamp validation every minute (deactivation and
  role changes take effect immediately), lockout after 5 failures, strong password policy.
* Authorization: every admin controller carries `[Authorize(Policy = Permissions.X)]`.
  Roles → permissions mapping is static (`Domain/Security/Permissions.cs`):
  `SuperAdmin`, `Administrator`, `ContentEditor`, `CommunicationsOfficer`.
* Antiforgery on every POST (global `AutoValidateAntiforgeryToken`).
* Rate limiting (built-in): contact form and login per IP.
* Contact bot mitigation: honeypot field + signed minimum-fill-time token (Data Protection).
* Rich text is sanitized server-side (`HtmlSanitizer`) on save.
* Uploads: extension allow-list + magic-byte signature check + size limits, random file
  names. Images are stored under `wwwroot/uploads` (public); documents are stored outside
  `wwwroot` (`App_Data/documents`) and streamed by controllers that check publication state.
* Security headers + Content-Security-Policy (no inline scripts), HSTS in production.
* Secrets (connection string, initial admin password) only from configuration / env vars /
  user-secrets.

## 7. Caching

* Output caching on public GET pages (policy `Public`, tag `content`, 60 s); every admin
  write evicts the tag through `IContentCacheInvalidator`.
* Menus and site settings are cached in `IMemoryCache` and invalidated on change.

## 8. Background work

`AnnouncementExpiryJob` (hosted service) archives expired announcements every 5 minutes.
Public queries also filter by expiry, so correctness does not depend on the job.

## 9. Demo airspace dashboard

`/{culture}/airspace-demo` renders a conceptual dashboard built from **simulated** data
(`DemoAirspaceService`, deterministic pseudo-random values per time window, fictional sector
and airport names). Banner: «نموذج تجريبي – بيانات غير تشغيلية». No real, military, restricted
or live data is used or implied.
