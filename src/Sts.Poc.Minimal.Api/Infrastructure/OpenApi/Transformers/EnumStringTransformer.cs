using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Sts.Poc.Minimal.Api.Infrastructure.Validation.Attributes;

namespace Sts.Poc.Minimal.Api.Infrastructure.OpenApi.Transformers;

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

            var existing =
                op.Parameters.FirstOrDefault(p => string.Equals(p.Name, pd.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is null) continue;

            // Check if the parameter is a string
            if (existing.Schema?.Type != JsonSchemaType.String) continue;

            // Get enum values
            var values = BuildAllowedValues(enumAttr.EnumType);

            var schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String
            };

            // Copy description from existing parameter
            if (existing.Description is { Length: > 0 })
                schema.Description = existing.Description;

            // Populate the OpenAPI enum list so tools (e.g., Swagger UI) can render a dropdown
            if (values.Count > 0)
                // Ensure non-null JsonNode elements to satisfy IList<JsonNode> and avoid CS8619
                schema.Enum = values
                    .Select(JsonNode (v) => JsonValue.Create(v))
                    .ToList();

            // Replace the existing parameter with the updated schema
            var replacement = new OpenApiParameter()
            {
                Name = existing.Name,
                Description = schema.Description,
                Required = existing.Required,
                In = existing.In,
                Deprecated = existing.Deprecated,
                AllowEmptyValue = existing.AllowEmptyValue,
                Schema = schema, 
                AllowReserved = existing.AllowReserved, 
                Content = existing.Content,
                Example = existing.Example, 
                Examples = existing.Examples, 
                Explode = existing.Explode,
                Extensions = existing.Extensions, 
                Style = existing.Style
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