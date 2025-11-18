using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Sts.Minimal.Api.Infrastructure.OpenApi.Transformers;

/// <summary>
/// Adds a JWT Bearer security scheme to the OpenAPI document if missing.
/// Updated for microsoft.openapi v3 flattened model.
/// </summary>
public sealed class JwtBearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();

        // If scheme already present, no-op
        if (document.Components.SecuritySchemes?.ContainsKey(JwtBearerDefaults.AuthenticationScheme) == true)
            return Task.CompletedTask;

        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        // Create concrete scheme and store; the dictionary expects IOpenApiSecurityScheme values
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            BearerFormat = "JWT",
            Description = "JWT Bearer authentication."
        };

        document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = scheme;

        return Task.CompletedTask;
    }
}
