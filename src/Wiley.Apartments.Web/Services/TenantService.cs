using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class TenantService : ITenantService
{
    private readonly ApartmentsDbContext _db;
    private readonly ILogger<TenantService> _logger;

    public TenantService(ApartmentsDbContext db, ILogger<TenantService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Tenant>> SearchAsync(
        string? query = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Tenants.AsNoTracking().AsQueryable();
        if (!includeDeleted)
        {
            q = q.Where(t => !t.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            q = q.Where(t =>
                t.LastName.ToLower().Contains(term)
                || t.FirstName.ToLower().Contains(term)
                || t.Email.ToLower().Contains(term)
                || t.Phone.ToLower().Contains(term));
        }

        return await q
            .OrderBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Tenants
            .Include(t => t.HouseholdMembers)
            .Include(t => t.Vehicles)
            .Include(t => t.Pets)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenant);
        tenant.Id = Guid.NewGuid();
        tenant.IsDeleted = false;
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Created tenant {LastName}, {FirstName} ({TenantId}).",
            tenant.LastName,
            tenant.FirstName,
            tenant.Id);
        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenant);

        var existing = await _db.Tenants.FindAsync([tenant.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Tenant {tenant.Id} was not found.");

        if (existing.IsDeleted)
        {
            throw new InvalidOperationException("Cannot update a soft-deleted tenant.");
        }

        existing.FirstName = tenant.FirstName.Trim();
        existing.LastName = tenant.LastName.Trim();
        existing.Phone = tenant.Phone.Trim();
        existing.Email = tenant.Email.Trim();
        existing.EmergencyContact = tenant.EmergencyContact.Trim();
        existing.Notes = tenant.Notes;

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Updated tenant {LastName}, {FirstName} ({TenantId}).",
            existing.LastName,
            existing.FirstName,
            existing.Id);
        return existing;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Tenant {id} was not found.");

        if (tenant.IsDeleted)
        {
            return;
        }

        tenant.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Soft-deleted tenant {TenantId}.", id);
    }

    public async Task<HouseholdMember> AddHouseholdMemberAsync(
        Guid tenantId,
        HouseholdMember member,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveTenantAsync(tenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(member.FullName))
        {
            throw new ArgumentException("Household member name is required.", nameof(member));
        }

        member.Id = Guid.NewGuid();
        member.TenantId = tenantId;
        member.FullName = member.FullName.Trim();
        member.Relationship = member.Relationship?.Trim() ?? string.Empty;
        _db.HouseholdMembers.Add(member);
        await _db.SaveChangesAsync(cancellationToken);
        return member;
    }

    public async Task<HouseholdMember> UpdateHouseholdMemberAsync(
        HouseholdMember member,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(member.FullName))
        {
            throw new ArgumentException("Household member name is required.", nameof(member));
        }

        var existing = await _db.HouseholdMembers.FindAsync([member.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Household member {member.Id} was not found.");
        await EnsureActiveTenantAsync(existing.TenantId, cancellationToken);

        existing.FullName = member.FullName.Trim();
        existing.Relationship = member.Relationship?.Trim() ?? string.Empty;
        existing.DateOfBirth = member.DateOfBirth;
        await _db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task RemoveHouseholdMemberAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var member = await _db.HouseholdMembers.FindAsync([memberId], cancellationToken)
            ?? throw new InvalidOperationException($"Household member {memberId} was not found.");
        _db.HouseholdMembers.Remove(member);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Vehicle> AddVehicleAsync(
        Guid tenantId,
        Vehicle vehicle,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveTenantAsync(tenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(vehicle.Plate))
        {
            throw new ArgumentException("Vehicle plate is required.", nameof(vehicle));
        }

        vehicle.Id = Guid.NewGuid();
        vehicle.TenantId = tenantId;
        vehicle.Make = vehicle.Make?.Trim() ?? string.Empty;
        vehicle.Model = vehicle.Model?.Trim() ?? string.Empty;
        vehicle.Color = vehicle.Color?.Trim() ?? string.Empty;
        vehicle.Plate = vehicle.Plate.Trim();
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync(cancellationToken);
        return vehicle;
    }

    public async Task<Vehicle> UpdateVehicleAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vehicle.Plate))
        {
            throw new ArgumentException("Vehicle plate is required.", nameof(vehicle));
        }

        var existing = await _db.Vehicles.FindAsync([vehicle.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Vehicle {vehicle.Id} was not found.");
        await EnsureActiveTenantAsync(existing.TenantId, cancellationToken);

        existing.Make = vehicle.Make?.Trim() ?? string.Empty;
        existing.Model = vehicle.Model?.Trim() ?? string.Empty;
        existing.Color = vehicle.Color?.Trim() ?? string.Empty;
        existing.Plate = vehicle.Plate.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task RemoveVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _db.Vehicles.FindAsync([vehicleId], cancellationToken)
            ?? throw new InvalidOperationException($"Vehicle {vehicleId} was not found.");
        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Pet> AddPetAsync(
        Guid tenantId,
        Pet pet,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveTenantAsync(tenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(pet.Name))
        {
            throw new ArgumentException("Pet name is required.", nameof(pet));
        }

        pet.Id = Guid.NewGuid();
        pet.TenantId = tenantId;
        pet.Name = pet.Name.Trim();
        pet.Type = pet.Type?.Trim() ?? string.Empty;
        pet.Breed = string.IsNullOrWhiteSpace(pet.Breed) ? null : pet.Breed.Trim();
        _db.Pets.Add(pet);
        await _db.SaveChangesAsync(cancellationToken);
        return pet;
    }

    public async Task<Pet> UpdatePetAsync(
        Pet pet,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pet.Name))
        {
            throw new ArgumentException("Pet name is required.", nameof(pet));
        }

        var existing = await _db.Pets.FindAsync([pet.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Pet {pet.Id} was not found.");
        await EnsureActiveTenantAsync(existing.TenantId, cancellationToken);

        existing.Name = pet.Name.Trim();
        existing.Type = pet.Type?.Trim() ?? string.Empty;
        existing.Breed = string.IsNullOrWhiteSpace(pet.Breed) ? null : pet.Breed.Trim();
        existing.Notes = pet.Notes;
        await _db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task RemovePetAsync(Guid petId, CancellationToken cancellationToken = default)
    {
        var pet = await _db.Pets.FindAsync([petId], cancellationToken)
            ?? throw new InvalidOperationException($"Pet {petId} was not found.");
        _db.Pets.Remove(pet);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureActiveTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant {tenantId} was not found.");

        if (tenant.IsDeleted)
        {
            throw new InvalidOperationException("Cannot modify a soft-deleted tenant.");
        }
    }

    private static void ValidateTenant(Tenant tenant)
    {
        if (string.IsNullOrWhiteSpace(tenant.FirstName))
        {
            throw new ArgumentException("First name is required.", nameof(tenant));
        }

        if (string.IsNullOrWhiteSpace(tenant.LastName))
        {
            throw new ArgumentException("Last name is required.", nameof(tenant));
        }

        tenant.FirstName = tenant.FirstName.Trim();
        tenant.LastName = tenant.LastName.Trim();
        tenant.Phone = tenant.Phone?.Trim() ?? string.Empty;
        tenant.Email = tenant.Email?.Trim() ?? string.Empty;
        tenant.EmergencyContact = tenant.EmergencyContact?.Trim() ?? string.Empty;
    }
}
