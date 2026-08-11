using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class LateFeeSettingsService : ILateFeeSettingsService
{
    private readonly ApartmentsDbContext _db;
    private readonly ClerkSuiteOptions _defaults;

    public LateFeeSettingsService(ApartmentsDbContext db, IOptions<ClerkSuiteOptions> options)
    {
        _db = db;
        _defaults = options.Value;
    }

    public async Task<LateFeeSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _db.LateFeeSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var seeded = new LateFeeSettings
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Enabled = _defaults.LateFeesEnabled,
            Amount = _defaults.LateFeeAmount,
            GraceDays = _defaults.LateFeeGraceDays
        };
        _db.LateFeeSettings.Add(seeded);
        await _db.SaveChangesAsync(cancellationToken);
        return seeded;
    }

    public async Task<LateFeeSettings> UpdateAsync(
        bool enabled,
        decimal amount,
        int graceDays,
        CancellationToken cancellationToken = default)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Late fee amount cannot be negative.", nameof(amount));
        }

        if (graceDays < 0)
        {
            throw new ArgumentException("Grace days cannot be negative.", nameof(graceDays));
        }

        var row = await _db.LateFeeSettings.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new LateFeeSettings { Id = Guid.Parse("11111111-1111-1111-1111-111111111111") };
            _db.LateFeeSettings.Add(row);
        }

        row.Enabled = enabled;
        row.Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        row.GraceDays = graceDays;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetAsync(cancellationToken);
    }
}
