using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Syncfusion.Blazor;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Web.Infrastructure;

public static class ClerkSuiteServiceExtensions
{
    public static WebApplicationBuilder AddClerkSuiteServices(this WebApplicationBuilder builder)
    {
        SyncfusionLicenseBootstrap.RegisterFromConfiguration(
            builder.Configuration,
            builder.Environment,
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Syncfusion"));

        builder.Services.Configure<ClerkSuiteOptions>(
            builder.Configuration.GetSection(ClerkSuiteOptions.SectionName));
        builder.Services.Configure<SeedUserOptions>(
            builder.Configuration.GetSection(SeedUserOptions.SectionName));

        builder.Services.AddMemoryCache();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<CircuitAuthCookieStore>();
        builder.Services.AddScoped<CircuitAuthCookieAccessor>();
        builder.Services.AddSingleton<IDateTimeService, DateTimeService>();
        builder.Services.AddSingleton<IDocumentPathResolver, DocumentPathResolver>();
        builder.Services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();
        builder.Services.AddScoped<AuditSaveChangesInterceptor>();
        builder.Services.AddScoped<IIdentitySeeder, IdentitySeeder>();
        builder.Services.AddScoped<IUnitSeeder, UnitSeeder>();
        builder.Services.AddScoped<IUnitService, UnitService>();
        builder.Services.AddScoped<ITenantService, TenantService>();
        builder.Services.AddScoped<IOccupancyService, OccupancyService>();
        builder.Services.AddScoped<IAssetService, AssetService>();
        builder.Services.AddScoped<IFlooringService, FlooringService>();
        builder.Services.AddSingleton<LeaseDocumentGenerator>();
        builder.Services.AddSingleton<IElectronicSignatureHook, NullElectronicSignatureHook>();
        builder.Services.AddScoped<IDocumentService, DocumentService>();
        builder.Services.AddScoped<ILeaseService, LeaseService>();
        builder.Services.AddScoped<IScheduleService, ScheduleService>();
        builder.Services.AddScoped<ILateFeeSettingsService, LateFeeSettingsService>();
        builder.Services.AddScoped<ILedgerService, LedgerService>();
        builder.Services.AddScoped<PaymentReceiptGenerator>();
        builder.Services.AddScoped<IPaymentReceiptService, PaymentReceiptService>();
        builder.Services.AddScoped<IRentRollService, RentRollService>();
        builder.Services.AddScoped<IUnitOperatingCostService, UnitOperatingCostService>();
        builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
        builder.Services.AddScoped<IFacilityRenterService, FacilityRenterService>();
        builder.Services.AddScoped<IFacilityReservationService, FacilityReservationService>();
        builder.Services.AddScoped<IFacilityInspectionService, FacilityInspectionService>();
        builder.Services.AddScoped<IFacilityInventoryService, FacilityInventoryService>();
        builder.Services.AddSingleton<FacilityRentalAgreementGenerator>();
        builder.Services.AddScoped<IFacilityRentalAgreementService, FacilityRentalAgreementService>();
        builder.Services.AddScoped<IDashboardService, DashboardService>();
        builder.Services.AddScoped<IPortfolioProfitLossService, PortfolioProfitLossService>();
        builder.Services.AddScoped<IAuditQueryService, AuditQueryService>();
        builder.Services.AddScoped<IDocumentVaultAuditService, DocumentVaultAuditService>();
        builder.Services.AddScoped<IDocumentVaultMetadataSync, DocumentVaultMetadataSync>();
        builder.Services.AddScoped<ClerkToastService>();
        builder.Services.AddScoped<IClerkToast>(sp => sp.GetRequiredService<ClerkToastService>());
        builder.Services.AddScoped<DocumentVaultAntiforgeryFilter>();
        builder.Services.AddHealthChecks()
            .AddCheck<DocumentRootHealthCheck>("document-root");

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=Data/clerksuite.db";

        builder.Services.AddDbContext<ApartmentsDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<ApartmentsDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        }).AddIdentityCookies();

        builder.Services.AddAuthorization();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddControllers();
        builder.Services.AddSyncfusionBlazor();
        builder.Services.AddScoped<CircuitHandler, LoggingCircuitHandler>();
        builder.Services.AddScoped<CircuitHandler, CircuitAuthCookieHandler>();
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents(options =>
            {
                options.DetailedErrors = builder.Environment.IsDevelopment();
            });

        return builder;
    }

    public static async Task InitializeClerkSuiteAsync(this WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApartmentsDbContext>();
        await db.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<IIdentitySeeder>();
        await seeder.SeedAsync();

        var unitSeeder = scope.ServiceProvider.GetRequiredService<IUnitSeeder>();
        await unitSeeder.SeedAsync();

        var residentialCount = await db.Units.CountAsync(u => !u.IsFacility);
        var facilityCount = await db.Units.CountAsync(u => u.IsFacility);
        Log.Information(
            "Database migrated. {ResidentialCount} residential + {FacilityCount} facility unit(s).",
            residentialCount,
            facilityCount);
    }

    public static WebApplication ConfigureClerkSuitePipeline(this WebApplication app)
    {
        app.UseClerkSuiteSerilogRequestLogging();

        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        if (!app.Environment.IsEnvironment("Testing"))
        {
            var urls = app.Configuration["ASPNETCORE_URLS"]
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                ?? string.Empty;
            if (urls.Contains("https://", StringComparison.OrdinalIgnoreCase))
            {
                app.UseHttpsRedirection();
            }
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        return app;
    }
}
