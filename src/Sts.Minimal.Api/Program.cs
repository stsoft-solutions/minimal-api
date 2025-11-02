using Sts.Minimal.Api.Features.Payment;
using Sts.Minimal.Api.Infrastructure.Host;
using Sts.Minimal.Api.Infrastructure.OpenApi;

// Create host builder with Serilog & OTLP
var builder = HostAppExtensionsAndFactory.CreateStsHostBuilder(args);

var services = builder.Services;

// Add OpenAPI infrastructure services
services.AddOpenApiInfrastructure();

var app = builder.Build();

app.UseStsHost();

// Developer-friendly error page in Development
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

// Use OpenAPI infrastructure middleware
app.UseOpenApiInfrastructure();

// Map payment-related API endpoints
app.MapPaymentEndpoints();

app.Run();