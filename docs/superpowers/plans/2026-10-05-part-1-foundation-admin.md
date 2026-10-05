# Children Care Part 1 Foundation and Administration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the complete English-language Part 1 account and administration module for the Children Care ASP.NET Core MVC application.

**Architecture:** A single .NET 8 MVC web application uses EF Core with SQL Server, custom `User` and `Setting` entities, secure cookie authentication, and focused services for account flows, email, tokens, menus, and avatar storage. A separate xUnit project exercises domain services and MVC integration paths against relational SQLite test databases before migrations are applied to local SQL Server.

**Tech Stack:** .NET 8, ASP.NET Core MVC, Entity Framework Core 8.0.31, SQL Server, Bootstrap, xUnit

**Spec:** `docs/superpowers/specs/2026-10-05-part-1-foundation-admin-design.md`

## Global Constraints

- Keep the deployable application on .NET 8 and implement MVC rather than Razor Pages.
- Use SQL Server `localhost` with Windows Authentication and database `ChildrenCareDb`.
- All UI copy, source identifiers, and comments are English.
- Keep `User`, `Setting`, and `RoleMenu` as the only Part 1 business entities.
- Use cookie authentication, `PasswordHasher<User>`, hashed single-use tokens, and server-side role authorization.
- Verification tokens expire after 24 hours; password-reset tokens expire after 60 minutes.
- Store avatars under `wwwroot/uploads/avatars`; accept only JPG, PNG, and WebP up to 2 MB.
- Never commit SMTP credentials or other secrets.
- Do not modify or add the unrelated `ERD_BF03.drawio` file.
- Preserve ten-row server-side pagination and soft deletion through statuses.

## Review Focus

- A deactivated or role-changed user with an existing cookie must be rejected or receive updated claims on the next request; Task 4 pins this behavior.
- Email and setting uniqueness must remain case-insensitive under both tests and SQL Server; Tasks 1 and 6 pin normalized values and duplicate handling.
- Verification and reset links must reject expired, reused, malformed, and cross-user tokens without exposing stored hashes; Task 3 pins these cases.
- Uploaded files with forged extensions, oversized content, or traversal-style names must be rejected and must not delete the previous avatar; Task 5 pins these cases.
- Search, filter, sort, and pagination query values must not permit invalid property selection or lose active query state; Tasks 6 and 7 pin allowlisted sorting and paging behavior.

---

### Task 1: MVC, Persistence, and Seed Foundation

**Files:**
- Modify: `CRUD_ChildrenCare/CRUD_ChildrenCare.csproj`
- Modify: `CRUD_ChildrenCare/Program.cs`
- Modify: `CRUD_ChildrenCare/appsettings.json`
- Modify: `CRUD_ChildrenCare/appsettings.Development.json`
- Create: `.config/dotnet-tools.json`
- Create: `CRUD_ChildrenCare/Models/Enums/Gender.cs`
- Create: `CRUD_ChildrenCare/Models/Enums/UserStatus.cs`
- Create: `CRUD_ChildrenCare/Models/Enums/SettingStatus.cs`
- Create: `CRUD_ChildrenCare/Models/Enums/SettingType.cs`
- Create: `CRUD_ChildrenCare/Models/User.cs`
- Create: `CRUD_ChildrenCare/Models/Setting.cs`
- Create: `CRUD_ChildrenCare/Models/RoleMenu.cs`
- Create: `CRUD_ChildrenCare/Data/ApplicationDbContext.cs`
- Create: `CRUD_ChildrenCare/Data/SystemData.cs`
- Create: `CRUD_ChildrenCare.Tests/CRUD_ChildrenCare.Tests.csproj`
- Create: `CRUD_ChildrenCare.Tests/Data/ApplicationDbContextTests.cs`
- Modify: `CRUD_ChildrenCare.sln`

**Interfaces:**
- Produces: `ApplicationDbContext`, the three business entities, four enums, `SystemData.RoleValues`, and EF model constraints used by every later task.

- [ ] **Step 1: Create the xUnit test project and add EF Core SQLite 8.0.31 test support**

Run `dotnet new xunit -n CRUD_ChildrenCare.Tests`, add the project reference and `Microsoft.EntityFrameworkCore.Sqlite` 8.0.31, then add the project to `CRUD_ChildrenCare.sln`.

- [ ] **Step 2: Write failing relational model tests**

Add tests named `SaveChanges_RejectsDuplicateNormalizedEmail`, `SaveChanges_RejectsDuplicateSettingTypeAndName`, `RoleMenu_RejectsDuplicateAssignment`, and `RoleDelete_IsRestrictedWhenUserExists` using an open SQLite in-memory connection.

- [ ] **Step 3: Run model tests and verify failure**

Run: `dotnet test CRUD_ChildrenCare.Tests --filter FullyQualifiedName~ApplicationDbContextTests`

Expected: FAIL because the entities and context do not exist.

- [ ] **Step 4: Add packages and implement the minimal EF model**

Reference `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.Design` 8.0.31. Implement required lengths, relationships, unique indexes, restrictive deletes, enum conversions, and UTC timestamp assignment in `ApplicationDbContext.SaveChangesAsync(CancellationToken)`.

- [ ] **Step 5: Convert application startup from Razor Pages to MVC**

Register controllers with views, `ApplicationDbContext`, cookie authorization placeholders, and static files. Configure the default MVC route and connection string `Server=localhost;Database=ChildrenCareDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true`.

- [ ] **Step 6: Add seed constants without executing the seed yet**

`SystemData` exposes the five exact role values, administration menu names/routes, and sample PostCategory and ServiceCategory rows for Task 2.

- [ ] **Step 7: Run tests and build**

Run: `dotnet test CRUD_ChildrenCare.Tests --filter FullyQualifiedName~ApplicationDbContextTests`

Expected: PASS.

Run: `dotnet build CRUD_ChildrenCare.sln`

Expected: Build succeeds with zero errors.

- [x] **Step 8: Commit**

```bash
git add .config CRUD_ChildrenCare CRUD_ChildrenCare.Tests CRUD_ChildrenCare.sln
git commit -m "feat: add mvc persistence foundation"
```

### Task 2: Validation, Secure Tokens, Initial Data, and Email

**Files:**
- Create: `CRUD_ChildrenCare/Validation/AccountValidation.cs`
- Create: `CRUD_ChildrenCare/Services/Time/IClock.cs`
- Create: `CRUD_ChildrenCare/Services/Time/SystemClock.cs`
- Create: `CRUD_ChildrenCare/Services/Security/ISecureTokenService.cs`
- Create: `CRUD_ChildrenCare/Services/Security/SecureTokenService.cs`
- Create: `CRUD_ChildrenCare/Services/Email/EmailMessage.cs`
- Create: `CRUD_ChildrenCare/Services/Email/IEmailSender.cs`
- Create: `CRUD_ChildrenCare/Services/Email/LoggingEmailSender.cs`
- Create: `CRUD_ChildrenCare/Services/Email/SmtpEmailSender.cs`
- Create: `CRUD_ChildrenCare/Options/SmtpOptions.cs`
- Create: `CRUD_ChildrenCare/Data/DatabaseSeeder.cs`
- Modify: `CRUD_ChildrenCare/Program.cs`
- Create: `CRUD_ChildrenCare.Tests/Validation/AccountValidationTests.cs`
- Create: `CRUD_ChildrenCare.Tests/Services/SecureTokenServiceTests.cs`
- Create: `CRUD_ChildrenCare.Tests/Data/DatabaseSeederTests.cs`

**Interfaces:**
- Consumes: `ApplicationDbContext`, `User`, `Setting`, `RoleMenu`, and `SystemData` from Task 1.
- Produces: `AccountValidation`, `IClock.UtcNow`, `ISecureTokenService.CreateToken()`, `ISecureTokenService.HashToken(string)`, `IEmailSender.SendAsync(EmailMessage, CancellationToken)`, and `DatabaseSeeder.SeedAsync(CancellationToken)`.

- [ ] **Step 1: Write failing validation tests**

Pin full-name trimming and Unicode letters, ten-digit mobile values beginning with zero, password length 8-50 with uppercase/lowercase/digit, and rejection of malformed email values.

- [ ] **Step 2: Write failing token tests**

Assert two generated tokens differ, tokens have sufficient entropy, hashes are deterministic, and raw tokens are never equal to stored hashes.

- [ ] **Step 3: Write failing idempotent seed tests**

Assert two calls create exactly five roles, three Part 1 menu entries, sample categories, role-menu assignments, and one active Admin. Assert the generated Admin password is emitted only on the first call and only the hash is persisted.

- [ ] **Step 4: Run focused tests and verify failure**

Run: `dotnet test CRUD_ChildrenCare.Tests --filter "FullyQualifiedName~AccountValidationTests|FullyQualifiedName~SecureTokenServiceTests|FullyQualifiedName~DatabaseSeederTests"`

Expected: FAIL because services are missing.

- [ ] **Step 5: Implement validation and security primitives**

Use compiled regular expressions where appropriate, `RandomNumberGenerator.GetBytes`, Base64 URL encoding, and SHA-256 hashing. Keep time behind `IClock` for deterministic expiry tests.

- [ ] **Step 6: Implement logging and SMTP email senders**

Use `System.Net.Mail.SmtpClient` behind `IEmailSender`. Development selects `LoggingEmailSender`; non-Development selects `SmtpEmailSender` and validates `SmtpOptions` at startup.

- [ ] **Step 7: Implement and register the idempotent database seeder**

Hash the generated Admin password with `PasswordHasher<User>` and log it once only when the Admin record is first inserted.

- [ ] **Step 8: Run focused tests and build**

Expected: all focused tests pass and the solution builds.

- [ ] **Step 9: Commit**

```bash
git add CRUD_ChildrenCare CRUD_ChildrenCare.Tests
git commit -m "feat: add validation tokens email and seed data"
```

### Task 3: Account Domain Service

**Files:**
- Create: `CRUD_ChildrenCare/Services/Accounts/AccountCommands.cs`
- Create: `CRUD_ChildrenCare/Services/Accounts/AccountResult.cs`
- Create: `CRUD_ChildrenCare/Services/Accounts/IAccountService.cs`
- Create: `CRUD_ChildrenCare/Services/Accounts/AccountService.cs`
- Create: `CRUD_ChildrenCare.Tests/Services/AccountServiceTests.cs`
- Modify: `CRUD_ChildrenCare/Program.cs`

**Interfaces:**
- Consumes: `ApplicationDbContext`, `IClock`, `ISecureTokenService`, `IEmailSender`, `PasswordHasher<User>`, and `SystemData`.
- Produces: `RegisterAsync`, `VerifyEmailAsync`, `ValidateCredentialsAsync`, `RequestPasswordResetAsync`, `ResetPasswordAsync`, and `ChangePasswordAsync` on `IAccountService` with typed command/result records.

- [ ] **Step 1: Write failing registration and verification tests**

Assert Customer/Unverified defaults, normalized email uniqueness, hashed password/token storage, a 24-hour expiry, successful activation, and rejection of malformed, expired, reused, and cross-user verification tokens.

- [ ] **Step 2: Write failing login tests**

Assert only Active accounts with correct passwords succeed and every invalid-email, invalid-password, Unverified, or Inactive case returns the same public failure message.

- [ ] **Step 3: Write failing reset/change-password tests**

Assert neutral forgot-password results, 60-minute reset expiry, single use, malformed and cross-user token rejection, correct-current-password enforcement, new/confirmation matching, and rejection when the new password equals the old password.

- [ ] **Step 4: Run tests and verify failure**

Run: `dotnet test CRUD_ChildrenCare.Tests --filter FullyQualifiedName~AccountServiceTests`

Expected: FAIL because `IAccountService` is missing.

- [ ] **Step 5: Implement the account service with transactions where token consumption changes credentials**

Use normalized email lookups, fixed public messages, `PasswordHasher<User>`, hashed-token comparison, and email messages containing caller-supplied absolute verification/reset URLs.

- [ ] **Step 6: Run tests and build**

Expected: account service tests pass and solution builds.

- [ ] **Step 7: Commit**

```bash
git add CRUD_ChildrenCare CRUD_ChildrenCare.Tests
git commit -m "feat: implement secure account workflows"
```

### Task 4: MVC Authentication and Account Screens

**Files:**
- Create: `CRUD_ChildrenCare/Security/AppClaimTypes.cs`
- Create: `CRUD_ChildrenCare/Security/ApplicationCookieEvents.cs`
- Create: `CRUD_ChildrenCare/ViewModels/Account/*.cs`
- Create: `CRUD_ChildrenCare/Controllers/AccountController.cs`
- Create: `CRUD_ChildrenCare/Views/Account/*.cshtml`
- Create: `CRUD_ChildrenCare/Views/Shared/AccessDenied.cshtml`
- Modify: `CRUD_ChildrenCare/Program.cs`
- Modify: `CRUD_ChildrenCare/Pages/_ViewImports.cshtml` to become `CRUD_ChildrenCare/Views/_ViewImports.cshtml`
- Modify: `CRUD_ChildrenCare/Pages/_ViewStart.cshtml` to become `CRUD_ChildrenCare/Views/_ViewStart.cshtml`
- Create: `CRUD_ChildrenCare.Tests/Integration/AccountControllerTests.cs`
- Create: `CRUD_ChildrenCare.Tests/Security/ApplicationCookieEventsTests.cs`

**Interfaces:**
- Consumes: `IAccountService`, `ApplicationDbContext`, and role/status models.
- Produces: MVC routes `/Account/Login`, `/Account/Register`, `/Account/VerifyEmail`, `/Account/ForgotPassword`, `/Account/ResetPassword`, `/Account/ChangePassword`, `/Account/Logout`, and `/Account/AccessDenied`.

- [ ] **Step 1: Add MVC integration-test hosting support**

Reference `Microsoft.AspNetCore.Mvc.Testing` 8.0.31 and create a test factory that replaces SQL Server with SQLite and replaces email with a recording fake.

- [ ] **Step 2: Write failing account endpoint tests**

Cover GET rendering, invalid-model redisplay, antiforgery-protected POST behavior, safe local return URLs, rejection of external return URLs, neutral forgot-password completion, and POST-only logout.

- [ ] **Step 3: Write failing cookie validation tests**

Assert an existing principal is rejected when the user becomes Inactive and its role claim is replaced when `RoleId` changes.

- [ ] **Step 4: Run tests and verify failure**

Expected: account and cookie tests fail because MVC endpoints are absent.

- [ ] **Step 5: Implement cookie configuration, claims, and per-request principal validation**

Set HttpOnly, Secure, SameSite=Lax, sliding expiration, LoginPath, and AccessDeniedPath. Query the user with role during principal validation.

- [ ] **Step 6: Implement account view models, controller actions, and English Razor views**

Use POST/Redirect/GET, TempData feedback, antiforgery validation, tag helpers, validation summaries, and URL generation for email links.

- [ ] **Step 7: Run integration tests and build**

Expected: all Task 4 tests pass and solution builds.

- [ ] **Step 8: Commit**

```bash
git add CRUD_ChildrenCare CRUD_ChildrenCare.Tests
git commit -m "feat: add mvc account screens and cookie security"
```

### Task 5: Profile and Avatar Management

**Files:**
- Create: `CRUD_ChildrenCare/Services/Files/IAvatarStorage.cs`
- Create: `CRUD_ChildrenCare/Services/Files/LocalAvatarStorage.cs`
- Create: `CRUD_ChildrenCare/Options/AvatarOptions.cs`
- Create: `CRUD_ChildrenCare/ViewModels/Profile/ProfileViewModel.cs`
- Create: `CRUD_ChildrenCare/ViewModels/Profile/EditProfileViewModel.cs`
- Create: `CRUD_ChildrenCare/Controllers/ProfileController.cs`
- Create: `CRUD_ChildrenCare/Views/Profile/Index.cshtml`
- Create: `CRUD_ChildrenCare/Views/Profile/Edit.cshtml`
- Create: `CRUD_ChildrenCare.Tests/Services/LocalAvatarStorageTests.cs`
- Create: `CRUD_ChildrenCare.Tests/Integration/ProfileControllerTests.cs`
- Modify: `CRUD_ChildrenCare/Program.cs`

**Interfaces:**
- Produces: `IAvatarStorage.SaveAsync(IFormFile, CancellationToken)` returning a web path and `DeleteAsync(string?, CancellationToken)`, plus authenticated `/Profile` routes.

- [ ] **Step 1: Write failing avatar tests**

Assert valid JPG/PNG/WebP saves with random names; empty, oversized, forged-content, unsupported-extension, and traversal-style names fail; failed replacement never deletes the previous file.

- [ ] **Step 2: Write failing profile tests**

Assert authentication is required, email remains unchanged, allowed fields update, validation errors redisplay, and successful avatar replacement deletes the old local avatar only after database save.

- [ ] **Step 3: Run tests and verify failure**

Expected: profile and avatar tests fail because storage/controller are absent.

- [ ] **Step 4: Implement avatar storage and profile MVC flow**

Inspect file signatures in addition to extension and content type. Resolve all paths beneath the configured avatar root and generate names with `Guid.NewGuid()`.

- [ ] **Step 5: Run tests and build**

Expected: Task 5 tests pass and solution builds.

- [ ] **Step 6: Commit**

```bash
git add CRUD_ChildrenCare CRUD_ChildrenCare.Tests
git commit -m "feat: add profile and avatar management"
```

### Task 6: Settings Administration

**Files:**
- Create: `CRUD_ChildrenCare/Areas/Admin/Controllers/SettingsController.cs`
- Create: `CRUD_ChildrenCare/Areas/Admin/ViewModels/Settings/*.cs`
- Create: `CRUD_ChildrenCare/Areas/Admin/Views/Settings/*.cshtml`
- Create: `CRUD_ChildrenCare/Areas/Admin/Views/_ViewImports.cshtml`
- Create: `CRUD_ChildrenCare/Areas/Admin/Views/_ViewStart.cshtml`
- Create: `CRUD_ChildrenCare/Infrastructure/PagedResult.cs`
- Create: `CRUD_ChildrenCare.Tests/Integration/Admin/SettingsControllerTests.cs`

**Interfaces:**
- Consumes: `ApplicationDbContext`, setting enums, Admin role claim.
- Produces: `/Admin/Settings` list/create/details/edit/toggle-status routes and reusable `PagedResult<T>`.

- [ ] **Step 1: Write failing Settings endpoint tests**

Cover Admin-only access, ten-row paging, case-insensitive search, Type/Status filters, allowlisted Id/Type/Name/Value/Status sorting, query-state preservation, duplicate rejection, immutable system-role values, and inactive soft deletion.

- [ ] **Step 2: Run tests and verify failure**

Expected: FAIL because the Admin Settings area is missing.

- [ ] **Step 3: Implement view models and controller queries**

Project read models with `AsNoTracking`, keep sort expressions in an explicit dictionary/switch, validate category/status selections, and catch unique-constraint races with a model error.

- [ ] **Step 4: Implement English Settings views**

Build responsive filters, table, pagination, create/edit/details forms, status badges, and explicit deactivate/reactivate confirmation forms.

- [ ] **Step 5: Run tests and build**

Expected: Task 6 tests pass and solution builds.

- [ ] **Step 6: Commit**

```bash
git add CRUD_ChildrenCare CRUD_ChildrenCare.Tests
git commit -m "feat: add settings administration"
```

### Task 7: User Administration

**Files:**
- Create: `CRUD_ChildrenCare/Services/Security/IPasswordGenerator.cs`
- Create: `CRUD_ChildrenCare/Services/Security/PasswordGenerator.cs`
- Create: `CRUD_ChildrenCare/Areas/Admin/Controllers/UsersController.cs`
- Create: `CRUD_ChildrenCare/Areas/Admin/ViewModels/Users/*.cs`
- Create: `CRUD_ChildrenCare/Areas/Admin/Views/Users/*.cshtml`
- Create: `CRUD_ChildrenCare.Tests/Services/PasswordGeneratorTests.cs`
- Create: `CRUD_ChildrenCare.Tests/Integration/Admin/UsersControllerTests.cs`
- Modify: `CRUD_ChildrenCare/Program.cs`

**Interfaces:**
- Consumes: `ApplicationDbContext`, `IEmailSender`, `PasswordHasher<User>`, `PagedResult<T>`, and active UserRole settings.
- Produces: `IPasswordGenerator.Generate()` and `/Admin/Users` list/create/details/edit/toggle-status routes.

- [ ] **Step 1: Write failing password-generator tests**

Assert generated values are 16 characters, satisfy all password rules, and differ across repeated calls.

- [ ] **Step 2: Write failing Users endpoint tests**

Cover Admin-only access, paging, FullName/Email/Mobile search, Gender/Role/Status filtering, allowlisted sorting, normalized-email uniqueness, active-role selection, generated-password hashing/email, immutable email/profile fields during admin edit, and inactive soft deletion.

- [ ] **Step 3: Run tests and verify failure**

Expected: FAIL because password generation and Users administration are missing.

- [ ] **Step 4: Implement password generation and user queries/commands**

Use `RandomNumberGenerator` and guarantee each required character class. Never display or persist the generated plain password outside the outbound email message.

- [ ] **Step 5: Implement English Users views**

Include responsive filters/table, avatar details, role/status edit form, creation form, and deactivate/reactivate confirmation.

- [ ] **Step 6: Run tests and build**

Expected: Task 7 tests pass and solution builds.

- [ ] **Step 7: Commit**

```bash
git add CRUD_ChildrenCare CRUD_ChildrenCare.Tests
git commit -m "feat: add user administration"
```

### Task 8: Role Authorization and Dynamic Administration Menu

**Files:**
- Create: `CRUD_ChildrenCare/Areas/Admin/Controllers/AuthorizationController.cs`
- Create: `CRUD_ChildrenCare/Areas/Admin/ViewModels/Authorization/RoleMenuAssignmentViewModel.cs`
- Create: `CRUD_ChildrenCare/Areas/Admin/Views/Authorization/Index.cshtml`
- Create: `CRUD_ChildrenCare/Services/Menus/IAdminMenuService.cs`
- Create: `CRUD_ChildrenCare/Services/Menus/AdminMenuService.cs`
- Create: `CRUD_ChildrenCare/ViewComponents/AdminMenuViewComponent.cs`
- Create: `CRUD_ChildrenCare/Views/Shared/Components/AdminMenu/Default.cshtml`
- Create: `CRUD_ChildrenCare.Tests/Integration/Admin/AuthorizationControllerTests.cs`
- Create: `CRUD_ChildrenCare.Tests/Services/AdminMenuServiceTests.cs`
- Modify: `CRUD_ChildrenCare/Program.cs`

**Interfaces:**
- Produces: Admin role-menu assignment endpoint, `IAdminMenuService.GetForRoleAsync(string, CancellationToken)`, and `AdminMenuViewComponent`.

- [ ] **Step 1: Write failing authorization and menu tests**

Assert only Admin can edit assignments, only active UserRole/AdminMenu rows are accepted, duplicate submitted IDs collapse safely, stale assignments are removed transactionally, inactive entries never render, and menu order is deterministic.

- [ ] **Step 2: Run tests and verify failure**

Expected: FAIL because authorization management and menu service are absent.

- [ ] **Step 3: Implement assignment transaction and menu projection**

Replace assignments for one role in one transaction. Query only active menu settings assigned to the current role and order by name then ID.

- [ ] **Step 4: Implement Authorization view and menu ViewComponent**

Render role selection and menu checkboxes in English. Use the component in the Admin layout while preserving server-side `[Authorize]` checks.

- [ ] **Step 5: Run tests and build**

Expected: Task 8 tests pass and solution builds.

- [ ] **Step 6: Commit**

```bash
git add CRUD_ChildrenCare CRUD_ChildrenCare.Tests
git commit -m "feat: add role menu authorization"
```

### Task 9: Shared Layout, Error Pages, and Visual Polish

**Files:**
- Modify: `CRUD_ChildrenCare/Views/Shared/_Layout.cshtml`
- Create: `CRUD_ChildrenCare/Areas/Admin/Views/Shared/_AdminLayout.cshtml`
- Create: `CRUD_ChildrenCare/Controllers/HomeController.cs`
- Create: `CRUD_ChildrenCare/Views/Home/Index.cshtml`
- Create: `CRUD_ChildrenCare/Views/Home/Error.cshtml`
- Create: `CRUD_ChildrenCare/Views/Home/NotFound.cshtml`
- Modify: `CRUD_ChildrenCare/wwwroot/css/site.css`
- Modify: `CRUD_ChildrenCare/wwwroot/js/site.js`
- Create: `CRUD_ChildrenCare.Tests/Integration/LayoutAndErrorTests.cs`

**Interfaces:**
- Consumes: account/profile/admin routes and `AdminMenuViewComponent`.
- Produces: coherent public/account/admin layouts and status-code/error endpoints.

- [ ] **Step 1: Write failing layout and error tests**

Assert anonymous/authenticated navigation differences, safe 403/404 pages, production exception handling route, and presence of responsive viewport/form/table hooks without leaking exception details.

- [ ] **Step 2: Run tests and verify failure**

Expected: FAIL because final layouts and error routes are missing.

- [ ] **Step 3: Implement layouts and status-code handling**

Use Bootstrap already vendored in `wwwroot`, render TempData feedback safely, provide accessible labels/focus states, and avoid external UI dependencies.

- [ ] **Step 4: Apply original responsive styling**

Create a restrained Children Care visual system for account cards, Admin navigation, tables, badges, pagination, empty states, and mobile breakpoints.

- [ ] **Step 5: Run tests and build**

Expected: Task 9 tests pass and solution builds.

- [ ] **Step 6: Commit**

```bash
git add CRUD_ChildrenCare CRUD_ChildrenCare.Tests
git commit -m "feat: polish layouts and error handling"
```

### Task 10: SQL Server Migration and End-to-End Verification

**Execution status:** Complete on 2026-10-05. `ChildrenCareDb` was migrated and seeded on the local default SQL Server instance; automated and HTTP smoke checks passed. Windows browser automation was attempted but unavailable because the Computer Use native helper pipe was not running, so visual behavior was verified through rendered MVC integration tests and responsive markup checks.

**Files:**
- Create: `CRUD_ChildrenCare/Migrations/*`
- Create: `README.md`
- Modify: `docs/superpowers/plans/2026-10-05-part-1-foundation-admin.md`

**Interfaces:**
- Consumes: the complete application from Tasks 1-9.
- Produces: reproducible schema creation, verified local database, and developer run/configuration instructions.

- [x] **Step 1: Restore the local EF tool and create the initial migration**

Run: `dotnet tool restore`

Run: `dotnet ef migrations add InitialPart1 --project CRUD_ChildrenCare --startup-project CRUD_ChildrenCare`

Expected: migration files describe Users, Settings, RoleMenus, constraints, and indexes.

- [x] **Step 2: Run the complete automated test suite**

Run: `dotnet test CRUD_ChildrenCare.sln`

Expected: all tests pass.

- [x] **Step 3: Apply the migration to local SQL Server**

Run: `dotnet ef database update --project CRUD_ChildrenCare --startup-project CRUD_ChildrenCare`

Expected: `ChildrenCareDb` is created successfully on `localhost`.

- [x] **Step 4: Start the application and capture the one-time Admin credential from Development logs**

Run: `dotnet run --project CRUD_ChildrenCare --no-build`

Expected: application starts, seed completes, and the temporary Admin credential is logged once.

- [x] **Step 5: Perform HTTP and rendered-layout smoke checks**

Verify public registration/login pages, authenticated profile and password flows, Admin Users/Settings/Authorization screens, redirect-to-login behavior, 403 behavior, validation messages, filtering/sorting/pagination, and responsive layouts. Confirm verification/reset links from Development email logs work once and then fail safely.

- [x] **Step 6: Add setup and SMTP configuration instructions**

Document prerequisites, connection string, migration commands, Development email behavior, User Secrets keys for SMTP, avatar path, Admin bootstrap behavior, and test commands without including any secret value.

- [x] **Step 7: Re-run final verification**

Run: `dotnet test CRUD_ChildrenCare.sln`

Expected: all tests pass.

Run: `dotnet build CRUD_ChildrenCare.sln --no-restore`

Expected: build succeeds with zero errors.

Run: `git diff --check`

Expected: no whitespace errors.

- [ ] **Step 8: Commit**

```bash
git add CRUD_ChildrenCare CRUD_ChildrenCare.Tests README.md docs/superpowers/plans/2026-10-05-part-1-foundation-admin.md .config CRUD_ChildrenCare.sln
git commit -m "feat: complete part 1 foundation and administration"
```
