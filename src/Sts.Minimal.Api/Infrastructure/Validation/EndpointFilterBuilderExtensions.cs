using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Sts.Minimal.Api.Infrastructure.Validation;

/// <summary>
/// Extension methods to simplify adding common endpoint filters.
/// </summary>
public static class EndpointFilterBuilderExtensions
{
    /// <summary>
    /// Adds an endpoint filter that validates handler parameters using
    /// System.ComponentModel.DataAnnotations attributes.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The same <see cref="RouteHandlerBuilder"/> for chaining.</returns>
    public static RouteHandlerBuilder AddDataAnnotationsValidation(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilterFactory((factoryContext, next) =>
        {
            var parameters = factoryContext.MethodInfo?.GetParameters() ?? [];
            var filter = new DataAnnotationsValidationFilter(parameters);
            return context => filter.InvokeAsync(context, next);
        });
    }
}
