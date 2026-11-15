using System.Net;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        builder.Services.AddScoped<SharedContractOperationalEventExportService>();
        builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
        builder.Services.AddScoped<IFacilityRenterService, FacilityRenterService>();
        builder.Services.AddScoped<IFacilityReservationService, FacilityReservationService>();
        builder.Services.AddScoped<IFacilityRentalRateService, FacilityRentalRateService>();
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
        builder.Services.AddScoped<ClerkSuiteThemeState>();
        builder.Services.AddScoped<DocumentVaultAntiforgeryFilter>();
        builder.Services.AddHealthChecks()
            .AddCheck<DocumentRootHealthCheck>("document-root");

        var dataProtectionKeys = Directory.Exists("/data")
            ? "/data/dpkeys"
            : Path.Combine(builder.Environment.ContentRootPath, "Data", "dpkeys");
        Directory.CreateDirectory(dataProtectionKeys);
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeys))
            .SetApplicationName("ClerkSuite");

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=Data/clerksuite.db";
        var databaseProvider = builder.Configuration[$"{ClerkSuiteOptions.SectionName}:DatabaseProvider"]
            ?? "Sqlite";

        builder.Services.AddDbContext<ApartmentsDbContext>((sp, options) =>
        {
            // Default remains Sqlite for local Mac + Synology clerk workflow.
            // Set ClerkSuite__DatabaseProvider=Postgres (+ Npgsql connection string) for NAS/Aurora.
            var usePostgres = databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
                || databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase);
            if (usePostgres)
            {
                options.UseNpgsql(connectionString);
                // Historical migrations were authored for SQLite; model still matches at runtime.
                // Ignore pending-model warning so MigrateAsync can apply the existing chain on Postgres.
                options.ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            }
            else
            {
                options.UseSqlite(connectionString);
            }

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

        builder.Services.ConfigureApplicationCookie(options =>
        {
            // ClerkSuite on the office tailnet is http://mr-storage:8082. SameAsRequest keeps that cookie.
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.LoginPath = "/Account/Login";
        });

        builder.Services.AddAuthorization();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddControllers();
        builder.Services.AddSyncfusionBlazor();
        builder.Services.AddScoped<CircuitHandler, LoggingCircuitHandler>();
        builder.Services.AddScoped<CircuitHandler, CircuitAuthCookieHandler>();
        builder.Services.AddSignalR(options =>
        {
            options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
        });
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
        app.UseClerkSuiteForwardedHeaders();
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
        app.Use(async (context, next) =>
        {
            var options = context.RequestServices.GetRequiredService<IOptions<ClerkSuiteOptions>>().Value;
            if (HttpMethods.IsGet(context.Request.Method)
                && context.Request.Path.StartsWithSegments("/Account/Login")
                && context.User.Identity?.IsAuthenticated != true
                && !string.IsNullOrWhiteSpace(options.CognitoDomain))
            {
                var safe = CognitoStaffAuth.SafeReturnPath(context.Request.Query["ReturnUrl"]);
                context.Response.Redirect("/authentication/cognito-login?return=" + Uri.EscapeDataString(safe));
                return;
            }

            if (CognitoStaffAuth.ShouldProbe(context))
            {
                var raw = (context.Request.Path.Value ?? "/") + context.Request.QueryString.Value;
                var target = CognitoStaffAuth.SafeReturnPath(raw);
                context.Response.Redirect("/authentication/cognito-start?return=" + Uri.EscapeDataString(target));
                return;
            }

            await next();
        });
        app.UseAuthorization();
        app.UseAntiforgery();

        return app;
    }

    /// <summary>
    /// Tailscale Serve on the NAS connects from loopback and sets the public scheme and host.
    /// Development and test hosts are reached directly, so they ignore forwarded headers.
    /// </summary>
    private static void UseClerkSuiteForwardedHeaders(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        var forwarded = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
        };
        forwarded.KnownNetworks.Clear();
        forwarded.KnownProxies.Clear();
        forwarded.KnownProxies.Add(IPAddress.Loopback);
        forwarded.KnownProxies.Add(IPAddress.IPv6Loopback);
        app.UseForwardedHeaders(forwarded);
    }
}
