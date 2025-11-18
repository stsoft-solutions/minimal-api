using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Sts.Minimal.Api.Infrastructure.Middleware;

/// <summary>
/// Handles exceptions of type <see cref="BadHttpRequestException" /> that occur due to
/// failed parameter binding. Converts the exception into a standardized
/// validation problem response compatible with API conventions.
/// </summary>
/// <remarks>
/// This handler is specifically designed to process exceptions where the
/// message starts with "Failed to bind parameter". It extracts error
/// details from the exception and generates a <see cref="ValidationProblemDetails" />
/// response, providing meaningful feedback about invalid parameters.
/// </remarks>
/// <seealso cref="IExceptionHandler" />
public sealed partial class BadHttpRequestToValidationHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public BadHttpRequestToValidationHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService ?? throw new ArgumentNullException(nameof(problemDetailsService));
    }

    /// <summary>
    /// Attempts to handle a <see cref="BadHttpRequestException" /> by generating a validation error response.
    /// </summary>
    /// <param name="context">The current HTTP context associated with the request.</param>
    /// <param name="ex">
    /// The exception to handle, which must be of type <see cref="BadHttpRequestException" /> to proceed with
    /// handling.
    /// </param>
    /// <param name="token">A <see cref="CancellationToken" /> to observe while performing the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{Boolean}" /> indicating whether the exception was successfully handled. Returns true if
    /// handled, false otherwise.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception ex, CancellationToken token)
    {
        if (ex is not BadHttpRequestException badHttpRequestException)
            return false;

        badHttpRequestException.Data.Add("BadHttpRequestToValidationHandler", true);

        var (name, value, typeHintRaw, isRequiredMissing) = BinderMessageParser.Parse(badHttpRequestException.Message);
        var typeHint = BinderMessageParser.UnwrapNullable(typeHintRaw);

        // Try to map CLR parameter name to the public query parameter name (FromQuery.Name)
        var publicName = MapToQueryParameterName(context, name);

        var vpd = new ValidationProblemDetails(
            new Dictionary<string, string[]>
            {
                [publicName ?? "unknownParameter"] = [FriendlyError(typeHint, value, isRequiredMissing)]
            }
        )
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more parameters are invalid."
        };

        // Explicitly set the HTTP response status to 400 to avoid the default 500 from the exception handler
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await _problemDetailsService.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = vpd });
        return true; // ← tells middleware it's handled (no error log)
    }

    /// <summary>
    /// Maps a CLR handler parameter name to its corresponding query parameter name
    /// as exposed via the <see cref="FromQueryAttribute" /> Name property.
    /// Falls back to the original name if mapping is unavailable.
    /// </summary>
    private static string? MapToQueryParameterName(HttpContext httpContext, string? originalName)
    {
        if (string.IsNullOrWhiteSpace(originalName)) return originalName;

        // Get handler MethodInfo from endpoint metadata (Minimal APIs add MethodInfo to metadata)
        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
        {
            // When we are already in the exception branch, the selected endpoint can be null.
            // Fall back to scanning EndpointDataSource for a matching endpoint by HTTP method
            // and then try to resolve the FromQuery(Name) from its parameter metadata.
            try
            {
                var dataSource = httpContext.RequestServices.GetService(typeof(EndpointDataSource)) as EndpointDataSource;
                var httpMethod = httpContext.Request.Method;
                if (dataSource is not null)
                    foreach (var ep in dataSource.Endpoints)
                    {
                        var methodMeta = ep.Metadata.GetMetadata<HttpMethodMetadata>();
                        if (methodMeta is not null && !methodMeta.HttpMethods.Contains(httpMethod, StringComparer.OrdinalIgnoreCase))
                            continue; // different HTTP method

                        var resolved = ResolveFromEndpointMetadata(ep, originalName);
                        if (resolved is { Length: > 0 }) return resolved;
                    }
            }
            catch
            {
                // ignore and fall back to the original name
            }

            return originalName;
        }

        var method = endpoint.Metadata.GetMetadata<MethodInfo>();

        // Some hosting scenarios wrap metadata; also check RouteEndpoint's metadata bag
        method ??= (endpoint as RouteEndpoint)?.Metadata.GetMetadata<MethodInfo>();

        // Primary path: use MethodInfo when available
        if (method is not null)
        {
            var param = method
                .GetParameters()
                .FirstOrDefault(p => string.Equals(p.Name, originalName, StringComparison.OrdinalIgnoreCase));

            var customName = GetFromQueryCustomName(param);
            if (!string.IsNullOrEmpty(customName)) return customName;
        }

        // Fallback: Some Minimal API setups expose ParameterInfo items directly in endpoint metadata
        try
        {
            var parameters = endpoint.Metadata.GetOrderedMetadata<ParameterInfo>();
            var p2 = parameters.FirstOrDefault(p => string.Equals(p.Name, originalName, StringComparison.OrdinalIgnoreCase));
            var custom2 = GetFromQueryCustomName(p2);
            if (!string.IsNullOrEmpty(custom2)) return custom2;
        }
        catch
        {
            // ignore and fall back
        }

        // Last chance: if the current endpoint didn't help, try a limited scan over EndpointDataSource
        try
        {
            var dataSource = httpContext.RequestServices.GetService(typeof(EndpointDataSource)) as EndpointDataSource;
            var httpMethod = httpContext.Request.Method;
            if (dataSource is not null)
                foreach (var ep in dataSource.Endpoints)
                {
                    var methodMeta = ep.Metadata.GetMetadata<HttpMethodMetadata>();
                    if (methodMeta is not null && !methodMeta.HttpMethods.Contains(httpMethod, StringComparer.OrdinalIgnoreCase))
                        continue;

                    var resolved = ResolveFromEndpointMetadata(ep, originalName);
                    if (!string.IsNullOrEmpty(resolved)) return resolved;
                }
        }
        catch
        {
            // ignore
        }

        return originalName;
    }

    /// <summary>
    /// Returns the custom query name specified via <see cref="FromQueryAttribute.Name"/> for the supplied parameter,
    /// or null when not present.
    /// </summary>
    private static string? GetFromQueryCustomName(ParameterInfo? parameter)
    {
        var attr = parameter?.GetCustomAttribute<FromQueryAttribute>();
        return !string.IsNullOrEmpty(attr?.Name) ? attr!.Name : null;
    }

    /// <summary>
    /// Attempts to resolve the public query name for a CLR parameter from a given <see cref="Endpoint"/> metadata.
    /// Tries both the <see cref="MethodInfo"/> parameters and the metadata bag of <see cref="ParameterInfo"/>.
    /// Returns null if not found.
    /// </summary>
    private static string? ResolveFromEndpointMetadata(Endpoint ep, string originalName)
    {
        // Try MethodInfo path first
        var mi = ep.Metadata.GetMetadata<MethodInfo>()
                 ?? (ep as RouteEndpoint)?.Metadata.GetMetadata<MethodInfo>();
        if (mi is not null)
        {
            var p = mi.GetParameters().FirstOrDefault(p => string.Equals(p.Name, originalName, StringComparison.OrdinalIgnoreCase));
            var custom = GetFromQueryCustomName(p);
            if (!string.IsNullOrEmpty(custom)) return custom;
        }

        // Fallback to ParameterInfo metadata bag
        try
        {
            var parameters = ep.Metadata.GetOrderedMetadata<ParameterInfo>();
            var p2 = parameters.FirstOrDefault(p => string.Equals(p.Name, originalName, StringComparison.OrdinalIgnoreCase));
            var custom2 = GetFromQueryCustomName(p2);
            if (!string.IsNullOrEmpty(custom2)) return custom2;
        }
        catch
        {
            // ignore
        }

        return null;
    }

    /// <summary>
    /// Generates a user-friendly error message based on the parameter type, provided value, and whether the parameter was missing.
    /// </summary>
    /// <param name="typeHint">A hint describing the expected type of the parameter, such as "int", "GUID", or "bool".</param>
    /// <param name="value">The value of the parameter, if supplied, or null if not provided.</param>
    /// <param name="isRequiredMissing">A boolean indicating whether the parameter was required but missing.</param>
    /// <returns>A user-friendly error message describing the issue with the provided parameter.</returns>
    private static string FriendlyError(string? typeHint, string? value, bool isRequiredMissing)
    {
        if (isRequiredMissing)
            return "Required parameter is missing.";
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
    private static partial class BinderMessageParser
    {
        // Regex pattern for parsing messages generated during model binding failures.
        // Example: "Failed to bind parameter 'value' from '123'."
        private static readonly Regex Rx = new(
            "Failed to bind parameter\\s+\"(?<type>[^\\s\"<>`]+(?:<[^>]+>)?(?:`\\d+\\[[^\\]]+\\])?)\\s+(?<name>\\w+)\"\\s+from\\s+\"(?<value>.*?)\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Regex for missing required parameter from query string
        // Example: "Required parameter \"PaymentStatus statusEnum\" was not provided from query string."
        private static readonly Regex RxRequired = new(
            "^Required parameter\\s+\"(?<type>[^\\s\"<>`]+(?:<[^>]+>)?(?:`\\d+\\[[^\\]]+\\])?)\\s+(?<name>\\w+)\"\\s+was not provided from query string\\.?$",
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
        public static (string? name, string? value, string? typeHint, bool requiredMissing) Parse(string message)
        {
            var m = Rx.Match(message);
            if (m.Success) return (m.Groups["name"].Value, m.Groups["value"].Value, m.Groups["type"].Value, false);

            var r = RxRequired.Match(message);
            if (r.Success) return (r.Groups["name"].Value, null, r.Groups["type"].Value, true);

            return (null, null, null, false);
        }

        /// <summary>
        /// Extracts the underlying type from a nullable type representation in string format.
        /// </summary>
        /// <param name="raw">
        /// The raw string representation of the nullable type,
        /// possibly in the format Nullable&lt;T&gt; or Nullable`1[T].
        /// </param>
        /// <returns>
        /// The extracted inner type as a string if the input represents a nullable type;
        /// otherwise, returns the original input string.
        /// </returns>
        public static string? UnwrapNullable(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            var a = NullableRegex1().Match(raw);
            if (a.Success) return a.Groups["inner"].Value;
            var b = NullableRegex2().Match(raw);
            if (b.Success) return b.Groups["inner"].Value;
            return raw;
        }

        [GeneratedRegex("^Nullable<(?<inner>[^>]+)>$")]
        private static partial Regex NullableRegex1();

        [GeneratedRegex(@"^Nullable`\d+\[(?<inner>[^\]]+)\]$")]
        private static partial Regex NullableRegex2();
    }
}