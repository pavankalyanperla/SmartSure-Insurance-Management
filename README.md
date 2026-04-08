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
