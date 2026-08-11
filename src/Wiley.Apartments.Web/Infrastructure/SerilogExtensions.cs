using Serilog;
using Serilog.Events;

namespace Wiley.Apartments.Web.Infrastructure;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddClerkSuiteSerilog(this WebApplicationBuilder builder)
    {
        var contentRoot = builder.Environment.ContentRootPath;
        var logDir = Path.Combine(contentRoot, "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "clerksuite-.log");

        // Surface Serilog sink/config failures to stderr (otherwise File sink can silently no-op).
        Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine("[Serilog SelfLog] " + msg));

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Components", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.Components.Server", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.Components.Server.Circuits", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", "ClerkSuite")
                .Enrich.WithProperty("Repository", "Wiley.Apartments")
                .WriteTo.File(
                    path: logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true,
                    outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}");

            if (context.Configuration.GetValue<bool>("Serilog:WriteToConsole", true))
            {
                configuration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");
            }
        });

        return builder;
    }

    public static WebApplication UseClerkSuiteSerilogRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex is not null || httpContext.Response.StatusCode >= 500)
                {
                    return LogEventLevel.Error;
                }

                if (httpContext.Response.StatusCode >= 400)
                {
                    return LogEventLevel.Warning;
                }

                return LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserName", httpContext.User.Identity?.Name ?? "anonymous");
                diagnosticContext.Set("TraceIdentifier", httpContext.TraceIdentifier);
            };
        });

        return app;
    }
}
