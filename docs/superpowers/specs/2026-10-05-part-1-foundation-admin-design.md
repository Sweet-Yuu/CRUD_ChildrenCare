# Children Care Part 1 Foundation and Administration Design

## Purpose

Part 1 establishes the account, authentication, authorization, user administration, and application-setting foundation required by all later Children Care modules. The implementation will replace the empty Razor Pages starter flow in the main `CRUD_ChildrenCare` project with an ASP.NET Core MVC application on .NET 8.

The completed feature includes screens 10 through 15 and 32 through 35 from the supplied specification. All user-facing text is English. Source-code identifiers and comments are also English.

## Scope

The implementation includes:

- User login, logout, registration, and email verification
- Forgot-password and reset-password flows
- Authenticated password changes
- User profile viewing and editing
- Avatar upload and replacement
- Cookie authentication and role-based server authorization
- Dynamic administration menus assigned to roles
- Administrative user creation, listing, filtering, sorting, details, role/status updates, and deactivation
- Administrative setting creation, listing, filtering, sorting, details, updates, and deactivation
- Seed data for system roles, administration menus, sample categories, and an initial administrator
- Development email delivery through structured logs and production-ready SMTP configuration
- SQL Server persistence through Entity Framework Core migrations
- Automated validation, authentication, authorization, and CRUD tests

Other application modules and the unrelated `ERD_BF03.drawio` file are out of scope.

## Platform and Project Structure

- Runtime: .NET 8
- Web framework: ASP.NET Core MVC
- Persistence: Entity Framework Core with SQL Server
- Database server: the local default SQL Server instance at `localhost`
- Database: `ChildrenCareDb`
- Authentication: ASP.NET Core cookie authentication
- Password hashing: `PasswordHasher<User>`
- UI: responsive Bootstrap without an external theme dependency

The solution keeps one deployable web project and adds a dedicated automated-test project. The web project uses focused folders for models, data access, view models, services, controllers, authorization, validation helpers, and MVC views. Interfaces are introduced only where multiple implementations or isolation for tests is required, particularly email delivery, token generation, current time, and avatar storage.

## Data Model

### Setting

`Setting` stores configurable values with the fields defined by the source specification:

- `Id`
- `Type`
- `Name`
- `Value`
- `Description`
- `Status`

`Type` supports `PostCategory`, `ServiceCategory`, `UserRole`, and `AdminMenu`. The pair `(Type, Name)` is unique. Inactive settings remain available to existing records but are excluded from new selections and dynamic menus.

The five system roles are Customer, Doctor, Nurse, Manager, and Admin. Their `Value` fields cannot be changed after seeding.

### User

`User` stores:

- `Id`
- `FullName`
- `Gender`
- `Email`
- `NormalizedEmail`
- `Mobile`
- `Address`
- `AvatarUrl`
- `PasswordHash`
- `RoleId`
- `Status`
- Verification-token hash and expiry
- Reset-token hash and expiry
- `CreatedDate`
- `UpdatedDate`

Email uniqueness is enforced through `NormalizedEmail`. The application never stores raw verification or reset tokens. Public registration assigns the Customer role and Unverified status. The email is immutable after account creation.

### RoleMenu

`RoleMenu` is the approved join entity between a `UserRole` setting and an `AdminMenu` setting. A unique constraint prevents duplicate assignments. It controls menu visibility only; controller and action authorization remains authoritative.

### Database Conventions

- Primary keys are integer identities.
- Business entities use status changes instead of hard deletion.
- Created and updated timestamps are assigned by the application in UTC.
- Referential delete behavior is restrictive for roles and menu settings that are in use.
- SQL Server is accessed with Windows Authentication.

## Authentication and Security

Login accepts email and password and returns a generic failure message for invalid credentials. Unverified and inactive accounts cannot sign in. A successful sign-in creates an encrypted authentication cookie containing the user ID, email, display name, and role value.

The application validates return URLs before redirecting. Unauthenticated users are redirected to Login. Authenticated users without permission receive the Access Denied page with HTTP 403 semantics.

Passwords must be 8 to 50 characters and include an uppercase letter, lowercase letter, and digit. Tokens use a cryptographically secure random source. Verification tokens expire after 24 hours; password-reset tokens expire after 60 minutes. A token is invalidated immediately after successful use.

All state-changing MVC forms use antiforgery validation. Uploaded file content, extension, size, and generated file name are validated on the server. User-provided return URLs and paths are never trusted directly.

## Account Flows

### Registration and Verification

The registration form collects full name, gender, email, mobile, optional address, password, and password confirmation. The application creates an Unverified Customer and sends a verification link. Development mode writes the email body and verification URL to application logs.

Opening a valid verification link activates the account and consumes the token. Invalid, expired, and previously used links show a safe error without exposing stored token data.

### Login and Logout

Login issues the authentication cookie only for active, verified accounts. The post-login destination is selected by the validated return URL or the user's role. Logout invalidates the cookie through a POST action.

### Forgot and Reset Password

The forgot-password endpoint always shows the same completion message, regardless of whether an email exists. Eligible users receive a single-use reset link. Resetting replaces the password hash and consumes the reset token.

### Change Password

Authenticated users provide the current password, new password, and confirmation. The current password must match, and the new password must differ from it.

### Profile and Avatar

Users can edit full name, gender, mobile, address, and avatar. Email is read-only. Avatars are stored under `wwwroot/uploads/avatars` with random file names. JPG, PNG, and WebP files up to 2 MB are accepted. An old local avatar is deleted only after its replacement has been saved successfully.

## Administration Flows

### Users

Admin users can:

- List users with ten records per page
- Search by full name, email, or mobile
- Filter by gender, role, and status
- Sort by the specified columns
- View full details
- Create an account with a generated password
- Update only role and status
- Deactivate an account instead of deleting it

Creating a user sets Active status and sends the generated password through the configured email sender. In Development, the generated credential appears in the email log. Admin cannot modify a user's email or profile fields through the administration edit screen.

### Settings

Admin users can create, view, update, search, filter, sort, and deactivate settings. Duplicate `(Type, Name)` values are rejected. The `Value` of the five system roles is immutable. Inactive settings do not appear in new-selection dropdowns or menus.

### Authorization and Menus

Admin users assign active AdminMenu settings to active UserRole settings through `RoleMenu`. The administration layout reads these assignments to render navigation. Server authorization policies independently protect every administration action.

## Initial Data

The database seed includes:

- Customer, Doctor, Nurse, Manager, and Admin roles
- Administration menu entries for Users, Settings, and Authorization
- Representative PostCategory and ServiceCategory values for later modules
- Role-menu assignments appropriate to Part 1
- An initial administrator with email `admin@childrencare.local`

On the first seed only, the application generates a strong administrator password, stores only its hash, and writes the temporary credential once to Development logs. Subsequent starts do not regenerate or redisplay it.

## Email Delivery

`IEmailSender` has two implementations:

- Development logging sender: writes recipient, subject, body, and links to structured logs
- SMTP sender: reads host, port, TLS mode, user name, password, and sender identity from configuration populated by User Secrets or environment variables

No SMTP password or other secret is committed to the repository.

## Validation and Error Handling

Validation follows the supplied specification for names, email, Vietnamese mobile numbers, password strength, address lengths, setting lengths, and file uploads. View models contain screen-specific rules, while domain services enforce rules that must hold outside MVC model binding.

Duplicate database values, expired tokens, missing records, invalid transitions, and upload failures produce safe user-facing messages. Unexpected exceptions go through the production error handler and are logged without exposing stack traces or secrets.

## User Interface

The UI uses an original, responsive Bootstrap design with shared typography, navigation, forms, tables, pagination, validation summaries, status badges, and feedback messages. Public account pages and the administration area have distinct layouts but share the same visual system. No external admin template is required.

Lists preserve search, filter, sort, and page state in query strings. Empty states, destructive-status confirmations, and loading-safe form behavior are included.

## Testing and Verification

Automated tests cover:

- Shared validation rules
- Password hashing and strength requirements
- Token creation, hashing, expiry, and one-time use
- Registration, login, verification, forgot/reset, and password change flows
- Role and status authorization behavior
- User administration restrictions
- Setting uniqueness and system-role protection
- Role-menu assignment behavior
- Avatar validation and storage behavior

Final verification includes:

- Applying migrations to the local SQL Server instance
- Confirming seed data and the initial administrator
- Running all automated tests
- Running `dotnet build` without errors
- Performing HTTP smoke tests for public, authenticated, and forbidden routes
- Checking the responsive layouts and key forms through the running application

## Completion Criteria

Part 1 is complete when all ten specified screens and supporting flows are available in English, persistence and seed data work against `ChildrenCareDb`, authorization is enforced server-side, the Development email workflow is testable without SMTP credentials, and the build and automated verification pass.
