using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Sts.Minimal.Api.Infrastructure.Validation;

namespace Sts.Minimal.Api.Infrastructure.OpenApi;

public sealed class IsoDateOnlyTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation op, OpenApiOperationTransformerContext ctx, CancellationToken _)
    {
        foreach (var pd in ctx.Description.ParameterDescriptions)
        {
            // 1) Get the hidden ParameterInfo (e.g., PropertyAsParameterInfo) via reflection
            var pi = TryGetParameterInfo(pd);
            if (pi is null) continue;

            // 2) Check for your custom attribute
            if (pi.GetCustomAttribute<IsoDateOnlyAttribute>() is null)
                continue;

            // 3) Find and modify the OpenAPI parameter
            var oap = op.Parameters?.FirstOrDefault(p =>
                string.Equals(p.Name, pd.Name, StringComparison.OrdinalIgnoreCase));
            if (oap is null) continue;

            oap.Schema ??= new OpenApiSchema();
            oap.Schema.Type = "string";
            oap.Schema.Format = "date"; // yyyy-MM-dd
            oap.Extensions["x-iso-date-only"] = new OpenApiBoolean(true);

            if (string.IsNullOrWhiteSpace(oap.Description))
                oap.Description = "ISO date (yyyy-MM-dd)";
        }

        return Task.CompletedTask;
    }

    private static ParameterInfo? TryGetParameterInfo(ApiParameterDescription pd)
    {
        var desc = pd.ParameterDescriptor;

        // Most concrete descriptors (Minimal API / MVC) expose a public ParameterInfo
        // We fetch it reflectively so you don't need to reference specific descriptor types.
        var prop = desc.GetType().GetProperty("ParameterInfo", BindingFlags.Instance | BindingFlags.Public);
        return prop?.GetValue(desc) as ParameterInfo;
    }
}