using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class MaintenanceService : IMaintenanceService
{
    private readonly ApartmentsDbContext _db;
    private readonly IUnitOperatingCostService _opsCosts;
    private readonly IDateTimeService _clock;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(
        ApartmentsDbContext db,
        IUnitOperatingCostService opsCosts,
        IDateTimeService clock,
        ILogger<MaintenanceService> logger)
    {
        _db = db;
        _opsCosts = opsCosts;
        _clock = clock;
        _logger = logger;
    }

    public async Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await BaseQuery()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MaintenanceRequest>> GetAllAsync(
        bool openOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = BaseQuery();
        if (openOnly)
        {
            query = query.Where(m =>
                m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.InProgress);
        }

        return await query
            .OrderByDescending(m => m.Priority)
            .ThenBy(m => m.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceRequest>> GetForUnitAsync(
        Guid unitId,
        CancellationToken cancellationToken = default) =>
        await BaseQuery()
            .Where(m => m.UnitId == unitId)
            .OrderByDescending(m => m.CreatedUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MaintenanceRequest>> GetForAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default) =>
        await BaseQuery()
            .Where(m => m.AssetId == assetId)
            .OrderByDescending(m => m.CreatedUtc)
            .ToListAsync(cancellationToken);

    public async Task<MaintenanceRequest> CreateAsync(
        Guid unitId,
        string description,
        MaintenancePriority priority = MaintenancePriority.Normal,
        Guid? assetId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureUnitAsync(unitId, cancellationToken);
        await EnsureAssetAsync(assetId, unitId, cancellationToken);

        var request = new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            AssetId = assetId,
            Description = RequireDescription(description),
            Priority = priority,
            Status = MaintenanceStatus.Open,
            Notes = Trim(notes, 2000),
            CreatedUtc = _clock.UtcNow,
            IsDeleted = false
        };
        _db.MaintenanceRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created maintenance {Id} for unit {UnitId}.", request.Id, unitId);
        return (await GetByIdAsync(request.Id, cancellationToken))!;
    }

    public async Task<MaintenanceRequest> UpdateAsync(
        Guid id,
        string description,
        MaintenanceStatus status,
        MaintenancePriority priority,
        Guid? assetId = null,
        decimal? cost = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var request = await RequireAsync(id, cancellationToken);
        await EnsureAssetAsync(assetId, request.UnitId, cancellationToken);

        if (status == MaintenanceStatus.Completed && request.Status != MaintenanceStatus.Completed)
        {
            return await CompleteAsync(
                id,
                cost ?? request.Cost,
                notes: notes ?? request.Notes,
                cancellationToken: cancellationToken);
        }

        request.Description = RequireDescription(description);
        request.Status = status;
        request.Priority = priority;
        request.AssetId = assetId;
        request.Cost = NormalizeCost(cost);
        request.Notes = Trim(notes, 2000);
        if (status == MaintenanceStatus.Cancelled)
        {
            request.CompletedUtc ??= _clock.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(request.Id, cancellationToken))!;
    }

    public async Task<MaintenanceRequest> CompleteAsync(
        Guid id,
        decimal? cost = null,
        DateTime? completedUtc = null,
        string? notes = null,
        bool postOperatingCost = true,
        CancellationToken cancellationToken = default)
    {
        var request = await RequireAsync(id, cancellationToken);
        if (request.Status == MaintenanceStatus.Completed)
        {
            return (await GetByIdAsync(request.Id, cancellationToken))!;
        }

        request.Status = MaintenanceStatus.Completed;
        request.CompletedUtc = EnsureUtc(completedUtc ?? _clock.UtcNow);
        request.Cost = NormalizeCost(cost) ?? request.Cost;
        if (notes is not null)
        {
            request.Notes = Trim(notes, 2000);
        }

        if (postOperatingCost
            && request.Cost is > 0
            && request.OperatingCostId is null)
        {
            var ops = await _opsCosts.CreateAsync(
                OperatingCostCategory.Repair,
                request.Cost.Value,
                request.CompletedUtc.Value,
                unitId: request.UnitId,
                notes: $"Maintenance WO {request.Id:N}: {request.Description}",
                maintenanceRequestId: request.Id,
                cancellationToken: cancellationToken);
            request.OperatingCostId = ops.Id;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Completed maintenance {Id}; ops cost {OpsCostId}.",
            request.Id, request.OperatingCostId);
        return (await GetByIdAsync(request.Id, cancellationToken))!;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await RequireAsync(id, cancellationToken);
        request.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<MaintenanceRequest> BaseQuery() =>
        _db.MaintenanceRequests
            .AsNoTracking()
            .Include(m => m.Unit)
            .Include(m => m.Asset)
            .Where(m => !m.IsDeleted);

    private async Task<MaintenanceRequest> RequireAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.MaintenanceRequests
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken)
        ?? throw new InvalidOperationException($"Maintenance request {id} was not found.");

    private async Task EnsureUnitAsync(Guid unitId, CancellationToken cancellationToken)
    {
        if (!await _db.Units.AnyAsync(u => u.Id == unitId, cancellationToken))
        {
            throw new InvalidOperationException($"Unit {unitId} was not found.");
        }
    }

    private async Task EnsureAssetAsync(Guid? assetId, Guid unitId, CancellationToken cancellationToken)
    {
        if (assetId is null)
        {
            return;
        }

        var asset = await _db.Assets.AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken)
                    ?? throw new InvalidOperationException($"Asset {assetId} was not found.");
        if (asset.UnitId != unitId)
        {
            throw new InvalidOperationException("Asset does not belong to the selected unit.");
        }
    }

    private static string RequireDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        var trimmed = description.Trim();
        if (trimmed.Length > 2000)
        {
            throw new ArgumentException("Description cannot exceed 2000 characters.", nameof(description));
        }

        return trimmed;
    }

    private static decimal? NormalizeCost(decimal? cost)
    {
        if (cost is null)
        {
            return null;
        }

        if (cost < 0)
        {
            throw new ArgumentException("Cost cannot be negative.", nameof(cost));
        }

        return decimal.Round(cost.Value, 2, MidpointRounding.AwayFromZero);
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var t = value.Trim();
        return t.Length > max ? t[..max] : t;
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
