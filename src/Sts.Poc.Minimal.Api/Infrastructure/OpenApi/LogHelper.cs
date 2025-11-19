using Serilog;

namespace Sts.Poc.Minimal.Api.Infrastructure.OpenApi;

public static class LogHelper
{
    public static void EnrichFromHttpContext(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        var request = httpContext.Request;

        // Set all the common properties available for every request
        diagnosticContext.Set("Host", request.Host);
        diagnosticContext.Set("Protocol", request.Protocol);
        diagnosticContext.Set("Scheme", request.Scheme);

        // Only set it if available. You're not sending sensitive data in a querystring right?!
        if (request.QueryString.HasValue) diagnosticContext.Set("QueryString", request.QueryString.Value);

        // Set the content-type of the Response at this point
        diagnosticContext.Set("ContentType", httpContext.Response.ContentType ?? string.Empty);

        // Retrieve the IEndpointFeature selected for the request
        var endpoint = httpContext.GetEndpoint();
        if (endpoint is not null) // endpoint != null
            diagnosticContext.Set("EndpointName", endpoint.DisplayName ?? string.Empty);
    }
}