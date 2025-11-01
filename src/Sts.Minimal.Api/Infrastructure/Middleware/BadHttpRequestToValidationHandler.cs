using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Sts.Minimal.Api.Infrastructure.Middleware;

/// <summary>
/// Handles exceptions of type <see cref="BadHttpRequestException"/> that occur due to
/// failed parameter binding. Converts the exception into a standardized
/// validation problem response compatible with API conventions.
/// </summary>
/// <remarks>
/// This handler is specifically designed to process exceptions where the
/// message starts with "Failed to bind parameter". It extracts error
/// details from the exception and generates a <see cref="ValidationProblemDetails"/>
/// response, providing meaningful feedback about invalid parameters.
/// </remarks>
/// <seealso cref="IExceptionHandler"/>
public sealed class BadHttpRequestToValidationHandler : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle a <see cref="BadHttpRequestException"/> by generating a validation error response.
    /// </summary>
    /// <param name="context">The current HTTP context associated with the request.</param>
    /// <param name="ex">The exception to handle, which must be of type <see cref="BadHttpRequestException"/> to proceed with handling.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while performing the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask{Boolean}"/> indicating whether the exception was successfully handled. Returns true if handled, false otherwise.</returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception ex, CancellationToken token)
    {
        if (ex is not BadHttpRequestException badHttpRequestException)
            return false;
        
        badHttpRequestException.Data.Add("BadHttpRequestToValidationHandler", true);
        
        var (name, value, typeHintRaw) = BinderMessageParser.Parse(badHttpRequestException.Message);
        var typeHint = BinderMessageParser.UnwrapNullable(typeHintRaw);

        var pds = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        var vpd = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [name ?? "referenceId"] = new[] { FriendlyError(typeHint, value) }
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title  = "One or more parameters are invalid."
        };

        // Explicitly set the HTTP response status to 400 to avoid default 500 from the exception handler
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        await pds.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = vpd });
        return true; // ← tells middleware it's handled (no error log)
    }

    /// <summary>
    /// Generates a user-friendly error message based on the provided type hint.
    /// </summary>
    /// <param name="typeHint">A string that indicates the expected type of the parameter, used to customize the error message (e.g., "int", "guid", "dateonly").</param>
    /// <param name="_">The actual parameter value that caused the error, not used in this method.</param>
    /// <returns>A user-friendly error message indicating the expected format or type of the parameter.</returns>
    private static string FriendlyError(string? typeHint, string? _)
    {
        var t = typeHint?.ToLowerInvariant() ?? "";
        if (t.Contains("guid")) return "Invalid format. Must be a valid GUID.";
        if (t.Contains("int")) return "Invalid number. Must be an integer.";
        if (t.Contains("dateonly")) return "Invalid date. Use yyyy-MM-dd.";
        if (t.Contains("bool")) return "Invalid boolean. Use true or false.";
        return "Invalid value.";
    }

    /// <summary>
    /// Provides functionality to parse and extract details from binder exception messages.
    /// </summary>
    /// <remarks>
    /// This utility class is used to parse messages generated during model binding failures
    /// and extract relevant information such as the parameter name, the provided value,
    /// and the expected type. Additionally, it includes methods to process and unwrap
    /// nullable type hints, allowing for better error handling during validation.
    /// </remarks>
    private static class BinderMessageParser
    {
        // Regex pattern for parsing messages generated during model binding failures.
        // Example: "Failed to bind parameter 'value' from '123'."
        static readonly Regex Rx = new(
            "Failed to bind parameter\\s+\"(?<type>[^\\s\"<>`]+(?:<[^>]+>)?(?:`\\d+\\[[^\\]]+\\])?)\\s+(?<name>\\w+)\"\\s+from\\s+\"(?<value>.*?)\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Parses a binder error message and extracts the parameter name, parameter value,
        /// and the type hint, if available, from the message.
        /// </summary>
        /// <param name="message">The error message to parse, typically generated from a binder failure.</param>
        /// <returns>
        /// A tuple containing:
        /// - The parameter name as a string, or null if not found.
        /// - The parameter value as a string, or null if not found.
        /// - The type hint as a string, or null if not found.
        /// </returns>
        public static (string? name, string? value, string? typeHint) Parse(string message)
        {
            var m = Rx.Match(message);
            if (!m.Success) return (null, null, null);
            return (m.Groups["name"].Value, m.Groups["value"].Value, m.Groups["type"].Value);
        }

        /// <summary>
        /// Extracts the underlying type from a nullable type representation in string format.
        /// </summary>
        /// <param name="raw">The raw string representation of the nullable type,
        /// possibly in the format "Nullable<T>" or "Nullable`1[T]".</param>
        /// <returns>
        /// The extracted inner type as a string if the input represents a nullable type;
        /// otherwise, returns the original input string.
        /// </returns>
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