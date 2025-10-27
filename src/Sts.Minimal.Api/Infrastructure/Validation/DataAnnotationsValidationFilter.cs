using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Sts.Minimal.Api.Infrastructure.Validation;

/// <summary>
/// Endpoint filter that enforces System.ComponentModel.DataAnnotations attributes
/// on minimal API handler parameters (e.g., [Required], [Range], etc.).
/// Pass the handler's ParameterInfo[] when registering the filter.
/// </summary>
public sealed class DataAnnotationsValidationFilter : IEndpointFilter
{
    private readonly ParameterInfo[] _parameters;

    public DataAnnotationsValidationFilter(ParameterInfo[] parameters)
    {
        _parameters = parameters ?? [];
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Collect validation errors per-parameter
        Dictionary<string, List<string>>? errors = null;

        for (var i = 0; i < _parameters.Length && i < context.Arguments.Count; i++)
        {
            var parameter = _parameters[i];
            var value = context.Arguments[i];

            // Find DataAnnotations validation attributes on the parameter
            var validationAttributes = parameter
                .GetCustomAttributes(typeof(ValidationAttribute), inherit: true)
                .OfType<ValidationAttribute>()
                .ToArray();

            if (validationAttributes.Length == 0)
                continue;

            var memberName = parameter.Name ?? $"arg{i}";

            foreach (var attr in validationAttributes)
            {
                // Build a validation context to let attributes compute formatted messages
                var validationContext = new ValidationContext(instance: value ?? new object(), serviceProvider: null, items: null)
                {
                    MemberName = memberName
                };

                var result = attr.GetValidationResult(value, validationContext);
                if (result != ValidationResult.Success)
                {
                    errors ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    if (!errors.TryGetValue(memberName, out var list))
                    {
                        list = new List<string>();
                        errors[memberName] = list;
                    }

                    var message = result?.ErrorMessage ?? attr.FormatErrorMessage(memberName);
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        list.Add(message);
                    }
                }
            }
        }

        if (errors is not null && errors.Count > 0)
        {
            // Convert to the format expected by ValidationProblem
            var dict = errors.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count == 0 ? Array.Empty<string>() : kvp.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);

            return TypedResults.ValidationProblem(dict);
        }

        // Proceed to next filter/handler if no validation issues
        return await next(context);
    }
}
