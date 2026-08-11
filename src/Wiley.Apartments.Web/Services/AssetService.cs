using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class AssetService(ApartmentsDbContext db, ILogger<AssetService> logger) : IAssetService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly ILogger<AssetService> _logger = logger;

    public async Task<IReadOnlyList<Asset>> GetByUnitIdAsync(
        Guid unitId,
        CancellationToken cancellationToken = default) =>
        await _db.Assets
            .AsNoTracking()
            .Where(a => a.UnitId == unitId)
            .OrderBy(a => a.Type)
            .ThenBy(a => a.Make)
            .ToListAsync(cancellationToken);

    public async Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Assets.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Asset>> SearchBySerialAsync(
        string serial,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serial))
        {
            return [];
        }

        var term = EscapeLikePattern(serial.Trim());
        return await _db.Assets
            .AsNoTracking()
            .Where(a => EF.Functions.Like(a.Serial, $"%{term}%", "\\"))
            .OrderBy(a => a.Serial)
            .ToListAsync(cancellationToken);
    }

    public async Task<Asset> CreateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        ValidateAsset(asset);
        await EnsureUnitExists(asset.UnitId, cancellationToken);

        if (await _db.Assets.AnyAsync(
                a => a.UnitId == asset.UnitId && a.Serial == asset.Serial,
                cancellationToken))
        {
            throw new InvalidOperationException(
                $"An asset with serial '{asset.Serial}' already exists on this unit.");
        }

        asset.Id = Guid.NewGuid();
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Created asset {AssetId} ({Type}) for unit {UnitId}.",
            asset.Id,
            asset.Type,
            asset.UnitId);
        return asset;
    }

    public async Task<Asset> UpdateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        ValidateAsset(asset);

        var existing = await _db.Assets.FindAsync([asset.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Asset {asset.Id} was not found.");

        if (await _db.Assets.AnyAsync(
                a => a.Id != asset.Id && a.UnitId == asset.UnitId && a.Serial == asset.Serial,
                cancellationToken))
        {
            throw new InvalidOperationException(
                $"An asset with serial '{asset.Serial}' already exists on this unit.");
        }

        existing.Type = asset.Type.Trim();
        existing.Make = asset.Make.Trim();
        existing.Model = asset.Model.Trim();
        existing.Serial = asset.Serial.Trim();
        existing.InstallDate = asset.InstallDate;
        existing.WarrantyStart = asset.WarrantyStart;
        existing.WarrantyEnd = asset.WarrantyEnd;
        existing.Condition = asset.Condition.Trim();
        existing.Notes = asset.Notes;
        existing.PhotoPaths = asset.PhotoPaths;

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated asset {AssetId}.", existing.Id);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await _db.Assets.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Asset {id} was not found.");

        _db.Assets.Remove(asset);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted asset {AssetId}.", id);
    }

    private async Task EnsureUnitExists(Guid unitId, CancellationToken cancellationToken)
    {
        if (!await _db.Units.AnyAsync(u => u.Id == unitId, cancellationToken))
        {
            throw new InvalidOperationException($"Unit {unitId} was not found.");
        }
    }

    private static void ValidateAsset(Asset asset)
    {
        if (asset.UnitId == Guid.Empty)
        {
            throw new ArgumentException("UnitId is required.", nameof(asset));
        }

        if (string.IsNullOrWhiteSpace(asset.Type))
        {
            throw new ArgumentException("Asset type is required.", nameof(asset));
        }

        if (string.IsNullOrWhiteSpace(asset.Serial))
        {
            throw new ArgumentException("Serial number is required.", nameof(asset));
        }

        asset.Type = asset.Type.Trim();
        asset.Make = asset.Make.Trim();
        asset.Model = asset.Model.Trim();
        asset.Serial = asset.Serial.Trim();
        asset.Condition = asset.Condition.Trim();
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
