using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi; // Updated to new flattened namespace


namespace Sts.Minimal.Api.Infrastructure.OpenApi.Transformers;

/// <summary>
/// Transforms OpenAPI operations adding JWT Bearer security requirements and standard auth responses.
/// Updated for microsoft.openapi v3 flattened model (OpenApiSecurityRequirement uses OpenApiSecuritySchemeReference keys).
/// </summary>
public sealed class JwtBearerOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = ((IEnumerable<object>?)context.Description.ActionDescriptor?.EndpointMetadata ?? []).ToArray();

        // Skip if endpoint explicitly allows anonymous access
        if (metadata.OfType<IAllowAnonymous>().Any() || metadata.OfType<AllowAnonymousAttribute>().Any())
            return Task.CompletedTask;

        // Determine if any authorization metadata present
        var authorizeData = metadata.OfType<IAuthorizeData>().ToArray();
        if (authorizeData.Length == 0)
            return Task.CompletedTask;

        operation.Security ??= new List<OpenApiSecurityRequirement>();

        // Build bearer requirement using new reference type
        var schemeRef = new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, context.Document, null);
        var bearerRequirement = new OpenApiSecurityRequirement
        {
            [schemeRef] = new List<string>() // no scopes for plain JWT
        };

        var alreadyPresent = operation.Security.Any(sr => sr.Keys.Any(k => k.Name == JwtBearerDefaults.AuthenticationScheme));
        if (!alreadyPresent)
            operation.Security.Add(bearerRequirement);

        // Ensure 401/403 responses present
        operation.Responses ??= new OpenApiResponses();
        if (!operation.Responses.ContainsKey("401"))
            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
        if (!operation.Responses.ContainsKey("403"))
            operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

        // Append policy info if any
        var policies = authorizeData.Select(d => d.Policy).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToArray();
        if (policies.Length > 0)
            operation.Description = (operation.Description ?? string.Empty) + $"\n\n**Requires policies**: {string.Join(", ", policies)}";

        return Task.CompletedTask;
    }
}
