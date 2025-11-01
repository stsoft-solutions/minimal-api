using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Sts.Minimal.Api.Infrastructure.Middleware;
using Sts.Minimal.Api.Infrastructure.OpenApi.Transformers;
using Sts.Minimal.Api.Infrastructure.Validation;

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

            // Add IsoDateOnlyTransformer
            options.AddOperationTransformer<IsoDateOnlyTransformer>();

            // Add EnumStringTransformer to expose enum choices for string-bound enums
            options.AddOperationTransformer<EnumStringTransformer>();
        });

        // Add API explorer for endpoint metadata
        services.AddEndpointsApiExplorer();

        // Add Problem Details middleware for standardized error responses
        services.AddProblemDetails();
        services.AddExceptionHandler<BadHttpRequestToValidationHandler>();

        return services;
    }


    /// <summary>
    /// Configures the middleware for OpenAPI-related functionality in the application.
    /// </summary>
    /// <param name="app">The WebApplication to configure the middleware for.</param>
    /// <returns>The WebApplication instance with OpenAPI middleware configured.</returns>
    public static IApplicationBuilder UseOpenApiInfrastructure(this WebApplication app)
    {

        
        // --- 2 Then the framework-wide exception handler for all other exceptions ---
        app.UseExceptionHandler(); // handles everything else (NullReferenceException, etc.)

        // http://localhost:5239/openapi/v1.json
        app.Logger.LogInformation("Configuring OpenAPI at http://localhost:5239/openapi/v1.json");
        app.MapOpenApi();
        
        // http://localhost:5239/scalar
        app.Logger.LogInformation("Configuring Scalar API Reference at http://localhost:5239/scalar");
        app.MapScalarApiReference(options =>
        {
            options.EnabledClients =
            [
                ScalarClient.RestSharp, ScalarClient.Curl, ScalarClient.Fetch, ScalarClient.HttpClient
            ];
            options.EnabledTargets =
            [
                ScalarTarget.CSharp, ScalarTarget.JavaScript, ScalarTarget.Shell
            ];
            options.Authentication = new ScalarAuthenticationOptions
            {
                PreferredSecuritySchemes = [JwtBearerDefaults.AuthenticationScheme]
            };
        });

        return app;
    }

    private static string FriendlyError(string? typeHint, string? sourceValue)
    {
        if (typeHint is null) return "Invalid value.";

        // Compare on normalized underlying type
        var t = typeHint.ToLowerInvariant();

        if (t.Contains("guid")) return "Invalid format. Must be a valid GUID.";
        if (t.Contains("int") || t.Contains("int32") || t.Contains("int64")) return "Invalid number. Must be an integer.";
        if (t.Contains("decimal") || t.Contains("double") || t.Contains("single") || t.Contains("float")) return "Invalid number.";
        if (t.Contains("dateonly")) return "Invalid date. Use yyyy-MM-dd.";
        if (t.Contains("datetime") || t.Contains("datetimeoffset")) return "Invalid date/time.";
        if (t.Contains("bool") || t.Contains("boolean")) return "Invalid boolean. Use true or false.";

        // Likely enum or custom type
        return "Invalid value.";
    }

    private static class BinderMessageParser
    {
        // Matches:
        //  "Nullable<Guid> referenceId" from "wrong-format-id"
        //  "Guid referenceId" from "..."
        //  "Int32 paymentId" from "abc"
        //  "PaymentStatus status" from "zzz"
        //  "Nullable`1[DateOnly] valueDate" from "x"
        private static readonly Regex Rx = new(
            "Failed to bind parameter\\s+\"(?<type>[^\\s\"<>`]+(?:<[^>]+>)?(?:`\\d+\\[[^\\]]+\\])?)\\s+(?<name>\\w+)\"\\s+from\\s+\"(?<value>.*?)\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static (string? name, string? value, string? typeHint) Parse(string message)
        {
            var m = Rx.Match(message);
            if (!m.Success) return (null, null, null);
            return (m.Groups["name"].Value, m.Groups["value"].Value, m.Groups["type"].Value);
        }

        public static string? UnwrapNullable(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;

            // Handle Nullable<T>
            var angle = Regex.Match(raw, "^Nullable<(?<inner>[^>]+)>$", RegexOptions.CultureInvariant);
            if (angle.Success) return angle.Groups["inner"].Value;

            // Handle Nullable`1[Inner]
            var backtick = Regex.Match(raw, "^Nullable`\\d+\\[(?<inner>[^\\]]+)\\]$", RegexOptions.CultureInvariant);
            if (backtick.Success) return backtick.Groups["inner"].Value;

            return raw; // already a simple type
        }
    }
}