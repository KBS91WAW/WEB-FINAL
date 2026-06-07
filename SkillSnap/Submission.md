# SkillSnap — Submission Document

**Author:** Krzysztof  
**Date:** June 7, 2026  
**Stack:** .NET 10 · Blazor WebAssembly · ASP.NET Core Web API · SQLite · Entity Framework Core

---

## 1. Project Summary

**SkillSnap** is a personal portfolio web application that lets a developer showcase their projects and skills in one place. It consists of two independently running services:

| Service | Tech | URL |
|---|---|---|
| `SkillSnap.Api` | ASP.NET Core 10 REST API | `https://localhost:7000` |
| `SkillSnap.Client` | Blazor WebAssembly SPA | `https://localhost:5001` |

The **portfolio home page** displays a profile card, a list of project cards (title, description, image), and a skill tags list (name + proficiency level). Registered users with the **Admin** role can add, edit, and delete both projects and skills directly from the UI without leaving the page. All other visitors can browse the portfolio read-only.

---

## 2. Architecture Overview

```
Browser (Blazor WASM)
  │
  ├── Pages/         Home, Login, Register
  ├── Components/    ProfileCard, ProjectList, SkillTags
  │                  AddProject, AddSkill   (reusable form components)
  ├── Services/      AuthService, ProjectService, SkillService
  │                  UserSessionService     (in-memory session state)
  └── Layout/        MainLayout, NavMenu
        │
        │  HTTPS + Bearer JWT
        ▼
ASP.NET Core API
  ├── Controllers/   AuthController, ProjectsController, SkillsController, SeedController
  ├── Models/        PortfolioUser, Project, Skill, ApplicationUser
  ├── Data/          SkillSnapContext (IdentityDbContext)
  └── SQLite DB      skillsnap.db
```

---

## 3. Key Features

### 3.1 CRUD Operations

Full create / read / update / delete is implemented for both **Projects** and **Skills**.

| Endpoint | Method | Auth required |
|---|---|---|
| `/api/projects` | GET | No |
| `/api/projects` | POST | Admin |
| `/api/projects/{id}` | PUT | Admin |
| `/api/projects/{id}` | DELETE | Admin |
| `/api/skills` | GET | No |
| `/api/skills` | POST | Admin |
| `/api/skills/{id}` | PUT | Admin |
| `/api/skills/{id}` | DELETE | Admin |

- Validation attributes (`[Required]`, `[StringLength]`) are applied at the model level on both the API (`SkillSnap.Api.Models`) and enforced client-side before submission.
- The API defaults `PortfolioUserId` to the first portfolio user if the client sends `0`, preventing foreign-key errors.

### 3.2 Security

- **ASP.NET Core Identity** manages user accounts (hashed passwords, lockout, email normalization).
- **JWT Bearer authentication** — tokens are signed with HMAC-SHA256, expire after 8 hours, and embed the user's `sub`, `email`, and `role` claims.
- **Role-based authorization** — the `Admin` role is required for all write operations (`[Authorize(Roles = "Admin")]`). Every registered user is automatically assigned the Admin role on registration (single-user portfolio assumption).
- Tokens are stored in **`localStorage`** via a thin JavaScript wrapper (`wwwroot/js/storage.js`) that returns `Promise.resolve()` immediately — this avoids the `Unchecked runtime.lastError` browser warning caused by Blazor's async message-channel interop.
- **CORS** is locked to known origins (`https://localhost:5001`). Custom response headers (`X-Cache`, `X-Cache-Invalidated`) are whitelisted via `WithExposedHeaders`.
- Unauthenticated requests to protected endpoints return **401 Unauthorized**; requests from users without the Admin role return **403 Forbidden**.

### 3.3 Server-Side Caching

`IMemoryCache` is used to cache the full project and skill lists for **5 minutes**.

| Trigger | Cache effect |
|---|---|
| GET `/api/projects` or `/api/skills` | Served from cache if present (HIT); otherwise queries DB and stores result (MISS) |
| POST / PUT / DELETE | Cache entry immediately removed (invalidated) |

**Observability headers** are included in every response so cache behaviour is visible without reading server logs:

| Header | On GET | On write |
|---|---|---|
| `X-Cache` | `HIT` or `MISS` | — |
| `X-Cache-Items` | Count of items returned | — |
| `X-Response-Time-Ms` | Server latency in ms | — |
| `X-Cache-Invalidated` | — | Key that was evicted |

Server logs use structured prefixes: `[Cache MISS]`, `[Cache HIT]`, `[Cache INVALIDATED]`.

### 3.4 Client-Side State Management

`UserSessionService` is a singleton scoped to the browser session (DI-registered as `Scoped` in Blazor WASM, which maps to a single instance per app lifetime). It holds:

- `UserId` — GUID from JWT `sub` claim
- `Email` — from JWT `email` claim, displayed in the top navigation bar
- `Role` — from `ClaimTypes.Role`; drives `IsAdmin` flag

Components subscribe to `Session.OnChange` to reactively re-render when the user logs in or out. The top bar shows `Admin user@example.com` for authenticated admins and nothing for unauthenticated visitors.

Session is restored on page load by `AuthService.InitializeAsync()`, which reads the token from `localStorage` and repopulates the session without requiring a new login.

---

## 4. Development Process and Use of Cursor / Copilot

SkillSnap was built iteratively in a single Cursor AI session. Cursor's agent handled the majority of code generation, debugging, and refactoring while I directed the high-level workflow.

### Phase 1 — Scaffolding and environment
- Created the `SkillSnap.sln` solution, added both projects.
- Installed .NET 10 SDK via `winget` when the build failed due to a version mismatch.
- Trusted the ASP.NET Core development certificate (`dotnet dev-certs https --trust`).

### Phase 2 — Core API and data model
- Defined `PortfolioUser`, `Project`, `Skill`, and `ApplicationUser` models.
- Configured `SkillSnapContext` (inheriting `IdentityDbContext`), ran EF Core migrations.
- Implemented `ProjectsController` and `SkillsController` (GET + POST), then added PUT and DELETE after the validation phase.
- Added `IMemoryCache` for GET responses; cache is invalidated on every write.
- Implemented `SeedController` for sample data.

### Phase 3 — Authentication and authorization
- Wired up ASP.NET Core Identity and JWT Bearer.
- Debugged 403 errors caused by `ClaimTypes.Role` not being mapped correctly in the token validation parameters — fixed by setting `RoleClaimType = ClaimTypes.Role` and `MapInboundClaims = true`.
- Added startup seeding to ensure the `Admin` role exists and all registered users are assigned to it.

### Phase 4 — Blazor client
- Implemented `AuthService` with login, logout, register, and `InitializeAsync` (session restore).
- Replaced direct `localStorage.*` JS interop calls with a `skillSnapStorage` wrapper using `.then()`-based promises, eliminating the `Unchecked runtime.lastError` browser warning.
- Built `ProjectList`, `SkillTags`, `ProfileCard` components.
- Refactored add-forms into standalone reusable components: `AddProject.razor` and `AddSkill.razor`, each accepting an `EventCallback OnAdded` parameter.
- Added inline edit/delete UI per card, visible only to admins.

### Phase 5 — UI polish
- Applied a custom CSS design system (CSS variables, gradients, Inter font, responsive layout).
- Created SVG assets: logo, avatar placeholder, project placeholder, section icons.
- Updated the top navigation bar to show the logged-in user's **email** instead of their raw GUID.

### Phase 6 — Validation and hardening
- Ran a 10-step curl-based cache consistency test (MISS → HIT → POST invalidates → MISS → HIT → PUT invalidates → MISS → updated title confirmed → DELETE → MISS → gone).
- Added `X-Cache`, `X-Cache-Items`, `X-Response-Time-Ms`, and `X-Cache-Invalidated` response headers for observability.
- Verified all protected endpoints return 401/403 for unauthenticated/unauthorised requests.

### Cursor AI contributions
Cursor AI was used throughout:
- **Code generation** — full file implementations for controllers, services, Razor components, CSS, and JS were generated from natural-language prompts.
- **Bug fixing** — investigated and fixed CORS issues, JWT role claim mapping, JSON serialization cycles, and the Blazor interop `runtime.lastError` warning.
- **Refactoring** — extracted inline form blocks into standalone reusable components on request.
- **Testing** — wrote and ran PowerShell/curl test scripts for end-to-end validation of auth, CRUD, and cache behaviour.

---

## 5. File Structure

```
SkillSnap/
├── SkillSnap.sln
├── Submission.md
│
├── SkillSnap.Api/
│   ├── Controllers/
│   │   ├── AuthController.cs         — register, login, JWT generation
│   │   ├── ProjectsController.cs     — CRUD + cache + observability headers
│   │   ├── SkillsController.cs       — CRUD + cache + observability headers
│   │   └── SeedController.cs         — sample data seeder
│   ├── Data/
│   │   └── SkillSnapContext.cs       — EF Core DbContext + Identity
│   ├── Migrations/                   — EF Core migration history
│   ├── Models/
│   │   ├── ApplicationUser.cs        — Identity user extension
│   │   ├── PortfolioUser.cs          — portfolio profile
│   │   ├── Project.cs                — project entity
│   │   └── Skill.cs                  — skill entity
│   └── Program.cs                    — DI, auth, CORS, DB seed, pipeline
│
└── SkillSnap.Client/
    ├── Components/
    │   ├── AddProject.razor          — reusable add-project form
    │   ├── AddSkill.razor            — reusable add-skill form
    │   ├── ProfileCard.razor         — portfolio profile display
    │   ├── ProjectList.razor         — project list with inline edit/delete
    │   └── SkillTags.razor           — skill tags with inline edit/delete
    ├── Layout/
    │   ├── MainLayout.razor          — top bar with user email/role badge
    │   └── NavMenu.razor             — sidebar navigation
    ├── Models/
    │   └── ApiModels.cs              — client-side Project and Skill DTOs
    ├── Pages/
    │   ├── Home.razor                — main portfolio page
    │   ├── Login.razor               — login form
    │   └── Register.razor            — registration form
    ├── Services/
    │   ├── AuthService.cs            — auth API calls + localStorage token
    │   ├── ProjectService.cs         — project CRUD API calls
    │   ├── SkillService.cs           — skill CRUD API calls
    │   └── UserSessionService.cs     — in-memory session (UserId, Email, Role)
    └── wwwroot/
        ├── css/app.css               — custom design system (variables, components)
        ├── js/storage.js             — localStorage wrapper (Promise.resolve pattern)
        └── images/                   — SVG logo, avatar, project placeholder, icons
```

---

## 6. Known Issues

| Issue | Impact | Notes |
|---|---|---|
| All registered users become Admin | Medium | By design for a single-owner portfolio, but would need a role-assignment UI for multi-user scenarios |
| JWT stored in `localStorage` | Low–Medium | Susceptible to XSS; `HttpOnly` cookie would be more secure for production |
| SQLite as database | Low | Adequate for development and single-instance deployment; replace with PostgreSQL/SQL Server for production |
| No HTTPS redirect middleware | Low | `app.UseHttpsRedirection()` is not enabled; dev certs handle HTTPS directly |
| Cache is in-memory only | Low | Restarting the API clears the cache; a distributed cache (Redis) would be needed for multi-instance deployments |
| No pagination on GET endpoints | Low | Lists are returned in full; large portfolios would benefit from cursor-based pagination |

---

## 7. Future Improvements

- **Role management UI** — allow assigning/revoking roles without touching the database directly
- **Image upload** — replace URL input with a file upload backed by blob storage (Azure Blob / S3)
- **Profile editing** — allow editing the `PortfolioUser` name, bio, and avatar from the UI
- **Refresh token** — add a refresh token endpoint so sessions can be extended without re-login
- **Unit and integration tests** — `xUnit` + `WebApplicationFactory` tests for API controllers; `bUnit` tests for Blazor components
- **OpenAPI / Swagger** — add Swashbuckle to expose interactive API documentation
- **CI/CD pipeline** — GitHub Actions workflow for build, test, and container image publish
- **Dark mode** — toggle between light and dark CSS themes using a CSS variable swap

---

## 8. Running the Application

### Prerequisites
- .NET 10 SDK
- Trusted dev certificate: `dotnet dev-certs https --trust`

### Start API
```powershell
cd SkillSnap/SkillSnap.Api
dotnet run --urls https://localhost:7000
```

### Start Client (separate terminal)
```powershell
cd SkillSnap/SkillSnap.Client
dotnet run --urls https://localhost:5001
```

Open **https://localhost:5001** in the browser.

### Seed sample data
```powershell
curl.exe -k -X POST https://localhost:7000/api/seed
```

### Create first admin user
Navigate to **https://localhost:5001/register** — the first registered user is automatically granted the Admin role.
