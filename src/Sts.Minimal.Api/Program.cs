using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Sts.Minimal.Api.Features.Payment;
using Sts.Minimal.Api.Infrastructure.Host;
using Sts.Minimal.Api.Infrastructure.OpenApi;

// Create host builder with Serilog & OTLP
var builder = HostAppExtensionsAndFactory.CreateStsHostBuilder(args);

var services = builder.Services;

// Add OpenAPI infrastructure services
services.AddOpenApiInfrastructure();

// Authentication & Authorization
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var cfg = builder.Configuration;
        options.Authority = cfg["Auth:Authority"]; // e.g. http://localhost:8080/realms/sts-realm
        if (builder.Environment.IsDevelopment())
        {
            options.RequireHttpsMetadata = false; // dev only
        }
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = builder.Environment.IsDevelopment() ? false : true // Only disable in development
        };
    });
services.AddAuthorization();

var app = builder.Build();

app.UseStsHost();

// Developer-friendly error page in Development
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

// Auth middlewares
app.UseAuthentication();
app.UseAuthorization();

// Use OpenAPI infrastructure middleware
app.UseOpenApiInfrastructure();

// Map payment-related API endpoints
app.MapPaymentEndpoints();

app.Run();