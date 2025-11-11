using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Sts.Minimal.Api.Infrastructure.OpenApi.Transformers;

/// <summary>
/// Represents a document transformer that adds a JWT Bearer security scheme
/// to the OpenAPI document components if not already present.
/// </summary>
public sealed class JwtBearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Do nothing if already present
        document.Components ??= new OpenApiComponents();
        if (document.Components.SecuritySchemes?.ContainsKey(JwtBearerDefaults.AuthenticationScheme) == true)
            return Task.CompletedTask;

        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

        document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            BearerFormat = "JWT",
            Description = "JWT Bearer authentication."
        };

        return Task.CompletedTask;
    }
}