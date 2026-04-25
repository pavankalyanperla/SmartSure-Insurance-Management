# SmartSure Insurance Management System

SmartSure is a .NET 10 microservices-based insurance management system with four domain services, an Ocelot API gateway, JWT authentication, Swagger aggregation, and an admin dashboard that composes data from the domain services.

## Overview

The solution is organized as clean architecture projects per service:

- API layer for HTTP endpoints
- Application layer for business logic and DTOs
- Domain layer for entities, enums, and contracts
- Infrastructure layer for persistence and external integrations

The gateway exposes the backend through one entry point and also aggregates Swagger docs from the downstream services.

## Repository Layout

- [gateway/ApiGateway](gateway/ApiGateway) - Ocelot API gateway and Swagger aggregation
- [services/IdentityService](services/IdentityService) - authentication and user management
- [services/PolicyService](services/PolicyService) - policy creation, lookup, and premium logic
- [services/ClaimsService](services/ClaimsService) - claim lifecycle management and documents
- [services/AdminService](services/AdminService) - admin dashboard, reports, and orchestration
- [start-all-services.ps1](start-all-services.ps1) - convenience script to start the full stack

## Service Ports

- IdentityService - `http://localhost:5265`
- PolicyService - `http://localhost:5145`
- ClaimsService - `http://localhost:5084`
- AdminService - `http://localhost:5073`
- Gateway - `http://localhost:5000`

## Authentication

All services share the same JWT settings:

- SecretKey: `SmartSure@SuperSecretKey#2025!XyZ987`
- Issuer: `SmartSure`
- Audience: `SmartSureClients`

Auth behavior:

- `POST /gateway/auth/register` is public
- `POST /gateway/auth/login` is public
- All other gateway routes require a JWT with `AuthenticationProviderKey = Bearer`
- Admin endpoints require the `ADMIN` role

## How To Run

Start everything from the repository root:

```powershell
.\start-all-services.ps1
```

Skip rebuilds for faster startup:

```powershell
.\start-all-services.ps1 -NoBuild
```

Gateway Swagger UI:

- `http://localhost:5000/swagger`

## Startup Script

The root script starts these projects:

- `services/IdentityService/IdentityService.API/IdentityService.API.csproj`
- `services/PolicyService/PolicyService.API/PolicyService.API.csproj`
- `services/ClaimsService/ClaimsService.API/ClaimsService.API.csproj`
- `services/AdminService/AdminService.API/AdminService.API.csproj`
- `gateway/ApiGateway/ApiGateway.csproj`

It launches each service in its own PowerShell window and keeps them running.

## Gateway Configuration

The gateway at [gateway/ApiGateway](gateway/ApiGateway) is configured with:

- Ocelot 24.1.0
- JWT bearer authentication
- CORS policy for `http://localhost:4200`
- Serilog console logging
- SwaggerForOcelot aggregation

Swagger docs are aggregated from:

- IdentityService - `http://localhost:5265/swagger/v1/swagger.json`
- PolicyService - `http://localhost:5145/swagger/v1/swagger.json`
- ClaimsService - `http://localhost:5084/swagger/v1/swagger.json`
- AdminService - `http://localhost:5073/swagger/v1/swagger.json`

### Gateway Routes

Public auth routes:

- `POST /gateway/auth/register`
- `POST /gateway/auth/login`

Protected auth routes:

- `GET|POST|PUT|DELETE /gateway/auth/{everything}`

Policy routes:

- `GET|POST|PUT|DELETE /gateway/policies/{everything}`

Claims routes:

- `GET|POST|PUT|DELETE /gateway/claims/{everything}`

Admin routes:

- `GET|POST|PUT|DELETE /gateway/admin/{everything}`

## IdentityService

IdentityService handles user registration, login, profile lookup, and admin user management.

Implemented admin endpoints:

- `GET /api/auth/admin/users`
- `GET /api/auth/admin/users/count`
- `PUT /api/auth/admin/users/{userId}/status`

Supported operations:

- fetch all users
- count all users
- activate/deactivate a user

## PolicyService

PolicyService manages policy types, premium calculation, policy creation, policy lookup, and status updates.

Implemented admin stats endpoint:

- `GET /api/policies/admin/count`

Response includes:

- totalPolicies
- totalRevenue

Supported operations:

- list active policy types
- calculate premium
- create policy
- list the current user’s policies
- update policy status as admin

## ClaimsService

ClaimsService manages the full claim lifecycle and claim documents.

Implemented admin stats endpoint:

- `GET /api/claims/admin/stats`

Response includes:

- totalClaims
- draftClaims
- submittedClaims
- underReviewClaims
- approvedClaims
- rejectedClaims
- closedClaims

Claim lifecycle rules implemented:

- Draft -> Submitted
- Submitted -> UnderReview
- UnderReview -> Approved
- UnderReview -> Rejected
- Approved -> Closed
- Rejected -> Closed

Invalid transitions return a clear error message.

Supported operations:

- create claim
- submit claim
- get claim details
- list the current user’s claims
- list all claims for admins
- update claim status for admins
- upload claim documents

## AdminService

AdminService combines data from all services to render the admin dashboard and related admin views.

Dashboard aggregation now reads:

- totalUsers from IdentityService
- totalPolicies and totalRevenue from PolicyService
- totalClaims, submittedClaims, underReviewClaims, approvedClaims, rejectedClaims, closedClaims from ClaimsService

Derived dashboard values:

- pendingClaims = submittedClaims + underReviewClaims
- approvedClaims = approvedClaims
- rejectedClaims = rejectedClaims
- closedClaims = closedClaims

AdminService also forwards the incoming JWT to downstream services for secured calls.

## End-to-End Verification

Verified from the gateway and service side:

- all expected ports are listening
- each downstream service Swagger document is reachable
- gateway Swagger aggregation works
- public auth routes work without JWT
- protected routes return `401` without JWT
- authenticated requests succeed with a valid JWT

Authenticated smoke test completed successfully by:

- registering a test user
- logging in to get a JWT
- calling protected gateway routes with that token

## Build Status

The following projects were validated with `dotnet build`:

- IdentityService API
- PolicyService API
- ClaimsService API
- AdminService API
- ApiGateway

## Notes

- No connection strings were changed.
- No JWT settings were changed.
- No `appsettings.json` files were altered except for the gateway configuration needed for Swagger and JWT.
- No database or EF Core was added to the gateway.
- No controllers were added to the gateway.
- The gateway Swagger UI is served through Ocelot aggregation.

## Troubleshooting

If the gateway Swagger page shows downstream errors:

- confirm all four microservices are running
- confirm the ports match the values listed above
- confirm `/swagger/v1/swagger.json` is reachable for each service

- start everything again with `powershell -File start-all-services.ps1 -NoBuild`

If a protected endpoint returns `401`:

- login through the gateway first
- send the returned JWT as `Authorization: Bearer <token>`

If you want to stop everything:

- close the PowerShell windows started by the script, or press `Ctrl+C` in each terminal

## Copilot Handover (Everything Done So Far)

This section summarizes all implementation and debugging work completed in this workspace so you can restart frontend development from scratch with full backend context.

### 1) End-to-End Smoke Testing (Gateway + Direct)

- Built and ran authenticated smoke scripts:
	- [tmp_smoke_run.ps1](tmp_smoke_run.ps1)
	- [tmp_smoke_run2.ps1](tmp_smoke_run2.ps1)
- Generated side-by-side report at [smoke_side_by_side_report.txt](smoke_side_by_side_report.txt)
- Coverage validated across auth, identity-admin, policies, claims, and admin-dashboard flows.
- Final matrix result:
	- Gateway: pass=24 fail=0
	- Direct: pass=24 fail=0

### 2) API Gateway Fixes

- Fixed Swagger aggregation error "Can not add property get ... already exists" by removing a conflicting route mapping in:
	- [gateway/ApiGateway/ocelot.json](gateway/ApiGateway/ocelot.json)
- Verified gateway docs endpoints:
	- `/swagger/docs/v1/identity`
	- `/swagger/docs/v1/policy`
	- `/swagger/docs/v1/claims`
	- `/swagger/docs/v1/admin`
- Confirmed Swagger UI at `/swagger/index.html` loads successfully.

### 3) Port/Process Conflict Handling

- Diagnosed repeated "address already in use" issues.
- Standardized cleanup/restart flow for occupied ports (especially 5000 gateway).
- Verified gateway healthy after forced listener cleanup and restart.

### 4) IdentityService EF/Migration Stability

- Resolved startup crash:
	- `PendingModelChangesWarning` for `AppDbContext`.
- Root cause: stale EF model snapshot did not include `OtpVerification` even though migration existed.
- Synced snapshot in:
	- [services/IdentityService/IdentityService.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs](services/IdentityService/IdentityService.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs)
- Confirmed service startup on `http://localhost:5265` after fix.

### 5) OTP Registration Backend (IdentityService)

Implemented full OTP-backed registration API flow:

- DTO additions in:
	- [services/IdentityService/IdentityService.Application/DTOs/AuthDTOs.cs](services/IdentityService/IdentityService.Application/DTOs/AuthDTOs.cs)
- Service contract changes in:
	- [services/IdentityService/IdentityService.Application/Interfaces/IAuthService.cs](services/IdentityService/IdentityService.Application/Interfaces/IAuthService.cs)
- Repository contract + implementation for OTP storage/use:
	- [services/IdentityService/IdentityService.Domain/Interfaces/IAuthRepository.cs](services/IdentityService/IdentityService.Domain/Interfaces/IAuthRepository.cs)
	- [services/IdentityService/IdentityService.Infrastructure/Repositories/AuthRepository.cs](services/IdentityService/IdentityService.Infrastructure/Repositories/AuthRepository.cs)
- App service logic for OTP send + verify-registration:
	- [services/IdentityService/IdentityService.Application/Services/AuthService.cs](services/IdentityService/IdentityService.Application/Services/AuthService.cs)
- Controller endpoints added:
	- `POST /api/auth/send-otp`
	- `POST /api/auth/verify-register`
	- `POST /api/auth/resend-otp`
	- File: [services/IdentityService/IdentityService.API/Controllers/AuthController.cs](services/IdentityService/IdentityService.API/Controllers/AuthController.cs)
- DbContext updates for OTP entity:
	- [services/IdentityService/IdentityService.Infrastructure/Data/AppDbContext.cs](services/IdentityService/IdentityService.Infrastructure/Data/AppDbContext.cs)
- Email service DI registration:
	- [services/IdentityService/IdentityService.API/Program.cs](services/IdentityService/IdentityService.API/Program.cs)

### 6) IdentityService Package/Build Changes

- Added MailKit package to infrastructure project:
	- [services/IdentityService/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj](services/IdentityService/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj)
- Build succeeds; NU1902 advisory warnings for MailKit/MimeKit remain informational (not blocking runtime).

### 7) Frontend Work Done Before Reset (for reference)

Even if you rebuild frontend from scratch, these were completed and can be reused conceptually:

- Fixed duplicated landing intro block causing layout clumsiness.
- Added missing landing CSS structure for consistent container alignment and overflow control.
- Fixed login page rendering twice by removing duplicated login markup.
- Removed duplicate toast source on login page to avoid repeated error notifications.
- Upgraded register page to two-step OTP UX (details -> OTP verify) and wired to new backend endpoints.
- Frontend auth service endpoint alignment for OTP verification route.

Relevant files touched:

- [frontend/src/app/features/landing/landing.component.html](frontend/src/app/features/landing/landing.component.html)
- [frontend/src/styles.scss](frontend/src/styles.scss)
- [frontend/src/app/features/auth/login/login.component.html](frontend/src/app/features/auth/login/login.component.html)
- [frontend/src/app/features/auth/register/register.component.ts](frontend/src/app/features/auth/register/register.component.ts)
- [frontend/src/app/features/auth/register/register.component.html](frontend/src/app/features/auth/register/register.component.html)
- [frontend/src/app/core/services/auth.service.ts](frontend/src/app/core/services/auth.service.ts)

### 8) PolicyService Stability Fixes Done During Gateway Validation

- Fixed compile-break in admin controller caused by duplicated class content and mismatched method usage.
- Updated admin policy type controller to use `PolicyDbContext` directly for:
	- list policy types
	- create policy type
	- update policy type
	- toggle active status
	- delete policy type
	- get policy-type stats
- File:
	- [services/PolicyService/PolicyService.API/Controllers/AdminPolicyController.cs](services/PolicyService/PolicyService.API/Controllers/AdminPolicyController.cs)

### 9) Latest APIGateway Endpoint Verification (Current)

Verification executed after bringing all services up:

- IdentityService: `5265` up
- PolicyService: `5145` up
- ClaimsService: `5084` up
- AdminService: `5073` up
- ApiGateway: `5000` up

Authenticated side-by-side smoke run result from [smoke_side_by_side_report.txt](smoke_side_by_side_report.txt):

- Gateway pass=24 fail=0
- Direct pass=24 fail=0
- Failures: None

This confirms the API Gateway routes are currently functioning for all tested backend flows.

### 10) Latest Identity OTP + Email Delivery Enforcement

Additional changes were implemented after the sections above to make OTP registration strict and production-aligned:

- Registration endpoint now enforces OTP flow instead of creating users directly:
	- `POST /api/auth/register` now triggers OTP send and returns `202 Accepted` with `requiresOtpVerification=true`.
	- User creation happens only through `POST /api/auth/verify-register` after valid OTP verification.
	- File: [services/IdentityService/IdentityService.API/Controllers/AuthController.cs](services/IdentityService/IdentityService.API/Controllers/AuthController.cs)

- Development OTP fallback was removed from app service logic:
	- OTP must be delivered through configured SMTP email.
	- File: [services/IdentityService/IdentityService.Application/Services/AuthService.cs](services/IdentityService/IdentityService.Application/Services/AuthService.cs)

- SMTP configuration support was expanded:
	- Added support for `Username`, `Password`, `UseAuthentication`, `UseStartTls`.
	- Added explicit validation messages for missing/invalid email settings.
	- File: [services/IdentityService/IdentityService.Infrastructure/Services/EmailService.cs](services/IdentityService/IdentityService.Infrastructure/Services/EmailService.cs)

- Identity API configuration now includes `EmailSettings` templates in both environments:
	- [services/IdentityService/IdentityService.API/appsettings.json](services/IdentityService/IdentityService.API/appsettings.json)
	- [services/IdentityService/IdentityService.API/appsettings.Development.json](services/IdentityService/IdentityService.API/appsettings.Development.json)

- API Gateway now exposes OTP endpoints publicly (no JWT required):
	- `POST /gateway/auth/send-otp`
	- `POST /gateway/auth/verify-register`
	- `POST /gateway/auth/resend-otp`
	- File: [gateway/ApiGateway/ocelot.json](gateway/ApiGateway/ocelot.json)

- Runtime diagnostic result while testing OTP send:
	- OTP endpoints are reachable through gateway.
	- `verify-register` returns `401` for wrong OTP as expected.
	- Email send currently fails with SMTP auth error until valid credentials are configured:
		- `535 5.7.8 Username and Password not accepted`

### 11) Latest Service Operations Performed

- Started/stopped services multiple times to validate fixes and route behavior.
- Final requested state in this chat: all SmartSure services were stopped and confirmed down on ports:
	- `5000`, `5265`, `5145`, `5084`, `5073`.
