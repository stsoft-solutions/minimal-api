using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Sts.Minimal.Api.Infrastructure.Validation;

namespace Sts.Minimal.Api.Infrastructure.OpenApi;

/// <summary>
/// Adds enum values to OpenAPI for string parameters annotated with <see cref="EnumStringAttribute" />.
/// This preserves runtime binding as string while surfacing allowed values in the schema.
/// </summary>
public sealed class EnumStringTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation op, OpenApiOperationTransformerContext ctx, CancellationToken _)
    {
        foreach (var pd in ctx.Description.ParameterDescriptions)
        {
            var pi = TryGetParameterInfo(pd);
            var enumAttr = pi?.GetCustomAttribute<EnumStringAttribute>();
            if (enumAttr is null) continue;

            // Find OpenAPI parameter by name
            var oap = op.Parameters?.FirstOrDefault(p => string.Equals(p.Name, pd.Name, StringComparison.OrdinalIgnoreCase));
            if (oap is null) continue;

            oap.Schema ??= new OpenApiSchema();
            oap.Schema.Type = "string";
            oap.Schema.Format = null; // ensure no conflicting format

            // Build enum values based on the enum type referenced by the attribute
            var values = BuildAllowedValues(enumAttr.EnumType);
            if (values.Count > 0) oap.Schema.Enum = values.Select(v => (IOpenApiAny)new OpenApiString(v)).ToList();

            // Add a vendor extension to indicate the source
            oap.Extensions["x-enum-source"] = new OpenApiString(enumAttr.EnumType.FullName ?? enumAttr.EnumType.Name);

            // If no description is set, hint that case-insensitive values are accepted
            if (string.IsNullOrWhiteSpace(oap.Description)) oap.Description = $"Payment's status (one of: {string.Join(", ", values)}).";
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
        // Add enum names
        var list = Enum.GetNames(enumType).ToList();

        // Add JsonStringEnumMemberName values when present
        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var f in fields)
        {
            var jsonNameAttr = f.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
            if (jsonNameAttr?.Name is { Length: > 0 } custom && !list.Contains(custom, StringComparer.OrdinalIgnoreCase)) list.Add(custom);
        }

        return list;
    }
}