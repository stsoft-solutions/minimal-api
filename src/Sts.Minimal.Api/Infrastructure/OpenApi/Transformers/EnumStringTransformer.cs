using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Sts.Minimal.Api.Infrastructure.Validation.Attributes;

namespace Sts.Minimal.Api.Infrastructure.OpenApi.Transformers;

/// <summary>
/// Adds enum values to OpenAPI for string parameters annotated with <see cref="StringAsEnumAttribute" />.
/// </summary>
public sealed class EnumStringTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation op, OpenApiOperationTransformerContext ctx, CancellationToken _)
    {
        if (op.Parameters is null || op.Parameters.Count == 0)
            return Task.CompletedTask;

        foreach (var pd in ctx.Description.ParameterDescriptions)
        {
            var pi = TryGetParameterInfo(pd);
            var enumAttr = pi?.GetCustomAttribute<StringAsEnumAttribute>();
            if (enumAttr is null) continue;

            var existing = op.Parameters.FirstOrDefault(p => string.Equals(p.Name, pd.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is null) continue;

            var values = BuildAllowedValues(enumAttr.EnumType);

            var schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Description = (existing.Description ?? string.Empty) + (values.Count > 0 ? $" Allowed values: {string.Join(", ", values)}." : string.Empty)
            };

            var replacement = new OpenApiParameter
            {
                Name = existing.Name,
                Description = schema.Description,
                Required = existing.Required,
                In = existing.In,
                Deprecated = existing.Deprecated,
                AllowEmptyValue = existing.AllowEmptyValue,
                Schema = schema
            };

            var index = op.Parameters.IndexOf(existing);
            if (index >= 0)
                op.Parameters[index] = replacement;
        }

        return Task.CompletedTask;
    }

    private static ParameterInfo? TryGetParameterInfo(ApiParameterDescription pd)
    {
        var desc = pd.ParameterDescriptor;
        var prop = desc.GetType().GetProperty("ParameterInfo", BindingFlags.Instance | BindingFlags.Public);
        return prop?.GetValue(desc) as ParameterInfo;
    }

    private static List<string> BuildAllowedValues(Type enumType)
    {
        var list = Enum.GetNames(enumType).ToList();
        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var f in fields)
        {
            var jsonNameAttr = f.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
            if (jsonNameAttr?.Name is { Length: > 0 } custom &&
                !list.Contains(custom, StringComparer.OrdinalIgnoreCase)) list.Add(custom);
        }

        return list;
    }
}
