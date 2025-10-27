using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace Sts.Minimal.Api.Infrastructure.OpenApi;

/// <summary>
/// Provides extension methods for configuring OpenAPI services and related infrastructure in the application.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Configures OpenAPI-related services and infrastructure for the application.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the OpenAPI services to.</param>
    /// <returns>The IServiceCollection instance with OpenAPI infrastructure configured.</returns>
    public static IServiceCollection AddOpenApiInfrastructure(this IServiceCollection services)
    {
        // Add OpenAPI services
        services.AddOpenApi(options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;

            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Version = "v1";
                document.Info.Title = "STS OpenAPI Specification";
                document.Info.Contact = new OpenApiContact
                {
                    Name = "STS Support",
                    Email = "support@stsoft.solutions"
                };
                return Task.CompletedTask;
            });
        });

        // Add API explorer for endpoint metadata
        services.AddEndpointsApiExplorer();

        // Add Problem Details middleware for standardized error responses
        services.AddProblemDetails();

        return services;
    }


    /// <summary>
    /// Configures the middleware for OpenAPI-related functionality in the application.
    /// </summary>
    /// <param name="app">The WebApplication to configure the middleware for.</param>
    /// <returns>The WebApplication instance with OpenAPI middleware configured.</returns>
    public static IApplicationBuilder UseOpenApiInfrastructure(this WebApplication app)
    {
        app.MapOpenApi();

        return app;
    }
}