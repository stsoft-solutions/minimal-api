using Microsoft.AspNetCore.Diagnostics;
using Serilog.Context;
using Sts.Minimal.Api.Features.Payment;
using Sts.Minimal.Api.Infrastructure.Host;
using Sts.Minimal.Api.Infrastructure.OpenApi;

// Create host builder with Serilog & OTLP
var builder = HostAppExtensionsAndFactory.CreateStsHostBuilder(args);

var services = builder.Services;

// Add OpenAPI infrastructure services
services.AddOpenApiInfrastructure();

var app = builder.Build();

// Enrich all logs in this request with TraceId from HttpContext
app.Use(async (context, next) =>
{
    using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
    {
        await next();
    }
});

// Developer-friendly error page in Development
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

// Log and format unhandled exceptions as ProblemDetails
app.UseExceptionHandler(errorApp =>
{
    // errorApp.Run(async context =>
    // {
    //     context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    //     context.Response.ContentType = "application/problem+json";
    //
    //     var feature = context.Features.Get<IExceptionHandlerFeature>();
    //     var exception = feature?.Error;
    //
    //     // Use Microsoft.Extensions.Logging; Serilog is the provider underneath
    //     var logger = context.RequestServices
    //         .GetRequiredService<ILoggerFactory>()
    //         .CreateLogger("GlobalExceptionHandler");
    //
    //     if (exception is not null)
    //         logger.LogError(exception, "Unhandled exception for {Method} {Path}. TraceId={TraceId}",
    //             context.Request.Method, context.Request.Path, context.TraceIdentifier);
    //     else
    //         logger.LogError("Unhandled error for {Method} {Path}. TraceId={TraceId}",
    //             context.Request.Method, context.Request.Path, context.TraceIdentifier);
    //
    //     var problem = Results.Problem(
    //         title: "An error occurred while processing your request.1",
    //         statusCode: StatusCodes.Status500InternalServerError,
    //         type: "https://tools.ietf.org/html/rfc9110#section-15.6.1",
    //         extensions: new Dictionary<string, object?>
    //         {
    //             ["traceId"] = context.TraceIdentifier
    //         });
    //
    //     await problem.ExecuteAsync(context);
    // });
});

app.UseStsHost();

// Use OpenAPI infrastructure middleware
app.UseOpenApiInfrastructure();

// Map payment-related API endpoints
app.MapPaymentEndpoints();

app.Run();