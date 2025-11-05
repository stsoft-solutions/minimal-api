using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Sts.Minimal.Api.Features.Payment;
using Sts.Minimal.Api.Infrastructure.Auth;
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
        if (builder.Environment.IsDevelopment()) options.RequireHttpsMetadata = false; // dev only
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = !builder.Environment.IsDevelopment() // Only disable in development
        };
    });

services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationConstants.Policies.Reader, policy =>
        policy.RequireAssertion(ctx => HasRole(ctx.User, AuthorizationConstants.Roles.Reader)));
    options.AddPolicy(AuthorizationConstants.Policies.Writer, policy =>
        policy.RequireAssertion(ctx => HasRole(ctx.User, AuthorizationConstants.Roles.Writer)));
});

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

static bool HasRole(ClaimsPrincipal user, string role)
{
    if (user.IsInRole(role)) return true;

    // Direct 'roles' multi-valued claims
    var roleClaims = user.FindAll("roles");
    if (roleClaims.Any(c => string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase))) return true;

    // Parse realm_access JSON claim: { "roles": ["reader", "writer"] }
    var realmAccess = user.FindFirst("realm_access")?.Value;
    if (!string.IsNullOrEmpty(realmAccess))
        try
        {
            using var doc = JsonDocument.Parse(realmAccess);
            if (doc.RootElement.TryGetProperty("roles", out var rolesEl) && rolesEl.ValueKind == JsonValueKind.Array)
                foreach (var r in rolesEl.EnumerateArray())
                    if (r.ValueKind == JsonValueKind.String &&
                        string.Equals(r.GetString(), role, StringComparison.OrdinalIgnoreCase))
                        return true;
        }
        catch
        {
            // ignore parsing issues
        }

    return false;
}