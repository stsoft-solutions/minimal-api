using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Sts.Poc.Minimal.Api.Infrastructure.Validation.Attributes;

namespace Sts.Poc.Minimal.Api.Infrastructure.OpenApi.Transformers;

public sealed class IsoDateOnlyStringTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation op, OpenApiOperationTransformerContext ctx, CancellationToken _)
    {
        if (op.Parameters is null || op.Parameters.Count == 0)
            return Task.CompletedTask;

        foreach (var pd in ctx.Description.ParameterDescriptions)
        {
            var pi = TryGetParameterInfo(pd);
            if (pi?.GetCustomAttribute<StringAsIsoDateAttribute>() is null) continue;

            var existing =
                op.Parameters.FirstOrDefault(p => string.Equals(p.Name, pd.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is null) continue;

            var newSchema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Description = "ISO date (yyyy-MM-dd)",
                Pattern = @"^(?:\d{4})-(?:0[1-9]|1[0-2])-(?:0[1-9]|[12]\d|3[01])$"
            };

            // Leap-year aware regex (more complex) — validates month/day counts including Feb 29 on leap years
            // var leapYearAwarePattern = @"^(?:(?:(?:\d{4})-(?:0[13578]|1[02])-(?:0[1-9]|[12]\d|3[01]))|(?:(?:\d{4})-(?:0[469]|11)-(?:0[1-9]|[12]\d|30))|(?:(?:\d{4})-02-(?:0[1-9]|1\d|2[0-8]))|(?:(?:\d{2}(?:0[48]|[2468][048]|[13579][26])|(?:[02468][048]00|[13579][26]00))-02-29))$";

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