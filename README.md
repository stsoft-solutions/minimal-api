# minimal-api

A minimal, production‑ready ASP.NET Core Minimal API showcasing clean endpoint organization, request validation, OpenAPI/Scalar API reference, and structured logging with Serilog.

> Tech stack: .NET 9, ASP.NET Core Minimal APIs, Serilog, Scalar (OpenAPI UI)

---

## Badges

<!-- Replace the placeholders with your repo details -->
[![.NET](https://img.shields.io/badge/.NET-9.0-5C2D91.svg)](#)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Features

- Minimal API endpoints grouped by feature (`/payments`)
- Request validation via data annotations and custom endpoint filters
- Consistent problem+json errors, including friendly parameter binding errors (400) for invalid query/path values
- OpenAPI 3.0 with Scalar API Reference UI (enum choices and ISO `dateOnly` formats)
- Structured logging via Serilog with per-request `TraceId` enrichment
- Ready‑to‑use HTTP request samples under `http/` (JetBrains HTTP Client / VS Code REST Client)
- Optional OpenTelemetry traces/metrics export when `OTEL_EXPORTER_OTLP_ENDPOINT` is set
- Seq logging sink preconfigured (see `docker/docker-compose.yml`)

---

## Quick start

### Prerequisites

- .NET SDK 9.0 or later
- Optional: Docker (for Seq log viewer)

### Run the API (Development)

```bash
# From repo root
cd src/Sts.Minimal.Api

dotnet run
```

The API listens by default on:

- Application URL: `http://localhost:5239`
- OpenAPI document: `http://localhost:5239/openapi/v1.json`
- Scalar UI: `http://localhost:5239/scalar`

See `src/Sts.Minimal.Api/Properties/launchSettings.json` to adjust the port and environment.

### Optional: Start Seq for log viewing

```bash
# From repo root
cd docker

docker compose up -d
```

- Seq UI: `http://localhost:5340`
- Seq ingestion endpoint: `http://localhost:5341`

Serilog is already configured to send logs to Seq at `http://localhost:5341`. If your Seq instance does not require an API key (typical for local), you can remove or override the `apiKey` setting via environment variables or user secrets.

---

## API overview

Base path: `/payments`

- GET `/payments/{paymentId:int}` — Retrieve a payment by ID (Stable)
  - 200: `GetPaymentResponse`
  - 400: Validation problem
  - 404: Not found
- GET `/payments/query` — Query payments via individual query params (Experimental)
  - Query: `paymentId: int? (1..1000)`, `valueDate: yyyy-MM-dd`, `status: PaymentStatus` (accepts enum name or string alias like `FINISHED`), `referenceId: Guid?`
  - 200: `IEnumerable<GetPaymentsItem>`
- GET `/payments/query-param` — Query using a parameter object (Stable)
  - 200: `IEnumerable<GetPaymentsItem>`
- POST `/payments` — Create/process a new payment (Stable)
  - 200: `PostPaymentResponse`
  - 400: Validation problem

See `src/Sts.Minimal.Api/Features/Payment` for implementation details.

---

## Try it out (HTTP examples)

Ready‑made request files are available under `http/`. You can run them with:

- JetBrains Rider / IntelliJ HTTP Client (built‑in)
- VS Code with the REST Client extension

Examples (curl):

```bash
# Get a payment (expected 404 if not found)
curl -i "http://localhost:5239/payments/23" -H "Accept: application/json, application/problem+json"

# Prohibited ID example (expected 400 with problem+json)
curl -i "http://localhost:5239/payments/666" -H "Accept: application/json, application/problem+json"

# Successful read (example ID 1)
curl -s "http://localhost:5239/payments/1" | jq .

# Query endpoint
curl -s "http://localhost:5239/payments/query?paymentId=10&valueDate=2025-01-01&status=Completed&referenceId=9b9f6f3a-9c7e-4e75-9f3a-8a2e2d1c1d1a"

# Binding error example for query GUID (expected 400 with problem+json)
curl -i "http://localhost:5239/payments/query?referenceId=not-a-guid" -H "Accept: application/json, application/problem+json"
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

---

## Telemetry (OpenTelemetry)

Tracing and metrics via OTLP are enabled only when an OTLP endpoint is configured.

- Set environment variable `OTEL_EXPORTER_OTLP_ENDPOINT` to a valid URL, e.g.:
  - Windows PowerShell: `setx OTEL_EXPORTER_OTLP_ENDPOINT "http://localhost:4317"`
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
├─ docker/
│  └─ docker-compose.yml                # Seq log viewer
├─ http/                                 # runnable HTTP request samples
├─ src/
│  └─ Sts.Minimal.Api/
│     ├─ Features/
│     │  └─ Payment/
│     │     ├─ PaymentEndpoints.cs
│     │     ├─ Handlers/
│     │     │  ├─ GetPaymentHandler.cs
│     │     │  ├─ GetPaymentsQueryHandler.cs
│     │     │  ├─ GetPaymentsQueryAsParamHandler.cs
│     │     │  └─ PostPaymentHandler.cs
│     │     └─ Model/
│     │        ├─ GetPaymentResponse.cs
│     │        ├─ GetPaymentsItem.cs
│     │        ├─ GetPaymentsRequest.cs
│     │        ├─ PaymentStatus.cs
│     │        ├─ PostPaymentRequest.cs
│     │        └─ PostPaymentResponse.cs
│     ├─ Infrastructure/
│     │  ├─ Host/
│     │  │  └─ HostAppExtensionsAndFactory.cs
│     │  ├─ Middleware/
│     │  │  └─ BadHttpRequestToValidationHandler.cs
│     │  ├─ OpenApi/
│     │  │  ├─ OpenApiExtensions.cs
│     │  │  └─ Transformers/
│     │  │     ├─ EnumStringTransformer.cs
│     │  │     └─ IsoDateOnlyStringTransformer.cs
│     │  ├─ Serialization/
│     │  │  └─ EnumParsing.cs
│     │  └─ Validation/
│     │     ├─ Attributes/
│     │     │  ├─ StringAsEnumAttribute.cs
│     │     │  └─ StringAsIsoDateAttribute.cs
│     │     ├─ DataAnnotationsValidationFilter.cs
│     │     └─ EndpointFilterBuilderExtensions.cs
│     ├─ Program.cs
│     └─ Sts.Minimal.Api.csproj
├─ LICENSE
└─ README.md
```

---

## Development

- Target framework: `net9.0`
- Local run: `dotnet run` from `src/Sts.Minimal.Api`
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
