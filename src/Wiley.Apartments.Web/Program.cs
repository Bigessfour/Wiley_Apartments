using Serilog;
using Wiley.Apartments.Web.Components;
using Wiley.Apartments.Web.Infrastructure;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddClerkSuiteSerilog();
    builder.AddClerkSuiteServices();

    var app = builder.Build();
    await app.InitializeClerkSuiteAsync();
    app.ConfigureClerkSuitePipeline();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ClerkSuite terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
