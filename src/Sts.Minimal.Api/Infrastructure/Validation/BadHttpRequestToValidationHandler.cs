using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Sts.Minimal.Api.Infrastructure.Validation;

public sealed class BadHttpRequestToValidationHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception ex, CancellationToken token)
    {
        if (ex is not BadHttpRequestException bhre || !bhre.Message.StartsWith("Failed to bind parameter")) 
            return false;
        
        var (name, value, typeHintRaw) = BinderMessageParser.Parse(bhre.Message);
        var typeHint = BinderMessageParser.UnwrapNullable(typeHintRaw);

        var pds = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        var vpd = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [name ?? "parameter"] = new[] { FriendlyError(typeHint, value) }
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title  = "One or more parameters are invalid."
        };

        await pds.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = vpd });
        return true; // ← tells middleware it's handled (no error log)
    }

    static string FriendlyError(string? typeHint, string? _)
    {
        var t = typeHint?.ToLowerInvariant() ?? "";
        if (t.Contains("guid")) return "Invalid format. Must be a valid GUID.";
        if (t.Contains("int")) return "Invalid number. Must be an integer.";
        if (t.Contains("dateonly")) return "Invalid date. Use yyyy-MM-dd.";
        if (t.Contains("bool")) return "Invalid boolean. Use true or false.";
        return "Invalid value.";
    }

    static class BinderMessageParser
    {
        static readonly Regex Rx = new(
            "Failed to bind parameter\\s+\"(?<type>[^\\s\"<>`]+(?:<[^>]+>)?(?:`\\d+\\[[^\\]]+\\])?)\\s+(?<name>\\w+)\"\\s+from\\s+\"(?<value>.*?)\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static (string? name, string? value, string? typeHint) Parse(string message)
        {
            var m = Rx.Match(message);
            if (!m.Success) return (null, null, null);
            return (m.Groups["name"].Value, m.Groups["value"].Value, m.Groups["type"].Value);
        }

        public static string? UnwrapNullable(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            var a = Regex.Match(raw, "^Nullable<(?<inner>[^>]+)>$");
            if (a.Success) return a.Groups["inner"].Value;
            var b = Regex.Match(raw, "^Nullable`\\d+\\[(?<inner>[^\\]]+)\\]$");
            if (b.Success) return b.Groups["inner"].Value;
            return raw;
        }
    }
}