# SkillSnap – Project Summary

## 1. Application Description

**SkillSnap** is a full-stack portfolio and project tracker developed across five structured activities, each adding a distinct layer of functionality on top of the previous one.

- **Back end:** ASP.NET Core 10 Web API, Entity Framework Core 10, SQLite
- **Front end:** Blazor WebAssembly (.NET 10)
- **Solution structure:** `SkillSnap.sln` → `SkillSnap.Api` + `SkillSnap.Client`

Users can register, log in, and view a personal portfolio of projects and skills. The portfolio is publicly readable; only authenticated Admin-role users may add new content. The entire application runs locally with a single SQLite database file (`skillsnap.db`).

### Three Key Features

| Feature | Where implemented | What it does |
|---------|------------------|--------------|
| **Portfolio CRUD** | `ProjectsController`, `SkillsController`, `ProjectList.razor`, `SkillTags.razor` | Creates and retrieves Projects and Skills through REST endpoints; results are shown live in Blazor components with inline add-forms |
| **Authentication & Authorization** | `AuthController`, `ApplicationUser`, `[Authorize(Roles="Admin")]`, `AuthService`, `Login.razor`, `Register.razor` | Full register/login flow using ASP.NET Identity; signed JWT returned and stored in `localStorage`; write endpoints locked to Admin role |
| **In-Memory Caching** | `ProjectsController`, `SkillsController`, `IMemoryCache` | 5-minute cache on all `GET` list endpoints; automatically invalidated on any `POST`; cache hit/miss and request duration logged via `ILogger` |

---

## 2. Development Process – Step by Step

The project was built in five sequential activities. Each activity had a defined scope and built directly on the work completed in the previous one.

### Activity 1 – Foundation: Data Models, EF Core, Static Layout

**Goal:** Establish the solution structure, define the domain model, wire up the database, and create placeholder Blazor components.

**Steps taken:**
1. Created the solution (`dotnet new sln`) and both projects (`dotnet new webapi`, `dotnet new blazorwasm`), then added them to the solution file.
2. Defined three domain model classes in `SkillSnap.Api/Models/`:
   - `PortfolioUser` — `Id`, `Name`, `Bio`, `ProfileImageUrl`, navigation lists for `Projects` and `Skills`
   - `Project` — `Id`, `Title`, `Description`, `ImageUrl`, `PortfolioUserId` (FK)
   - `Skill` — `Id`, `Name`, `Level`, `PortfolioUserId` (FK)
   - `[Key]` and `[ForeignKey]` attributes applied to enforce primary/foreign-key constraints in EF.
3. Created `SkillSnapContext : DbContext` exposing `DbSet<PortfolioUser>`, `DbSet<Project>`, `DbSet<Skill>`.
4. Registered the context in `Program.cs` with `AddDbContext<SkillSnapContext>(opt => opt.UseSqlite(...))`.
5. Ran `dotnet ef migrations add InitialCreate` and `dotnet ef database update` — EF generated the SQLite schema with FK constraints and indexes.
6. Created `SeedController` (`POST /api/seed`) that inserts one sample user with two projects and two skills when the database is empty.
7. Scaffolded three static Blazor components in `SkillSnap.Client/Components/`: `ProfileCard.razor` (name/bio/image), `ProjectList.razor` (project loop), `SkillTags.razor` (pill-style skill list).

**Outcome:** A compilable solution with a seeded SQLite database and placeholder Blazor UI.

---

### Activity 2 – API Routes and Blazor Data Binding

**Goal:** Connect the Blazor front end to the API by building real CRUD endpoints and injecting HTTP services into components.

**Steps taken:**
1. Created `ProjectsController` with `[HttpGet]` (return all) and `[HttpPost]` (add one), both using constructor-injected `SkillSnapContext`.
2. Created `SkillsController` with the same pattern for skills.
3. Added a CORS policy `"AllowClient"` in `Program.cs` targeting `https://localhost:5001`, applied with `app.UseCors()` before `MapControllers()` — required so the browser-hosted Blazor app can call the API cross-origin.
4. Created `SkillSnap.Client/Models/ApiModels.cs` with lightweight client-side `Project` and `Skill` records (no navigation properties) matching the API response shape.
5. Created `ProjectService` and `SkillService` in `SkillSnap.Client/Services/` — each wraps `HttpClient` with `GetFromJsonAsync` and `PostAsJsonAsync` calls.
6. Registered both services as `AddScoped<T>()` in the client `Program.cs`.
7. Rewrote `ProjectList.razor` and `SkillTags.razor` to `@inject` the services, load data in `OnInitializedAsync`, and re-fetch after each add-form submission.

**Outcome:** A live Blazor page that reads from and writes to the real API.

---

### Activity 3 – Authentication and Authorization

**Goal:** Secure the application with ASP.NET Identity, JWT Bearer authentication, and role-based endpoint guards; add login/register UI in Blazor.

**Steps taken:**
1. Installed `Microsoft.AspNetCore.Identity.EntityFrameworkCore` and `Microsoft.AspNetCore.Authentication.JwtBearer` NuGet packages.
2. Created `ApplicationUser : IdentityUser` (the hook point for future profile extensions).
3. Changed `SkillSnapContext` base class from `DbContext` to `IdentityDbContext<ApplicationUser>` — EF will now manage all Identity tables alongside the domain tables.
4. Registered Identity in `Program.cs`: `AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<SkillSnapContext>()`.
5. Configured JWT Bearer authentication in `Program.cs`: `AddAuthentication(...).AddJwtBearer(...)` with `TokenValidationParameters` verifying lifetime and signature; secret key read from `appsettings.json → Jwt:Key`.
6. Added `app.UseAuthentication()` and `app.UseAuthorization()` to the middleware pipeline (order matters — must be between `UseCors` and `MapControllers`).
7. Ran `dotnet ef migrations add AddIdentity` and `dotnet ef database update` — Identity tables (`AspNetUsers`, `AspNetRoles`, etc.) added to `skillsnap.db`.
8. Created `AuthController` with two endpoints:
   - `POST /api/auth/register` — calls `UserManager.CreateAsync`, returns 400 with Identity error details on failure.
   - `POST /api/auth/login` — calls `SignInManager.CheckPasswordSignInAsync` (with `lockoutOnFailure: true`); on success, builds a HS256 JWT containing `sub`, `email`, `jti` claims with an 8-hour expiry.
9. Added `[Authorize(Roles = "Admin")]` to the `POST` actions of `ProjectsController` and `SkillsController`.
10. Created `AuthService` in the Blazor client — `LoginAsync` posts credentials, stores the returned token in `localStorage` via `IJSRuntime`, and parses JWT claims to populate `UserSessionService`. `LogoutAsync` removes the token and clears session.
11. Created `Login.razor` (`/login`) and `Register.razor` (`/register`) Blazor pages with bound input fields and error/success messages.
12. Registered `AuthService` as `AddScoped<AuthService>()` in the client.

**Outcome:** Only registered users can log in; only Admin-role users can call write endpoints; unauthorized calls return HTTP 401/403.

---

### Activity 4 – Performance: Caching and State Management

**Goal:** Reduce database load with server-side caching, optimize EF queries, and introduce a Blazor state service for cross-component session sharing.

**Steps taken:**
1. Added `builder.Services.AddMemoryCache()` to the API `Program.cs`.
2. Refactored `ProjectsController.GetProjects()`:
   - Injected `IMemoryCache` and `ILogger<ProjectsController>` via constructor.
   - Wrapped the DB call with `_cache.TryGetValue("projects", out List<Project>? projects)`.
   - On cache miss: queries DB with `.AsNoTracking().Include(p => p.PortfolioUser)`, stores result with `_cache.Set(..., TimeSpan.FromMinutes(5))`, logs `"Cache miss"`.
   - On cache hit: logs `"Cache hit"` and returns immediately.
   - `AddProject` calls `_cache.Remove("projects")` after save to keep data consistent.
   - Both paths are timed with `System.Diagnostics.Stopwatch`; elapsed milliseconds written to the structured log.
3. Applied the identical pattern to `SkillsController`.
4. Created `UserSessionService` in the Blazor client — a scoped class holding `UserId`, `Role`, and `CurrentProjectTitle`; exposes `SetUser()`, `SetCurrentProject()`, `Clear()`, `IsAuthenticated`, `IsAdmin`, and an `OnChange` event for reactive components.
5. Registered `UserSessionService` as `AddScoped<UserSessionService>()` in the client.

**Outcome:** Repeated GET requests served from memory; EF never re-executes a query within the 5-minute window unless data changes. Components share user identity without redundant API calls.

---

### Activity 5 – Integration, UX Polish, and Validation

**Goal:** Wire everything together end-to-end, harden the API with input validation, polish the Blazor UI, and verify the full application flow.

**Steps taken:**
1. **Token attachment on write requests:** Updated `ProjectService.AddProjectAsync` and `SkillService.AddSkillAsync` to call `AuthService.GetTokenAsync()` and attach `Authorization: Bearer <token>` via `HttpRequestMessage.Headers.Authorization` — this is what the `[Authorize]` middleware verifies.
2. **Session restoration on reload:** Added `AuthService.InitializeAsync()` which reads the stored token from `localStorage` and re-parses its claims into `UserSessionService`. Called in `NavMenu.OnInitializedAsync` so the user stays "logged in" after a browser refresh.
3. **Input validation hardening:**
   - Added `[Required]`, `[StringLength(max, MinimumLength = min)]` to `PortfolioUser`, `Project`, and `Skill` model properties — ASP.NET Core's `[ApiController]` attribute automatically returns HTTP 400 with a structured error body if any constraint fails.
   - Added `[Required][EmailAddress]` to `RegisterRequest.Email` and `[MinLength(8)]` to `RegisterRequest.Password`; `[Required][EmailAddress]` to `LoginRequest.Email`.
   - Enabled account lockout (`lockoutOnFailure: true`); API returns HTTP 429 when the account is locked.
4. **UX overhaul:**
   - `Home.razor` rewritten to render `<ProfileCard />`, `<ProjectList />`, and `<SkillTags />` as the portfolio landing page.
   - `NavMenu.razor` updated: shows **Login / Register** links when unauthenticated; **Logout** button when authenticated; subscribes to `UserSessionService.OnChange` so it re-renders reactively.
   - `MainLayout.razor` updated: shows the logged-in `UserId` in the top bar.
5. **CSS polish:** Component-specific styles added to `app.css` — card layout for `ProfileCard`, responsive project cards with image placeholders, pill-style skill tags, and consistent input field styling.
6. **XML doc comments** added to all client services documenting each public method's purpose and access requirements.

**Outcome:** The full authentication flow (register → login → view → add with token → logout) works end-to-end. Unauthenticated write attempts are blocked at the API, and the Blazor UI reflects the login state reactively.

---

## 3. API Structure

### Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/auth/register` | Public | Register new user |
| `POST` | `/api/auth/login` | Public | Authenticate, receive JWT |
| `GET` | `/api/projects` | Public | List all projects (cached) |
| `POST` | `/api/projects` | Admin JWT | Add project (invalidates cache) |
| `GET` | `/api/skills` | Public | List all skills (cached) |
| `POST` | `/api/skills` | Admin JWT | Add skill (invalidates cache) |
| `POST` | `/api/seed` | Public | Insert sample data (once) |

### Data Persistence

EF Core manages two migrations:

| Migration | What it created |
|-----------|----------------|
| `InitialCreate` | `PortfolioUsers`, `Projects` (FK + cascade delete), `Skills` (FK + cascade delete), indexes |
| `AddIdentity` | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserTokens`, `AspNetRoleClaims`, `AspNetUserLogins` |

---

## 4. Authentication and Security Implementation

### Identity Setup
- `ApplicationUser : IdentityUser` stores all standard identity fields; extensible for future profile properties.
- Password rules: minimum 8 characters, requires uppercase, digit, and non-alphanumeric character (Identity defaults).
- Account lockout: 5 failed attempts locks the account; API returns HTTP 429.

### JWT Flow
1. Client POSTs `{ email, password }` to `/api/auth/login`.
2. API validates with `SignInManager`, builds a JWT signed with HS256 using the key from `appsettings.json`.
3. Claims embedded: `sub` (userId), `email`, `jti` (unique token ID); expiry 8 hours.
4. Blazor stores the token in `localStorage`, parses `sub` and `role` claims into `UserSessionService`.
5. Every write request attaches `Authorization: Bearer <token>`; the JWT middleware validates signature and expiry before the controller action runs.

### Authorization Layers
1. `[Authorize(Roles = "Admin")]` on every write endpoint — unauthenticated → 401, wrong role → 403.
2. CORS policy restricts cross-origin requests to `https://localhost:5001` only.
3. Input validation via `[Required]`, `[StringLength]`, `[EmailAddress]`, `[MinLength]` — invalid payloads → 400 before hitting any business logic.

---

## 5. Performance Optimization Strategies

### Server-Side Caching (IMemoryCache)
- Both `GetProjects` and `GetSkills` check the in-process cache first (`TryGetValue`).
- On a **cache miss**: EF query executes with `.AsNoTracking().Include(...)`, result stored for 5 minutes.
- On a **cache hit**: result returned immediately — zero DB round-trips.
- Write operations call `_cache.Remove(key)` to invalidate stale data.

### EF Core Query Optimization
- `.AsNoTracking()` on all read queries eliminates change-tracker allocations for data that will never be modified in the same context.
- `.Include(p => p.PortfolioUser)` performs a single SQL JOIN instead of lazy-loading related rows individually (eliminates N+1).

### Observability
- `Stopwatch` wraps every `GET` handler — elapsed time logged as a structured property, enabling comparison of cache-hit vs. cache-miss response times.
- `ILogger` messages differentiate `"Cache hit"` from `"Cache miss"` — visible in the console during `dotnet run`.

### Client-Side State
- `UserSessionService` (scoped lifetime) stores authenticated user data in Blazor memory — components read from it without issuing additional HTTP calls on every render.

---

## Known Issues / Future Improvements

- **Role assignment** — users must be manually promoted to `Admin` in the database; a dedicated admin management endpoint is not yet implemented.
- **No refresh-token** — the 8-hour JWT cannot be silently renewed; the user must re-login after expiry.
- **No DELETE / PUT endpoints** — the API currently supports create and read only; full CRUD requires adding `DELETE /api/projects/{id}` and `PUT /api/projects/{id}` with the same `[Authorize(Roles="Admin")]` guard.
- **WASM bundle size** — `System.IdentityModel.Tokens.Jwt` adds ~400 KB to the Blazor download; a minimal Base64 JSON claim parser could replace it.
- **SQLite not suitable for production** — `skillsnap.db` is a file-based store appropriate for development; a production deployment should target SQL Server or PostgreSQL with a connection string from environment variables.

