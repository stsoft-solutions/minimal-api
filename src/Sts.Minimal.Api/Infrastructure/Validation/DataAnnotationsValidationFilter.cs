using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Sts.Minimal.Api.Infrastructure.Validation;

/// <summary>
/// Endpoint filter that enforces System.ComponentModel.DataAnnotations attributes
/// on minimal API handler parameters (e.g., [Required], [Range], etc.).
/// Pass the handler's ParameterInfo[] when registering the filter.
/// </summary>
public sealed class DataAnnotationsValidationFilter : IEndpointFilter
{
    // Cache per-type property validation metadata to avoid reflection and slow PropertyInfo.GetValue each request
    private static readonly ConcurrentDictionary<Type, PropertyValidationMeta[]> PropertyValidationMetasMap = new();
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

            // Use external binding name when available to ensure ProblemDetails keys reflect query/body names
            var fromQuery = p
                .GetCustomAttributes(typeof(FromQueryAttribute), true)
                .OfType<FromQueryAttribute>()
                .FirstOrDefault();

            var memberName = !string.IsNullOrWhiteSpace(fromQuery?.Name)
                ? fromQuery!.Name!
                : p.Name ?? $"arg{i}";
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
                // Reuse a single ValidationContext for the parameter across its attributes
                var validationContext = new ValidationContext(value ?? new object(), null, null)
                {
                    MemberName = memberName
                };

                foreach (var attr in validationAttributes)
                {
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
                // Validate complex object properties using cached metadata.
                var obj = value;
                var type = obj.GetType();
                var metas = GetOrAddPropertyMetas(type);

                if (metas.Length > 0)
                    foreach (var meta in metas)
                    {
                        var propValue = meta.Getter(obj);

                        // One ValidationContext per property; reuse across its attributes
                        var vc = new ValidationContext(propValue ?? new object(), null, null)
                        {
                            MemberName = meta.BindingName
                        };

                        foreach (var attr in meta.Attributes)
                        {
                            var result = attr.GetValidationResult(propValue, vc);
                            if (result == ValidationResult.Success) continue;

                            errors ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                            if (!errors.TryGetValue(meta.BindingName, out var list))
                            {
                                list = [];
                                errors[meta.BindingName] = list;
                            }

                            var message = result?.ErrorMessage ?? attr.FormatErrorMessage(meta.BindingName);
                            if (!string.IsNullOrWhiteSpace(message)) list.Add(message);
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

    private static PropertyValidationMeta[] GetOrAddPropertyMetas(Type type)
    {
        return PropertyValidationMetasMap.GetOrAdd(type, static t =>
        {
            var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (props.Length == 0) return [];

            var list = new List<PropertyValidationMeta>(props.Length);

            foreach (var prop in props)
            {
                var propValidationAttributes = prop
                    .GetCustomAttributes(typeof(ValidationAttribute), true)
                    .OfType<ValidationAttribute>()
                    .ToArray();

                if (propValidationAttributes.Length == 0) continue;

                // Determine the external/binding name for the property
                // Priority:
                // 1) JsonPropertyNameAttribute (for body-bound JSON contracts)
                // 2) FromQueryAttribute.Name (for query-bound params)
                // 3) camelCase of the CLR property name (fallback)
                var jsonProperty = prop.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                    .OfType<JsonPropertyNameAttribute>()
                    .FirstOrDefault();

                var fromQuery = prop.GetCustomAttributes(typeof(FromQueryAttribute), true)
                    .OfType<FromQueryAttribute>()
                    .FirstOrDefault();

                var bindingName = !string.IsNullOrWhiteSpace(jsonProperty?.Name)
                    ? jsonProperty!.Name!
                    : !string.IsNullOrWhiteSpace(fromQuery?.Name)
                        ? fromQuery!.Name!
                        : prop.Name.Length > 0
                            ? char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..]
                            : prop.Name;

                list.Add(new PropertyValidationMeta
                {
                    BindingName = bindingName,
                    Getter = CreateFastGetter(prop),
                    Attributes = propValidationAttributes
                });
            }

            return list.Count == 0 ? [] : list.ToArray();
        });
    }

    private static Func<object, object?> CreateFastGetter(PropertyInfo prop)
    {
        // Build: (object instance) => (object?) ((TDeclaring)instance).Prop
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var castInstance = Expression.Convert(instanceParam, prop.DeclaringType!);
        var propertyAccess = Expression.Property(castInstance, prop);
        var castResult = Expression.Convert(propertyAccess, typeof(object));
        var lambda = Expression.Lambda<Func<object, object?>>(castResult, instanceParam);
        return lambda.Compile();
    }

    private sealed class PropertyValidationMeta
    {
        public required string BindingName { get; init; }
        public required Func<object, object?> Getter { get; init; }
        public required ValidationAttribute[] Attributes { get; init; }
    }
}