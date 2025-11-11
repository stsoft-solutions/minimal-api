using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace Sts.Minimal.Api.Infrastructure.OpenApi.Transformers;

/// <summary>
/// The <c>JwtBearerOperationTransformer</c> class is used to transform OpenAPI operations
/// by applying modifications related to JWT Bearer authentication.
/// </summary>
/// <remarks>
/// This class is intended to be used as part of the OpenAPI operation transformation pipeline
/// to ensure that specific operations are configured for JWT Bearer authentication handling.
/// </remarks>
public sealed class JwtBearerOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Collect endpoint metadata
        var metadata = ((IEnumerable<object>?)context.Description.ActionDescriptor?.EndpointMetadata
                        ?? []).ToArray();

        // If [AllowAnonymous] is present -> skip
        var hasAllowAnonymous = metadata.OfType<IAllowAnonymous>().Any()
                                || metadata.OfType<AllowAnonymousAttribute>().Any();
        if (hasAllowAnonymous)
            return Task.CompletedTask;

        // Detect any authorization requirement:
        // - [Authorize] attribute(s)
        // - .RequireAuthorization() / policy names (IAuthorizeData)
        var authorizeData = metadata.OfType<IAuthorizeData>().ToArray();
        var requiresAuth = authorizeData.Length > 0;

        if (!requiresAuth)
            return Task.CompletedTask;

        operation.Security ??= new List<OpenApiSecurityRequirement>();

        // Attach Bearer security requirement (no scopes for plain JWT)
        var bearerRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = JwtBearerDefaults.AuthenticationScheme
                    }
                }
            ] = []
        };

        // Avoid duplicates
        var alreadyPresent = operation.Security.Any(sr =>
            sr.Keys.Any(k => k.Reference?.Id == JwtBearerDefaults.AuthenticationScheme));

        if (!alreadyPresent)
            operation.Security.Add(bearerRequirement);

        // Add standard 401/403 responses if missing
        operation.Responses ??= new OpenApiResponses();
        if (!operation.Responses.ContainsKey("401"))
            operation.Responses.Add("401", new OpenApiResponse
            {
                Description = "Unauthorized"
            });
        if (!operation.Responses.ContainsKey("403"))
            operation.Responses.Add("403", new OpenApiResponse
            {
                Description = "Forbidden"
            });

        // Optional: reflect policy names in description (handy for debugging)
        var policies = authorizeData
            .Select(d => d.Policy)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .ToArray();

        if (policies.Length > 0)
            operation.Description = (operation.Description ?? string.Empty) +
                                    $"\n\n**Requires policies**: {string.Join(", ", policies)}";

        return Task.CompletedTask;
    }
}