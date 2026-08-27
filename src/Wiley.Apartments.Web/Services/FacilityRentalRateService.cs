using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class FacilityRentalRateService(
    ApartmentsDbContext db,
    ILogger<FacilityRentalRateService> logger) : IFacilityRentalRateService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly ILogger<FacilityRentalRateService> _logger = logger;

    public async Task<IReadOnlyList<FacilityRentalRate>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var q = _db.FacilityRentalRates.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            q = q.Where(r => r.IsActive);
        }

        return await q
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<FacilityRentalRate> UpsertAsync(
        FacilityRentalRate rate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rate.Name))
        {
            throw new ArgumentException("Rate name is required.");
        }

        if (rate.Fee < 0 || rate.Deposit < 0)
        {
            throw new ArgumentException("Fee and deposit cannot be negative.");
        }

        FacilityRentalRate row;
        if (rate.Id == Guid.Empty)
        {
            row = new FacilityRentalRate { Id = Guid.NewGuid() };
            _db.FacilityRentalRates.Add(row);
        }
        else
        {
            row = await _db.FacilityRentalRates.FirstOrDefaultAsync(r => r.Id == rate.Id, cancellationToken)
                  ?? throw new InvalidOperationException($"Rental rate {rate.Id} was not found.");
        }

        row.Space = rate.Space;
        row.Name = rate.Name.Trim();
        if (row.Name.Length > 128)
        {
            row.Name = row.Name[..128];
        }

        row.Fee = decimal.Round(rate.Fee, 2, MidpointRounding.AwayFromZero);
        row.Deposit = decimal.Round(rate.Deposit, 2, MidpointRounding.AwayFromZero);
        row.SortOrder = rate.SortOrder;
        row.IsActive = rate.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Upserted CC rental rate {Id} space={Space} fee={Fee} deposit={Deposit} active={Active}.",
            row.Id, row.Space, row.Fee, row.Deposit, row.IsActive);
        return row;
    }

    internal async Task EnsureDefaultsAsync(CancellationToken cancellationToken)
    {
        if (await _db.FacilityRentalRates.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.FacilityRentalRates.AddRange(
            new FacilityRentalRate
            {
                Id = Guid.Parse("c0c00000-0001-4000-8000-000000000001"),
                Space = FacilitySpace.FireplaceRoom,
                Name = "Fireplace Room",
                Fee = 50m,
                Deposit = 50m,
                SortOrder = 10,
                IsActive = true
            },
            new FacilityRentalRate
            {
                Id = Guid.Parse("c0c00000-0001-4000-8000-000000000002"),
                Space = FacilitySpace.Kitchen,
                Name = "Kitchen",
                Fee = 75m,
                Deposit = 75m,
                SortOrder = 20,
                IsActive = true
            },
            new FacilityRentalRate
            {
                Id = Guid.Parse("c0c00000-0001-4000-8000-000000000003"),
                Space = FacilitySpace.MainHall,
                Name = "Main Space (Hall)",
                Fee = 150m,
                Deposit = 100m,
                SortOrder = 30,
                IsActive = true
            },
            new FacilityRentalRate
            {
                Id = Guid.Parse("c0c00000-0001-4000-8000-000000000004"),
                Space = FacilitySpace.WholeBuilding,
                Name = "Entire Facility (package)",
                Fee = 250m,
                Deposit = 150m,
                SortOrder = 40,
                IsActive = true
            });
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded default Community Center rental rates.");
    }
}
