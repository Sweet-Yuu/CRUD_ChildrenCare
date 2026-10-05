# Children Care - Part 1

ASP.NET Core MVC application for account, profile, administration, settings, and role-menu authorization workflows. The UI and application messages are in English.

## Prerequisites

- .NET 8 SDK
- SQL Server running on `localhost` with Windows Authentication
- SQL Server Management Studio (SSMS) 21 or another SQL client (optional)

The default database is `ChildrenCareDb`. The committed connection string uses Windows Authentication and trusts the local development certificate:

```text
Server=localhost;Database=ChildrenCareDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Override it without editing committed configuration:

```powershell
$env:ConnectionStrings__DefaultConnection = "your connection string"
```

## Restore, migrate, and run

```powershell
dotnet tool restore
dotnet restore
dotnet ef database update --project CRUD_ChildrenCare --startup-project CRUD_ChildrenCare
dotnet run --project CRUD_ChildrenCare --launch-profile https
```

The application also applies pending migrations and seeds required data at startup. Set `Database:InitializeOnStartup` to `false` if deployment migrations are managed separately.

Use the HTTPS launch profile because authentication cookies are intentionally marked `Secure`.

Seed data includes the five system roles, administration menus, sample categories, role-menu assignments, and the initial administrator:

```text
admin@childrencare.local
```

On the first seed only, a strong temporary password is generated and written inside the Development email log. It is never stored as plain text or displayed again. Change it immediately after signing in.

## Email configuration

Development uses the logging email sender by default. Verification links, reset links, and generated temporary credentials appear in structured application logs.

To test SMTP locally, store settings with .NET User Secrets and enable the SMTP provider:

```powershell
dotnet user-secrets set "Email:UseSmtp" "true" --project CRUD_ChildrenCare
dotnet user-secrets set "Smtp:Host" "smtp.example.com" --project CRUD_ChildrenCare
dotnet user-secrets set "Smtp:Port" "587" --project CRUD_ChildrenCare
dotnet user-secrets set "Smtp:EnableSsl" "true" --project CRUD_ChildrenCare
dotnet user-secrets set "Smtp:UserName" "your-user" --project CRUD_ChildrenCare
dotnet user-secrets set "Smtp:Password" "your-password" --project CRUD_ChildrenCare
dotnet user-secrets set "Smtp:FromEmail" "no-reply@example.com" --project CRUD_ChildrenCare
dotnet user-secrets set "Smtp:FromName" "Children Care" --project CRUD_ChildrenCare
```

Production uses SMTP automatically. Supply the same values through environment variables or the deployment secret store, for example `Smtp__Host` and `Smtp__Password`. Never commit SMTP credentials.

## Avatar storage

Profile avatars are stored under `CRUD_ChildrenCare/wwwroot/uploads/avatars` with randomized file names. JPG, PNG, and WebP images up to 2 MB are accepted. Configure the directory and limit with `Avatar:UploadDirectory` and `Avatar:MaximumBytes`.

The upload directory is created automatically. In multi-instance or cloud deployments, replace local storage with shared durable storage before scaling out.

## Tests

```powershell
dotnet test CRUD_ChildrenCare.sln --nologo
dotnet build CRUD_ChildrenCare.sln --no-restore --nologo
```

The test suite covers persistence constraints, validation, token expiry and one-time use, authentication, profile/avatar handling, administration CRUD, authorization assignments, layouts, and safe error responses.

## Main routes

- `/` - public home
- `/Account/Login`, `/Account/Register`, `/Account/ForgotPassword`
- `/Profile`, `/Profile/Edit`
- `/Admin/Users`
- `/Admin/Settings`
- `/Admin/Authorization`

All state-changing forms use antiforgery protection. Administration endpoints require the Admin role independently of menu visibility.
