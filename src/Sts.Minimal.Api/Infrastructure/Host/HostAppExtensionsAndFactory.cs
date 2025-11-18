using Microsoft.AspNetCore.HostFiltering;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

namespace Sts.Minimal.Api.Infrastructure.Host;

public static class HostAppExtensionsAndFactory
{
    public static WebApplicationBuilder CreateStsHostBuilder(string[] args)
    {
        var serviceVersion = typeof(HostAppExtensionsAndFactory).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog and ensure Log.Logger is initialized before registering exception handlers.
        try
        {
            CreateSerilog(builder);
        }
        catch (Exception ex)
        {
            // If Serilog fails to initialize, write to the console and rethrow.
            Console.Error.WriteLine($"Failed to initialize Serilog: {ex}");
            throw;
        }

        // Global exception processing for domain and tasks
        // NOTE: These handlers must be registered only after Log.Logger is initialized.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Log.Fatal(ex, "Unhandled exception in AppDomain");
            else
                Log.Fatal("Unhandled non-exception object in AppDomain: {ExceptionObject}", e.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Fatal(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        // Ensure flush on normal shutdown paths too
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Log.CloseAndFlush();
        Console.CancelKeyPress += (_, _) => Log.CloseAndFlush();

        // Configure Host Filtering from configuration
        builder.Services.Configure<HostFilteringOptions>(
            builder.Configuration.GetSection("HostFiltering"));

        Log.Information("Starting {Application} v{ServiceVersion} on {Environment} environment",
            builder.Environment.ApplicationName, serviceVersion, builder.Environment.EnvironmentName);

        // OpenTelemetry Tracing & Metrics
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(builder.Environment.ApplicationName, serviceVersion: serviceVersion,
                serviceInstanceId: Environment.MachineName);

        var otlpEndpointUrl = builder.Configuration.GetValue<string>("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrEmpty(otlpEndpointUrl) &&
            Uri.TryCreate(otlpEndpointUrl, UriKind.Absolute, out var otlpEndpoint) &&
            (otlpEndpoint.Scheme == Uri.UriSchemeHttp || otlpEndpoint.Scheme == Uri.UriSchemeHttps))
        {
            // OTLP endpoint must be a valid URL
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddEntityFrameworkCoreInstrumentation(options =>
                        {
                            options.SetDbStatementForStoredProcedure = true;
                            options.SetDbStatementForText = true;
                        })
                        .AddOtlpExporter();
                })
                .WithMetrics(meterProviderBuilder =>
                {
                    meterProviderBuilder
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddOtlpExporter();
                });

            Log.Information("OpenTelemetry enabled with OTLP endpoint: {OtlpEndpoint}", otlpEndpointUrl);
        }
        else
        {
            Log.Information(
                "OTLP endpoint not configured. Set environment variable OTEL_EXPORTER_OTLP_ENDPOINT (http://localhost:4317 or http://localhost:17011)");
        }

        return builder;
    }

    private static LoggerConfiguration SerilogConfiguration(WebApplicationBuilder builder)
    {
        var configuration = new LoggerConfiguration()
                // Load configuration from configuration (appsettings.json)
                .ReadFrom.Configuration(builder.Configuration)
                // Enrich logs with additional properties
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithSpan()
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)

                // Suppress the specific error log for binder failures (BadHttpRequestException: "Failed to bind parameter ...")
                // Scope to logs emitted by ExceptionHandlerMiddleware to avoid hiding other sources
                .Filter.ByExcluding(e =>
                {
                    var result = e is { Level: LogEventLevel.Error, Exception: BadHttpRequestException ex } &&
                                 (ex.Message?.StartsWith("Failed to bind parameter") ?? false) &&
                                 e.Properties.TryGetValue("SourceContext", out var sc) &&
                                 sc is ScalarValue { Value: string src } &&
                                 src.Contains("ExceptionHandlerMiddleware");

                    return result;
                })
            ;

        return configuration;
    }

    /// <summary>
    /// Configures and initializes the static <see cref="Log.Logger" /> instance for the application using Serilog.
    /// This method does not return a value.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder" /> object used to configure the application.</param>
    private static void CreateSerilog(WebApplicationBuilder builder)
    {
        var configuration = SerilogConfiguration(builder);

        var logger = configuration.CreateLogger();

        // Ensure the static logger is the same instance and will be flushed/disposed
        Log.Logger = logger;

        builder.Logging.ClearProviders();

        // Prefer host integration to manage lifetime and disposal
        builder.Host.UseSerilog(logger, true);
    }

    public static IApplicationBuilder UseStsHost(this IApplicationBuilder app)
    {
        // Configure logging middleware
        // app.UseSerilogRequestLogging(options =>
        // {
        //     options.IncludeQueryInRequestPath = true;
        //
        //     // Emit debug-level events instead of the defaults
        //     options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Debug;
        //
        //     // Attach additional properties to the request completion event
        //     options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        //     {
        //         diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
        //         diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        //     };
        // });

        return app;
    }
}