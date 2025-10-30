using Serilog;
using Serilog.Core;
using Serilog.Enrichers.Span;

namespace Sts.Minimal.Api.Infrastructure.Host;

public static class HostAppExtensionsAndFactory
{
    public static WebApplicationBuilder CreateStsHostBuilder(string[] args)
    {
        var serviceVersion = typeof(HostAppExtensionsAndFactory).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        var builder = WebApplication.CreateBuilder(args);

        // Configure serilog
        var logger = CreateSerilog(builder);

        // Global exception processing for domain and tasks
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                logger.Fatal(ex, "Unhandled exception in AppDomain");
            else
                logger.Fatal("Unhandled non-exception object in AppDomain: {ExceptionObject}", e.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            logger.Fatal(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        // Ensure flush on normal shutdown paths too
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Log.CloseAndFlush();
        Console.CancelKeyPress += (_, _) => Log.CloseAndFlush();

        logger.Information("Starting {Application} v{ServiceVersion} on {Environment} environment",
            builder.Environment.ApplicationName, serviceVersion, builder.Environment.EnvironmentName);

        // // OpenTelemetry Tracing & Metrics
        // var resourceBuilder = ResourceBuilder.CreateDefault()
        //     .AddService(builder.Environment.ApplicationName, serviceVersion: serviceVersion,
        //         serviceInstanceId: Environment.MachineName);
        //
        // var otlpEndpointUrl = builder.Configuration.GetConnectionString("OTLP_ENDPOINT");

        //if (string.IsNullOrEmpty(otlpEndpointUrl)) return builder;

        // var otlpEndpoint = new Uri(otlpEndpointUrl);
        // builder.Services.AddOpenTelemetry()
        //     .WithTracing(tracerProviderBuilder =>
        //     {
        //         tracerProviderBuilder
        //             .SetResourceBuilder(resourceBuilder)
        //             .AddSource(RabbitMqConsumerBase.OtlpSourceName, RabbitMqPublisher.OtlpSourceName)
        //             .AddAspNetCoreInstrumentation()
        //             .AddHttpClientInstrumentation()
        //             .AddEntityFrameworkCoreInstrumentation(options =>
        //             {
        //                 options.SetDbStatementForStoredProcedure = true;
        //                 options.SetDbStatementForText = true;
        //             })
        //             .AddKeycloakAuthServicesInstrumentation()
        //             .AddOtlpExporter(options => { options.Endpoint = otlpEndpoint; });
        //     })
        //     .WithMetrics(meterProviderBuilder =>
        //     {
        //         meterProviderBuilder
        //             .SetResourceBuilder(resourceBuilder)
        //             .AddAspNetCoreInstrumentation()
        //             .AddHttpClientInstrumentation()
        //             .AddRuntimeInstrumentation()
        //             .AddKeycloakAuthServicesInstrumentation()
        //             .AddOtlpExporter(options => { options.Endpoint = otlpEndpoint; });
        //     });
        //
        // logger.Information("OpenTelemetry enabled with OTLP endpoint: {OtlpEndpoint}", otlpEndpointUrl);

        return builder;
    }

    private static LoggerConfiguration SerilogConfiguration(WebApplicationBuilder builder)
    {
        var configuration = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .Enrich.WithProperty("Instance", Environment.MachineName);

        return configuration;
    }

    /// <summary>
    /// Configures and initializes a Serilog logger instance for the application.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder" /> object used to configure the application.</param>
    /// <returns>A configured <see cref="Logger" /> instance used as the application's logging provider.</returns>
    private static Logger CreateSerilog(WebApplicationBuilder builder)
    {
        var configuration = SerilogConfiguration(builder);

        var logger = configuration.CreateLogger();

        // Ensure the static logger is the same instance and will be flushed/disposed
        Log.Logger = logger;

        builder.Logging.ClearProviders();

        // Prefer host integration to manage lifetime and disposal
        builder.Host.UseSerilog(logger, true);

        return logger;
    }

    public static WebApplication UseStsHost(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        return app;
    }
}