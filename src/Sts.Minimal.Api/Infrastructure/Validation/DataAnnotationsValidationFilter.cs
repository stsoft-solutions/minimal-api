using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Sts.Minimal.Api.Infrastructure.Validation;

/// <summary>
/// Endpoint filter that enforces System.ComponentModel.DataAnnotations attributes
/// on minimal API handler parameters (e.g., [Required], [Range], etc.).
/// Pass the handler's ParameterInfo[] when registering the filter.
/// </summary>
public sealed class DataAnnotationsValidationFilter : IEndpointFilter
{
    private readonly ParameterInfo[] _parameters;

    // Precompute validation attributes and member names per parameter to avoid reflection on every request
    private readonly (string MemberName, ValidationAttribute[] Attributes)[] _precomputed;

    public DataAnnotationsValidationFilter(ParameterInfo[] parameters)
    {
        _parameters = parameters ?? [];
        _precomputed = new (string, ValidationAttribute[])[_parameters.Length];
        for (var i = 0; i < _parameters.Length; i++)
        {
            var p = _parameters[i];
            var attrs = p
                .GetCustomAttributes(typeof(ValidationAttribute), true)
                .OfType<ValidationAttribute>()
                .ToArray();
            var memberName = p.Name ?? $"arg{i}";
            _precomputed[i] = (memberName, attrs);
        }
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Collect validation errors per-parameter
        Dictionary<string, List<string>>? errors = null;

        for (var i = 0; i < _parameters.Length && i < context.Arguments.Count; i++)
        {
            var value = context.Arguments[i];
            var (memberName, validationAttributes) = _precomputed[i];

            if (validationAttributes.Length > 0)
            {
                foreach (var attr in validationAttributes)
                {
                    // Build a validation context to let attributes compute formatted messages
                    var validationContext = new ValidationContext(value ?? new object(), null, null)
                    {
                        MemberName = memberName
                    };

                    var result = attr.GetValidationResult(value, validationContext);

                    if (result == ValidationResult.Success) continue;

                    errors ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    if (!errors.TryGetValue(memberName, out var list))
                    {
                        list = new List<string>();
                        errors[memberName] = list;
                    }

                    var message = result?.ErrorMessage ?? attr.FormatErrorMessage(memberName);
                    if (!string.IsNullOrWhiteSpace(message)) list.Add(message);
                }
            }
            else if (value is not null)
            {
                // No parameter-level validation attributes present.
                // Validate complex object arguments (e.g., [AsParameters] records) using DataAnnotations on their properties.
                var validationResults = new List<ValidationResult>();
                var objectContext = new ValidationContext(value, null, null);
                // ValidateAllProperties ensures attributes like [Range] on properties are evaluated
                Validator.TryValidateObject(value, objectContext, validationResults, true);

                if (validationResults.Count <= 0) continue;

                errors ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var vr in validationResults)
                {
                    var names = vr.MemberNames.Any() ? vr.MemberNames : [memberName];
                    var message = string.IsNullOrWhiteSpace(vr.ErrorMessage) ? "Invalid value." : vr.ErrorMessage!;
                    foreach (var name in names)
                    {
                        if (!errors.TryGetValue(name, out var list))
                        {
                            list = [];
                            errors[name] = list;
                        }

                        list.Add(message);
                    }
                }
            }
        }

        // Proceed to the next filter / handler if no validation issues
        if (errors is null || errors.Count <= 0) return await next(context);

        // Convert to the format expected by ValidationProblem
        var dict = errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Count == 0 ? [] : kvp.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);

        return TypedResults.ValidationProblem(dict);
    }
}