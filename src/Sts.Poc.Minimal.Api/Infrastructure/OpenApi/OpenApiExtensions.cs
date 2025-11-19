using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Sts.Poc.Minimal.Api.Infrastructure.Middleware;
using Sts.Poc.Minimal.Api.Infrastructure.OpenApi.Transformers;

namespace Sts.Poc.Minimal.Api.Infrastructure.OpenApi;

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
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;

            // Add Scalar transformers
            options.AddScalarTransformers();

            // Customize the OpenAPI document
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

            // Add AddJwtBearerSchemeDocumentTransformer to add JWT Bearer scheme to components
            options.AddDocumentTransformer<JwtBearerSecuritySchemeTransformer>();

            // Add IsoDateOnlyStringTransformer
            options.AddOperationTransformer<IsoDateOnlyStringTransformer>();

            // Add EnumStringTransformer to expose enum choices for string-bound enums
            options.AddOperationTransformer<EnumStringTransformer>();

            // Add the JWT Bearer scheme to the operation
            options.AddOperationTransformer<JwtBearerOperationTransformer>();
        });

        // Add API explorer for endpoint metadata
        services.AddEndpointsApiExplorer();

        // Add Problem Details middleware for standardized error responses
        services.AddValidation();
        services.AddProblemDetails(options =>
        {
            // Normalize validation error keys to public query names
            options.CustomizeProblemDetails = ctx =>
            {
                if (ctx.ProblemDetails is not HttpValidationProblemDetails vpd)
                    return;

                if (vpd.Errors.Count == 0)
                    return;

                var httpContext = ctx.HttpContext;
                var remapped = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

                foreach (var kv in vpd.Errors)
                {
                    var originalKey = kv.Key;
                    var mapped = MapToQueryParameterName(httpContext, originalKey)
                                 ?? ToKebabCase(originalKey)
                                 ?? originalKey;

                    if (remapped.TryGetValue(mapped, out var existing))
                    {
                        var combined = new string[existing.Length + kv.Value.Length];
                        existing.CopyTo(combined, 0);
                        kv.Value.CopyTo(combined, existing.Length);
                        remapped[mapped] = combined;
                    }
                    else
                    {
                        remapped[mapped] = kv.Value.Select(s => s.Replace(kv.Key, mapped)).ToArray();
                    }
                }

                // Replace it with a remapped dictionary
                foreach (var key in vpd.Errors.Keys.ToList())
                {
                    vpd.Errors.Remove(key);
                }

                foreach (var kv in remapped)
                {
                    vpd.Errors.Add(kv.Key, kv.Value);
                }
            };
        });
        services.AddExceptionHandler<BadHttpRequestToValidationHandler>();

        return services;
    }

    /// <summary>
    /// Maps a CLR or ModelState key to the public query parameter name (FromQuery.Name) when available.
    /// </summary>
    private static string? MapToQueryParameterName(HttpContext httpContext, string? originalName)
    {
        if (string.IsNullOrWhiteSpace(originalName)) return originalName;

        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null) return originalName;

        // Try resolve via MethodInfo parameters
        var method = endpoint.Metadata.GetMetadata<MethodInfo>()
                     ?? (endpoint as RouteEndpoint)?.Metadata.GetMetadata<MethodInfo>();

        if (method is not null)
        {
            // 1) Direct parameter [FromQuery(Name=...)]
            var p = method.GetParameters().FirstOrDefault(p => string.Equals(p.Name, originalName, StringComparison.OrdinalIgnoreCase));
            var custom = GetFromQueryCustomName(p);
            if (!string.IsNullOrEmpty(custom)) return custom;

            // 2) Property on complex parameter types
            foreach (var prm in method.GetParameters())
            {
                var prop = prm.ParameterType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(pi => string.Equals(pi.Name, originalName, StringComparison.OrdinalIgnoreCase));
                var propCustom = GetFromQueryCustomName(prop);
                if (!string.IsNullOrEmpty(propCustom)) return propCustom;
            }
        }

        // Fallback: ParameterInfo metadata bag (some hosting setups)
        try
        {
            var parameters = endpoint.Metadata.GetOrderedMetadata<ParameterInfo>();
            var p2 = parameters.FirstOrDefault(p => string.Equals(p.Name, originalName, StringComparison.OrdinalIgnoreCase));
            var custom2 = GetFromQueryCustomName(p2);
            if (!string.IsNullOrEmpty(custom2)) return custom2;

            foreach (var prm in parameters)
            {
                var prop = prm.ParameterType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(pi => string.Equals(pi.Name, originalName, StringComparison.OrdinalIgnoreCase));
                var propCustom = GetFromQueryCustomName(prop);
                if (!string.IsNullOrEmpty(propCustom)) return propCustom;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static string? GetFromQueryCustomName(ParameterInfo? parameter)
    {
        var attr = parameter?.GetCustomAttribute<FromQueryAttribute>();
        return !string.IsNullOrEmpty(attr?.Name) ? attr!.Name : null;
    }

    private static string? GetFromQueryCustomName(PropertyInfo? property)
    {
        var attr = property?.GetCustomAttribute<FromQueryAttribute>();
        return !string.IsNullOrEmpty(attr?.Name) ? attr!.Name : null;
    }

    private static string? ToKebabCase(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (name.Contains('-')) return name.Replace("_", "-").ToLowerInvariant();
        var withHyphens = Regex.Replace(name, "(?<!^)([A-Z][a-z]|(?<=[a-z0-9])[A-Z])", "-$1");
        withHyphens = withHyphens.Replace('_', '-');
        return withHyphens.ToLowerInvariant();
    }

    /// <summary>
    /// Configures the middleware for OpenAPI-related functionality in the application.
    /// </summary>
    /// <param name="app">The WebApplication to configure the middleware for.</param>
    /// <returns>The WebApplication instance with OpenAPI middleware configured.</returns>
    public static IApplicationBuilder UseOpenApiInfrastructure(this WebApplication app)
    {
        // Add HTTP logging middleware
        app.UseSerilogRequestLogging(options => { options.EnrichDiagnosticContext = LogHelper.EnrichFromHttpContext; });

        // ---1 Then the framework-wide exception handler for all other exceptions ---
        app.UseExceptionHandler(); // handles everything else (NullReferenceException, etc.)

        // ---2 Map OpenAPI
        // http://localhost:5239/openapi/v1.json
        app.Logger.LogInformation("Configuring OpenAPI at http://localhost:5239/openapi/v1.json");
        app.MapOpenApi();

        // ---3 Map Scalar API Reference
        // http://localhost:5239/scalar
        app.Logger.LogInformation("Configuring Scalar API Reference at http://localhost:5239/scalar");
        app.MapScalarApiReference(options =>
        {
            options.EnabledClients =
                [ScalarClient.RestSharp, ScalarClient.Curl, ScalarClient.Fetch, ScalarClient.HttpClient];
            options.EnabledTargets = [ScalarTarget.CSharp, ScalarTarget.JavaScript, ScalarTarget.Shell];
            options.Authentication = new ScalarAuthenticationOptions
            {
                PreferredSecuritySchemes = [JwtBearerDefaults.AuthenticationScheme]
            };
        });

        return app;
    }
}