# minimal-api

A minimal, production‑ready ASP.NET Core Minimal API showcasing clean endpoint organization, request validation, OpenAPI/Scalar API reference, and structured logging with Serilog.

> Tech stack: .NET 10, ASP.NET Core Minimal APIs, Serilog, Scalar (OpenAPI UI), JWT auth (Keycloak)

---

## Badges

<!-- Replace the placeholders with your repo details -->
[![.NET](https://img.shields.io/badge/.NET-10.0-5C2D91.svg)](#)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

> Status: README updated to reflect current project state as of 2025-11-18 (local time).

## Features

- Minimal API endpoints grouped by feature (`/payments`)
- Request validation via data annotations and custom endpoint filters
- Consistent problem+json errors, including friendly parameter binding errors (400) for invalid query/path values
- OpenAPI 3.0 with Scalar API Reference UI (enum choices and ISO `dateOnly` formats)
- JWT Bearer security integrated into OpenAPI and Scalar (Keycloak), with auth button in Scalar UI
- Structured logging via Serilog with per-request `TraceId` enrichment
- Ready‑to‑use HTTP request samples under `http/` (JetBrains HTTP Client / VS Code REST Client)
- Optional OpenTelemetry traces/metrics export when `OTEL_EXPORTER_OTLP_ENDPOINT` is set
- Seq logging sink preconfigured (see `docker/docker-compose.yml`)

---

## Quick start

### Prerequisites

- .NET SDK 10.0 or later
- Docker Desktop (required for Keycloak; also runs Seq locally)

### Run the API (Development)

#### Bash (runnable)
```bash
dotnet run --project ./src/Sts.Minimal.Api/Sts.Minimal.Api.csproj
```

Note: Authorized endpoints require Keycloak to be running. Start it with the steps in "Start local infrastructure" below.

The API listens by default on:

- Application URL: `http://localhost:5239`
- OpenAPI document: `http://localhost:5239/openapi/v1.json`
- Scalar UI: `http://localhost:5239/scalar`

See `src/Sts.Minimal.Api/Properties/launchSettings.json` to adjust the port and environment.

### Start local infrastructure (Keycloak required; Seq optional)

Both services are started from the same Docker Compose file.

#### Windows PowerShell (runnable)
```powershell
# From repo root (returns to original folder when done)
Push-Location; Set-Location .\docker; docker compose up -d; Pop-Location
```

#### Bash (runnable)
```bash
# From repo root (runs in a subshell so your cwd doesn't change)
(cd ./docker && docker compose up -d)
```

Note: PowerShell 5.x does not support Bash-style `&&` or subshell parentheses. Use the PowerShell block above when running in Windows PowerShell or PowerShell 7.

Keycloak (required):
- Admin Console: `http://localhost:8080` (admin/admin)
- Realm: `sts-realm`
- Clients:
  - `sts-api` (public client) — for password grant samples with user credentials
  - `sts-api-writer` (confidential) — client credentials, has roles: `writer`, `reader`
  - `sts-api-reader` (confidential) — client credentials, has role: `reader`
- Sample user: `api-user` / `pwd` (has roles: `reader`, `writer`)

Seq (optional):
- UI: `http://localhost:5340`
- Ingestion endpoint: `http://localhost:5341`

Serilog is preconfigured to send logs to Seq at `http://localhost:5341`. If your local Seq does not require an API key, you can remove or override the `apiKey` setting via environment variables or user secrets.

To stop the infrastructure:

#### Windows PowerShell (runnable)
```powershell
# From repo root (returns to original folder when done)
Push-Location; Set-Location .\docker; docker compose down --volumes; Pop-Location
```

#### Bash (runnable)
```bash
# From repo root (runs in a subshell so your cwd doesn't change)
(cd ./docker && docker compose down)
```

### Obtain an access token

You can use Client Credentials (recommended for service-to-service) or Password Grant (sample user).

- JetBrains HTTP Client:
  - Client Credentials: run `http/Keycloak.ClientCredentials.http` and pick writer/reader as needed
  - Password Grant: run `http/Keycloak.Token.http` (uses `api-user` / `pwd`)
- Curl examples (requires `jq`):

Writer (has roles reader+writer):
```bash
TOKEN=$(curl -s -X POST \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=sts-api-writer&client_secret=writer-secret" \
  http://localhost:8080/realms/sts-realm/protocol/openid-connect/token | jq -r '.access_token'); export TOKEN; printf "Writer token: %.20s...\n" "$TOKEN"
```

Reader only:
```bash
TOKEN=$(curl -s -X POST \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=sts-api-reader&client_secret=reader-secret" \
  http://localhost:8080/realms/sts-realm/protocol/openid-connect/token | jq -r '.access_token'); export TOKEN; printf "Reader token: %.20s...\n" "$TOKEN"
```

Optional — Password grant (sample user):
```bash
TOKEN=$(curl -s -X POST \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=sts-api&username=api-user&password=pwd&scope=openid" \
  http://localhost:8080/realms/sts-realm/protocol/openid-connect/token | jq -r '.access_token'); export TOKEN; printf "User token: %.20s...\n" "$TOKEN"
```

Then call the API with `Authorization: Bearer <token>`.

---

## API overview

Base path: `/payments`

Authorization model:
- The `/payments` route group enforces the `reader` policy by default (Bearer JWT via Keycloak required).
- Anonymous exceptions (for demo purposes):
  - GET `/payments/{paymentId:int}`
  - GET `/payments/by-reference/{referenceId:guid}`
  - GET `/payments/by-date/{date}`
- Elevated authorization:
  - POST `/payments` requires the `writer` role.

Use the access token from the steps above where authorization is required.

- GET `/payments/{paymentId:int}` — Retrieve a payment by ID (Stable, Anonymous)
  - 200: `GetPaymentResponse`
  - 400: Validation problem
  - 404: Not found
- GET `/payments/by-reference/{referenceId:guid}` — Retrieve a payment by reference ID (Stable, Anonymous)
  - 200: `GetPaymentResponse`
  - 400: Validation problem
  - 404: Not found
- GET `/payments/by-date/{date}` — Retrieve a payment by payment date (Stable, Anonymous)
  - 200: `GetPaymentResponse`
  - 400: Validation problem
  - 404: Not found
- GET `/payments/query` — Query payments via individual query params (Experimental, requires role: `reader`)
  - Query params (camelCase): `paymentId: int? (1..1000)`, `valueDateString: YYYY-MM-DD`, `status: PaymentStatus` (accepts enum name or string alias like `FINISHED`), `referenceId: Guid?`
  - 200: `IEnumerable<GetPaymentsItem>`
- GET `/payments/query-param` — Query using a parameter object (Stable, requires role: `reader`)
  - Query params (kebab-case): `payment-id`, `value-date` (YYYY-MM-DD), `status`
  - 200: `IEnumerable<GetPaymentsItem>`
- POST `/payments` — Create/process a new payment (Stable, requires role: `writer`)
  - 200: `PostPaymentResponse`
  - 400: Validation problem

See `src/Sts.Minimal.Api/Features/Payment` for implementation details.

---

## Try it out (HTTP examples)

Ready‑made request files are available under `http/`. You can run them with:

- JetBrains Rider / IntelliJ HTTP Client (built‑in)
- VS Code with the REST Client extension

The Rider/IntelliJ HTTP Client environment `http/http-client.env.json` includes three OAuth2 profiles under the `dev` environment:
- `auth-writer` — Client Credentials with `sts-api-writer` (roles: writer, reader)
- `auth-reader` — Client Credentials with `sts-api-reader` (role: reader)
- `auth-id` — Password grant with `sts-api` (user `api-user`)

Sample requests use `{{$auth.token("auth-writer")}}` for write operations and `{{$auth.token("auth-reader")}}` for read-only cases.

Examples (curl):

```bash
# Assume $TOKEN contains a valid access token (see Obtain an access token)

# Get a payment (expected 404 if not found)
curl -i "http://localhost:5239/payments/23" -H "Accept: application/json, application/problem+json"

# Prohibited ID example (expected 400 with problem+json)
curl -i "http://localhost:5239/payments/666" -H "Accept: application/json, application/problem+json"

# Successful read (example ID 1)
curl -s "http://localhost:5239/payments/1"

# Get a payment by reference (anonymous)
curl -i "http://localhost:5239/payments/by-reference/9b9f6f3a-9c7e-4e75-9f3a-8a2e2d1c1d1a" -H "Accept: application/json, application/problem+json"

# Get a payment by date (anonymous)
curl -i "http://localhost:5239/payments/by-date/2025-01-01" -H "Accept: application/json, application/problem+json"

# Query endpoint (individual params)
curl -s "http://localhost:5239/payments/query?paymentId=10&valueDateString=2025-01-01&status=FINISHED&referenceId=9b9f6f3a-9c7e-4e75-9f3a-8a2e2d1c1d1a" -H "Authorization: Bearer $TOKEN"

# Binding error example for query GUID (expected 400 with problem+json)
curl -i "http://localhost:5239/payments/query?referenceId=not-a-guid" -H "Accept: application/json, application/problem+json" -H "Authorization: Bearer $TOKEN"
```

---

## Configuration

Configuration files:

- `src/Sts.Minimal.Api/appsettings.json`
- `src/Sts.Minimal.Api/appsettings.Development.json`

Logging is configured via Serilog (see `Infrastructure/Host/HostAppExtensionsAndFactory.cs`).
- Sinks: Console and Seq (`http://localhost:5341` by default)
- To use Seq locally, run `docker/docker-compose.yml` (see Quick start) and open `http://localhost:5340`

Key endpoints configured in code (see `Infrastructure/OpenApi/OpenApiExtensions.cs`):

- Map OpenAPI: `http://localhost:5239/openapi/v1.json`
- Scalar UI: `http://localhost:5239/scalar`
- OpenAPI transformers: `JwtBearerSecuritySchemeTransformer` (adds `bearer` security scheme) and `JwtBearerOperationTransformer` (applies JWT requirement to operations)
- Scalar UI is configured to prefer JWT Bearer; use the Auth button in Scalar to paste a Keycloak token

---

## Telemetry (OpenTelemetry)

Tracing and metrics via OTLP are enabled only when an OTLP endpoint is configured.

- Set environment variable `OTEL_EXPORTER_OTLP_ENDPOINT` to a valid URL, e.g.:
  - Bash: `export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317`
- The development profile sets: `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:17011` in `Properties/launchSettings.json`. Change it to your collector address or remove it to disable OTLP.
- When not set or invalid, the app starts normally and logs: "OTLP endpoint not configured".

---

## Error handling & validation

- Data annotations and custom endpoint filter `AddDataAnnotationsValidation()` provide model/request validation for bodies and parameter objects.
- Friendly binding errors for query/path parameters: when ASP.NET Core fails to bind a value (e.g., `paymentId=abc`, `referenceId=xyz`, `valueDate=2024-06-35`), the custom exception handler `BadHttpRequestToValidationHandler` converts the `BadHttpRequestException` into a `400` response with `application/problem+json` using `ValidationProblemDetails`.
  - Registration: see `Infrastructure/OpenApi/OpenApiExtensions.cs` → `services.AddExceptionHandler<BadHttpRequestToValidationHandler>();` and `app.UseExceptionHandler();`
  - Example payload:

```json
{
  "title": "One or more parameters are invalid.",
  "status": 400,
  "errors": {
    "paymentId": ["Invalid number. Must be an integer."]
  }
}
```

Common messages include:
- Integers: "Invalid number. Must be an integer."
- GUIDs: "Invalid format. Must be a valid GUID."
- Dates: "Invalid date. Use yyyy-MM-dd."
- Booleans: "Invalid boolean. Use true or false."

---

## Project structure
```
minimal-api/
├─ docker/                       # Docker resources for local tooling (Seq, Keycloak)
│  └─ keycloak/                  # Local realm export and setup assets
├─ http/                         # Runnable HTTP request samples and environments
├─ src/
│  └─ Sts.Minimal.Api/          # Main Minimal API project
│     ├─ Features/              # Feature‑oriented endpoints and domain models
│     │  └─ Payment/            # Payment feature (endpoints, handlers, models)
│     │     ├─ Handlers/        # Endpoint handlers for payment operations
│     │     └─ Model/           # Request/response DTOs and enums
│     ├─ Infrastructure/        # Cross‑cutting concerns and utilities
│     │  ├─ Host/               # Host configuration and Serilog setup
│     │  ├─ Middleware/         # Global exception/validation middleware
│     │  ├─ OpenApi/            # OpenAPI + Scalar configuration and transformers
│     │  │  └─ Transformers/    # OpenAPI/Scalar customization helpers
│     │  ├─ Serialization/      # Helpers for parsing/formatting types
│     │  └─ Validation/         # Data annotations and endpoint filters
│     │     └─ Attributes/      # Custom validation attributes
│     ├─ Properties/            # Launch profiles and environment settings
```

---

## Development

- Target framework: `net10.0`
- Local run: `dotnet run --project src/Sts.Minimal.Api/Sts.Minimal.Api.csproj`
- HTTP logging middleware and Serilog request logging are enabled by default.

### Tests

This sample currently doesn’t include automated tests. You can introduce them with `xUnit`/`NUnit` and `WebApplicationFactory` for integration tests.

---

## Contributing

Contributions are welcome! Please:

- Open an issue first to discuss significant changes
- Follow conventional commits where possible
- Keep PRs small and focused

---

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

## Contact

- STS Support — support@stsoft.solutions

If this project helps you, consider giving it a ⭐ on GitHub.
