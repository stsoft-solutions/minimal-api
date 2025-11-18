using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Sts.Minimal.Api.Infrastructure.Validation.Attributes;

namespace Sts.Minimal.Api.Infrastructure.OpenApi.Transformers;

public sealed class IsoDateOnlyStringTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation op, OpenApiOperationTransformerContext ctx, CancellationToken _)
    {
        if (op.Parameters is null || op.Parameters.Count == 0)
            return Task.CompletedTask;

        foreach (var pd in ctx.Description.ParameterDescriptions)
        {
            var pi = TryGetParameterInfo(pd);
            if (pi is null) continue;
            if (pi.GetCustomAttribute<StringAsIsoDateAttribute>() is null) continue;

            var existing = op.Parameters.FirstOrDefault(p => string.Equals(p.Name, pd.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is null) continue;

            var newSchema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Description = "ISO date (yyyy-MM-dd)"
            };

            var replacement = new OpenApiParameter
            {
                Name = existing.Name,
                Description = existing.Description ?? "ISO date (yyyy-MM-dd)",
                Required = existing.Required,
                In = existing.In,
                Deprecated = existing.Deprecated,
                AllowEmptyValue = existing.AllowEmptyValue,
                Schema = newSchema
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
}