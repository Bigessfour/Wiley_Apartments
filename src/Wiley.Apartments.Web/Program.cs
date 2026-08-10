using Serilog;
using Wiley.Apartments.Web.Components;
using Wiley.Apartments.Web.Infrastructure;

try
{
    var builder = WebApplication.CreateBuilder(args);
    // Enable static web assets for `dotnet run` outside Development (avoids FileNotFound
    // for Syncfusion/_content and scoped CSS when ASPNETCORE_ENVIRONMENT=Production).
    builder.WebHost.UseStaticWebAssets();
    builder.AddClerkSuiteSerilog();
    builder.AddClerkSuiteServices();

    var app = builder.Build();
    await app.InitializeClerkSuiteAsync();
    app.ConfigureClerkSuitePipeline();

    app.MapStaticAssets();
    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.MapGet("/api/documents/{id:guid}/download", async (
            Guid id,
            Wiley.Apartments.Contracts.IDocumentService documents,
            CancellationToken ct) =>
        {
            var info = await documents.GetByIdAsync(id, ct);
            var path = await documents.ResolveAbsolutePathAsync(id, ct);
            if (info is null || path is null)
            {
                return Results.NotFound();
            }

            return Results.File(
                path,
                info.ContentType ?? "application/octet-stream",
                info.OriginalFileName);
        })
        .RequireAuthorization();

    var urls = builder.Configuration["ASPNETCORE_URLS"]
        ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
        ?? "http://localhost:5077";
    Log.Information("ClerkSuite is ready. Open {LoginUrl} (Ctrl+C to stop). File logs: {LogDir}",
        $"{urls.TrimEnd('/')}/Account/Login",
        Path.Combine(app.Environment.ContentRootPath, "logs"));

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "ClerkSuite terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
