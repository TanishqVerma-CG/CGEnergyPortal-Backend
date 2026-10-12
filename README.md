# BP Industrial Portal Middleware

The BP Industrial Portal Middleware is the ASP.NET Core Web API that serves the BP Industrial Portal frontend. It provides authenticated APIs for customer self-service and broker operations, including account management, contracts, renewals, billing, payments, metering and usage, support cases, user details, notifications, pricing alerts, and broker commissions.

## Solution Overview

```text
BPIndustrialPortal/
	BPIndustrialPortal.sln  .NET solution
	BP/                    Web API project
		Controllers/         HTTP endpoint definitions
		Services/            Domain and XML data operations
		IService/            Service contracts
		Models/              Request and response models
		Data/                XML data files copied to build output
		Helpers/             JWT, notification, and supporting helpers
	BP.Tests/              xUnit unit and integration tests
postman/                 API collections for broker features
```

The API uses controller-based, explicitly attributed routes, primarily below `/api`. Domain services are registered through dependency injection and operate on XML data sources packaged with the application. The app also exposes an in-memory Hangfire server and dashboard for background work.

## Technology

- .NET 9 and ASP.NET Core Web API
- JWT bearer authentication and role-based authorization
- Hangfire with in-memory storage
- XML-backed application data
- MailKit for SMTP delivery
- ClosedXML for spreadsheet generation and export
- OpenAPI document generation in Development
- xUnit, Moq, FluentAssertions, and `WebApplicationFactory` for tests

## Prerequisites

- .NET 9 SDK
- A supported local HTTPS development certificate when using the HTTPS profile
- Configuration values appropriate for the environment

Confirm the installed SDK:

```powershell
dotnet --version
```

## Run Locally

From this repository root:

```powershell
dotnet restore .\BPIndustrialPortal\BPIndustrialPortal.sln
dotnet run --project .\BPIndustrialPortal\BP\BP.csproj --launch-profile http
```

The HTTP launch profile listens on `http://localhost:5128`. The HTTPS launch profile additionally uses `https://localhost:7023`:

```powershell
dotnet run --project .\BPIndustrialPortal\BP\BP.csproj --launch-profile https
```

For local frontend integration, use `http://localhost:5128` as `NEXT_PUBLIC_API_BASE_URL` in the frontend and keep `http://localhost:3000` in the API's allowed CORS origins.

## Configuration

Configuration is loaded through ASP.NET Core's standard providers. `appsettings.json` defines the baseline settings, `appsettings.Development.json` supplies development overrides, and environment variables can override configuration at deployment time.

| Section or variable | Purpose |
| --- | --- |
| `Cors:AllowedOrigins` | Explicit origins allowed to call the API with credentials. |
| `CORS_ALLOWED_ORIGINS` | Comma-separated additional origins merged with configured CORS origins. |
| `Jwt:Key` | Symmetric signing key for JWT validation. |
| `Jwt:Issuer` | Expected JWT issuer. |
| `Jwt:Audience` | Expected JWT audience. |
| `Jwt:ExpiryInMinutes` | Token expiry used by the authentication service. |
| `Smtp` | SMTP host, port, username, password, and sender configuration. |
| `AppSettings:FrontendBaseUrl` | Public frontend base URL used in application flows such as emailed links. |
| `AppSettings:AppName` | Application identifier used by supporting services. |

Use environment variables for production secrets, for example `Jwt__Key`, `Smtp__Password`, and `AppSettings__FrontendBaseUrl`. Do not commit live JWT keys, SMTP passwords, or provider credentials. Any secret already committed to a repository must be treated as exposed and rotated.

## API Behavior

Authentication is configured with JWT bearer tokens. Tokens are validated for issuer, audience, lifetime, and signing key, with no clock-skew allowance. Authorization policies include `Billing` and `Broker` role requirements; endpoint-level authorization determines which operations require them.

The API surface is organized by business capability:

| Area | Representative route group |
| --- | --- |
| Authentication and recovery | `/api/Login`, `/api/ForgotPassword`, `/api/ResetPassword` |
| Account and user management | `/api/GetAccountManagementPage`, `/api/InviteUser`, `/api/UpdateUserDetail` |
| Contracts and renewals | `/api/GetContractSummary`, `/api/RequestContractRenewal` |
| Billing and payments | `/api/GetBillingSummary`, `/api/MakePayment` |
| Metering and usage | `/api/GetMeterSummary`, `/api/GetContractMeterSummary` |
| Support and notifications | `/api/GetSupportCases`, `/api/GetNotifications` |
| Broker workflows | `/api/broker/home/*`, `/api/broker/renewals/*`, `/api/broker/commission/*` |

In the Development environment, the OpenAPI document is mapped by ASP.NET Core. The Hangfire dashboard is available at `/hangfire`; protect or restrict that endpoint appropriately before any non-local deployment.

## Data and Background Work

The `BP/Data` XML files are content assets and are copied to the build output on every build. This means local behavior and deployed behavior depend on those files being present and writable only when the relevant feature requires it. Treat XML format changes as API-impacting data-contract changes and validate them through the affected service and controller tests.

Hangfire is configured with `Hangfire.MemoryStorage`. In-memory jobs and state are not durable across process restarts or scaled-out instances. Use persistent Hangfire storage before relying on background jobs for production-critical or recoverable processing.

## Test

Run the complete suite from this repository root:

```powershell
dotnet test .\BPIndustrialPortal\BP.Tests\BP.Tests.csproj
```

Useful focused commands:

```powershell
dotnet test .\BPIndustrialPortal\BP.Tests\BP.Tests.csproj --filter "FullyQualifiedName~Unit"
dotnet test .\BPIndustrialPortal\BP.Tests\BP.Tests.csproj --filter "FullyQualifiedName~Integration"
dotnet test .\BPIndustrialPortal\BP.Tests\BP.Tests.csproj --collect:"XPlat Code Coverage"
```

Unit tests isolate services with temporary XML data. Integration tests host the complete ASP.NET Core pipeline in memory through `WebApplicationFactory<Program>`, including dependency injection, authentication, authorization, and controllers. The integration environment disables HTTPS redirection so tests do not require a live web server.

## Postman Collections

The [`postman`](postman) directory contains collections for broker APIs, broker commission APIs, and broker home APIs. Import the appropriate collection into Postman, set the target API base URL, and supply a valid bearer token for protected endpoints.

## Operational Checks

Before deploying, verify the following:

- `dotnet restore`, `dotnet build`, and `dotnet test` complete successfully.
- CORS contains only approved frontend origins and uses HTTPS outside local development.
- JWT and SMTP secrets are injected from protected configuration, not source-controlled files.
- The frontend uses the intended API base URL.
- XML data files are included in the deployment artifact.
- The Hangfire dashboard is secured and background-job persistence matches reliability requirements.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Frontend requests are blocked by CORS | Add the exact frontend origin to `Cors:AllowedOrigins` or `CORS_ALLOWED_ORIGINS`; do not use a wildcard with credentials. |
| Requests return `401 Unauthorized` | Verify bearer token validity, issuer, audience, signing key, and expiration. |
| Requests return `403 Forbidden` | Confirm the authenticated user has the role required by the endpoint policy. |
| XML-backed endpoint fails after deployment | Confirm the expected files from `BP/Data` were included in the application output. |
| Background jobs disappear after restart | This is expected with in-memory Hangfire storage; configure persistent storage for durable jobs. |
