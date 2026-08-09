using Serilog;
using Serilog.Events;

namespace Wiley.Apartments.Web.Infrastructure;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddClerkSuiteSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", "ClerkSuite")
                .Enrich.WithProperty("Repository", "Wiley.Apartments");

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
