using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class DocumentPathResolver(
    IServiceScopeFactory scopeFactory,
    IOptions<ClerkSuiteOptions> options,
    IHostEnvironment environment,
    ILogger<DocumentPathResolver> logger) : IDocumentPathResolver
{
    public const string DocumentRootKey = "DocumentRoot";

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ClerkSuiteOptions _options = options.Value;
    private readonly IHostEnvironment _environment = environment;
    private readonly ILogger<DocumentPathResolver> _logger = logger;
    private readonly Lock _gate = new();
    private string? _cachedRoot;

    public string ConfiguredDefaultRoot => Normalize(_options.DocumentRoot);

    public string GetDocumentRoot()
    {
        lock (_gate)
        {
            if (_cachedRoot is not null)
            {
                return _cachedRoot;
            }
        }

        try
        {
            return GetDocumentRootAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falling back to configured DocumentRoot.");
            return ConfiguredDefaultRoot;
        }
    }

    public async Task<string> GetDocumentRootAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_cachedRoot is not null)
            {
                return _cachedRoot;
            }
        }

        string root;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApartmentsDbContext>();
        var overrideRow = await db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == DocumentRootKey, cancellationToken);
        root = string.IsNullOrWhiteSpace(overrideRow?.Value)
            ? ConfiguredDefaultRoot
            : Normalize(overrideRow!.Value);

        lock (_gate)
        {
            _cachedRoot = root;
        }

        return root;
    }

    public async Task SetDocumentRootAsync(
        string absoluteOrRelativePath,
        string? updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(absoluteOrRelativePath))
        {
            throw new ArgumentException("Document storage path is required.", nameof(absoluteOrRelativePath));
        }

        var normalized = Normalize(absoluteOrRelativePath.Trim());
        Directory.CreateDirectory(normalized);
        DocumentRootAvailability.EnsureWritable(normalized);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApartmentsDbContext>();
        var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == DocumentRootKey, cancellationToken);
        if (row is null)
        {
            row = new AppSetting { Key = DocumentRootKey };
            db.AppSettings.Add(row);
        }

        row.Value = normalized;
        row.UpdatedUtc = DateTime.UtcNow;
        row.UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "clerk" : updatedBy.Trim();
        await db.SaveChangesAsync(cancellationToken);

        lock (_gate)
        {
            _cachedRoot = normalized;
        }

        _logger.LogInformation(
            "DocumentRoot override set to {Root} by {User}.",
            normalized,
            row.UpdatedBy);
    }

    private string Normalize(string root) =>
        Path.IsPathRooted(root)
            ? Path.GetFullPath(root)
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, root));
}
