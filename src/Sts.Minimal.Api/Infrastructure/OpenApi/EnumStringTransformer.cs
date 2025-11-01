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
    /// <summary>
    /// Transforms the OpenAPI operation by adding enum values to parameters annotated with the <see cref="EnumStringAttribute" />.
    /// This enhancement modifies the schema of parameters to include allowed string enum values in OpenAPI documentation.
    /// </summary>
    /// <param name="op">The OpenAPI operation to be transformed.</param>
    /// <param name="ctx">The context describing the API operation and its parameters.</param>
    /// <param name="_">The cancellation token that can be observed for task cancellation.</param>
    /// <returns>A task that represents the asynchronous transformation operation.</returns>
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

    /// <summary>
    /// Attempts to retrieve the reflection metadata for a parameter described in the API parameter description.
    /// This metadata can be used to inspect attributes such as <see cref="EnumStringAttribute" />.
    /// </summary>
    /// <param name="pd">The API parameter description from the context of an operation.</param>
    /// <returns>The parameter metadata as <see cref="ParameterInfo" />, or null if it cannot be retrieved.</returns>
    private static ParameterInfo? TryGetParameterInfo(ApiParameterDescription pd)
    {
        var desc = pd.ParameterDescriptor;
        var prop = desc.GetType().GetProperty("ParameterInfo", BindingFlags.Instance | BindingFlags.Public);
        return prop?.GetValue(desc) as ParameterInfo;
    }

    /// <summary>
    /// Builds a list of allowed values for an enumeration type by extracting its standard names and any custom names
    /// defined using <see cref="JsonStringEnumMemberNameAttribute" />.
    /// </summary>
    /// <param name="enumType">The enum type from which to extract the allowed values.</param>
    /// <returns>A list of allowed string values representing the enum's standard and custom names.</returns>
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