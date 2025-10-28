using Sts.Minimal.Api.Features.Payment;
using Sts.Minimal.Api.Infrastructure.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add OpenAPI infrastructure services
services.AddOpenApiInfrastructure();

var app = builder.Build();

// Use OpenAPI infrastructure middleware
app.UseOpenApiInfrastructure();

// Map payment-related API endpoints
app.MapPaymentEndpoints();

app.Run();