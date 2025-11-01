# minimal-api

A minimal, production‑ready ASP.NET Core Minimal API showcasing clean endpoint organization, request validation, OpenAPI/Scalar API reference, and structured logging with Serilog.

> Tech stack: .NET 9, ASP.NET Core Minimal APIs, Serilog, Scalar (OpenAPI UI)

---

## Badges

<!-- Replace the placeholders with your repo details -->
[![Build](https://img.shields.io/badge/build-passing-brightgreen.svg)](#)
[![.NET](https://img.shields.io/badge/.NET-9.0-5C2D91.svg)](#)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Features

- Minimal API endpoints grouped by feature (`/payments`)
- Request validation via data annotations and custom endpoint filters
- Consistent problem details for errors
- OpenAPI 3.0 specification with Scalar API Reference UI
- Structured request/response logging via Serilog
- Ready‑to‑use HTTP request samples (JetBrains HTTP Client / VS Code REST Client)

---

## Quick start

### Prerequisites

- .NET SDK 9.0 or later

### Clone and run

```bash
# From repo root
cd src/Sts.Minimal.Api

# Run the API (Development environment)
dotnet run
```

The API listens by default on:

- Application URL: `http://localhost:5239`
- OpenAPI document: `http://localhost:5239/openapi/v1.json`
- Scalar UI: `http://localhost:5239/scalar`

(See `src/Sts.Minimal.Api/Properties/launchSettings.json` to adjust.)

---

## API overview

Base path: `/payments`

- GET `/payments/{paymentId:int}` — Retrieve a payment by ID
  - 200: `GetPaymentResponse`
  - 400: Validation problem
  - 404: Not found
- GET `/payments/query` — Query payments via individual query params
  - Query: `paymentId: int? (1..1000)`, `valueDate: yyyy-MM-dd`, `status: PaymentStatus`, `referenceId: Guid?`
  - 200: `IEnumerable<GetPaymentsItem>`
- GET `/payments/query-param` — Query using a parameter object
  - 200: `IEnumerable<GetPaymentsItem>`
- POST `/payments` — Create/process a new payment
  - 200: `PostPaymentResponse`
  - 400: Validation problem

See `src/Sts.Minimal.Api/Features/Payment` for implementation details.

---

## Try it out (HTTP examples)

Ready‑made request files are available under `src/Sts.Minimal.Api/http`. You can run them with:

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
```

---

## Configuration

Configuration files:

- `src/Sts.Minimal.Api/appsettings.json`
- `src/Sts.Minimal.Api/appsettings.Development.json`

Logging is configured via Serilog (see `Infrastructure/Host/HostAppExtensionsAndFactory.cs`).

Key endpoints configured in code (see `Infrastructure/OpenApi/OpenApiExtensions.cs`):

- Map OpenAPI: `http://localhost:5239/openapi/v1.json`
- Scalar UI: `http://localhost:5239/scalar`

---

## Project structure

```
minimal-api/
├─ src/
│  └─ Sts.Minimal.Api/
│     ├─ Features/
│     │  └─ Payment/
│     │     ├─ PaymentEndpoints.cs
│     │     ├─ GetPayment.cs
│     │     ├─ GetPayments.cs
│     │     ├─ GetPaymentsParam.cs
│     │     └─ Model/
│     ├─ Infrastructure/
│     │  ├─ Host/
│     │  ├─ OpenApi/
│     │  └─ Validation/
│     ├─ http/              # runnable HTTP request samples
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
