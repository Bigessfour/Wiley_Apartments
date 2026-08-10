using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Web.Data;

public class ApartmentsDbContext : IdentityDbContext<ApplicationUser>
{
    public ApartmentsDbContext(DbContextOptions<ApartmentsDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Flooring> Floorings => Set<Flooring>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Occupancy> Occupancies => Set<Occupancy>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ScheduledItem> ScheduledItems => Set<ScheduledItem>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<LateFeeSettings> LateFeeSettings => Set<LateFeeSettings>();
    public DbSet<UnitOperatingCost> UnitOperatingCosts => Set<UnitOperatingCost>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(256);
            entity.Property(e => e.EntityType).HasMaxLength(128);
            entity.Property(e => e.EntityId).HasMaxLength(64);
            entity.Property(e => e.Action).HasMaxLength(32);
            entity.HasIndex(e => new { e.EntityType, e.EntityId, e.TimestampUtc });
        });

        builder.Entity<Unit>(entity =>
        {
            entity.ToTable("Units");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Number).HasMaxLength(16).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.IsFacility).HasDefaultValue(false);
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasIndex(e => e.Number).IsUnique();
            entity.HasIndex(e => e.IsFacility);
        });

        builder.Entity<Asset>(entity =>
        {
            entity.ToTable("Assets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Make).HasMaxLength(128);
            entity.Property(e => e.Model).HasMaxLength(128);
            entity.Property(e => e.Serial).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Condition).HasMaxLength(64);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.PhotoPaths).HasMaxLength(4000);
            entity.HasIndex(e => new { e.UnitId, e.Serial }).IsUnique();
            entity.HasIndex(e => e.Serial);
            entity.HasOne(e => e.Unit)
                .WithMany()
                .HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Flooring>(entity =>
        {
            entity.ToTable("Floorings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Condition).HasMaxLength(64);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => e.UnitId);
            entity.HasOne(e => e.Unit)
                .WithMany()
                .HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(64);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.EmergencyContact).HasMaxLength(512);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasIndex(e => new { e.LastName, e.IsDeleted });
        });

        builder.Entity<HouseholdMember>(entity =>
        {
            entity.ToTable("HouseholdMembers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Relationship).HasMaxLength(128);
            entity.HasIndex(e => e.TenantId);
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.HouseholdMembers)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("Vehicles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Make).HasMaxLength(128);
            entity.Property(e => e.Model).HasMaxLength(128);
            entity.Property(e => e.Color).HasMaxLength(64);
            entity.Property(e => e.Plate).HasMaxLength(32).IsRequired();
            entity.HasIndex(e => e.TenantId);
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Vehicles)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Pet>(entity =>
        {
            entity.ToTable("Pets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(64);
            entity.Property(e => e.Breed).HasMaxLength(128);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => e.TenantId);
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Pets)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Occupancy>(entity =>
        {
            entity.ToTable("Occupancies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StartUtc).IsRequired();
            entity.HasIndex(e => new { e.UnitId, e.EndUtc });
            entity.HasIndex(e => new { e.TenantId, e.EndUtc });
            entity.HasOne(e => e.Unit)
                .WithMany()
                .HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Lease>(entity =>
        {
            entity.ToTable("Leases");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Rent).HasPrecision(18, 2);
            entity.Property(e => e.Deposit).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.TemplateUsed).HasMaxLength(256).IsRequired();
            entity.Property(e => e.GeneratedDocxRelativePath).HasMaxLength(512);
            entity.Property(e => e.GeneratedPdfRelativePath).HasMaxLength(512);
            entity.Property(e => e.CustomClauses).HasMaxLength(4000);
            entity.Property(e => e.LifecycleNote).HasMaxLength(2000);
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasIndex(e => new { e.UnitId, e.IsDeleted });
            entity.HasIndex(e => new { e.TenantId, e.IsDeleted });
            entity.HasIndex(e => e.SignedDocumentId);
            entity.HasIndex(e => new { e.Status, e.EndUtc });
            entity.HasIndex(e => e.PriorLeaseId);
            entity.HasOne(e => e.Unit)
                .WithMany()
                .HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.FilePathOnNas).HasMaxLength(512).IsRequired();
            entity.Property(e => e.OriginalFileName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(128);
            entity.Property(e => e.UploadedBy).HasMaxLength(256);
            entity.HasIndex(e => new { e.EntityType, e.EntityId, e.IsDeleted });
            entity.HasIndex(e => e.Category);
        });

        builder.Entity<ScheduledItem>(entity =>
        {
            entity.ToTable("ScheduledItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => new { e.UnitId, e.IsDeleted });
            entity.HasIndex(e => new { e.Category, e.StartUtc });
            entity.HasIndex(e => new { e.StartUtc, e.EndUtc });
            entity.HasIndex(e => e.IsCompleted);
            entity.HasOne(e => e.Unit)
                .WithMany()
                .HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Lease)
                .WithMany()
                .HasForeignKey(e => e.LeaseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<LedgerEntry>(entity =>
        {
            entity.ToTable("LedgerEntries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntryType).HasConversion<string>().HasMaxLength(16);
            entity.Property(e => e.Method).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => new { e.TenantId, e.IsDeleted, e.DateUtc });
            entity.HasIndex(e => new { e.UnitId, e.IsDeleted, e.DateUtc });
            entity.HasIndex(e => e.LeaseId);
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Unit)
                .WithMany()
                .HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Lease)
                .WithMany()
                .HasForeignKey(e => e.LeaseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<LateFeeSettings>(entity =>
        {
            entity.ToTable("LateFeeSettings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });

        builder.Entity<UnitOperatingCost>(entity =>
        {
            entity.ToTable("UnitOperatingCosts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Vendor).HasMaxLength(256);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => new { e.UnitId, e.IsDeleted, e.IncurredUtc });
            entity.HasIndex(e => new { e.Category, e.IncurredUtc });
            entity.HasOne(e => e.Unit)
                .WithMany()
                .HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<MaintenanceRequest>(entity =>
        {
            entity.ToTable("MaintenanceRequests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.Cost).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => new { e.UnitId, e.IsDeleted, e.Status });
            entity.HasIndex(e => new { e.AssetId, e.IsDeleted });
            entity.HasIndex(e => e.OperatingCostId);
            entity.HasOne(e => e.Unit)
                .WithMany()
                .HasForeignKey(e => e.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Asset)
                .WithMany()
                .HasForeignKey(e => e.AssetId)
                .OnDelete(DeleteBehavior.SetNull);
        });


        builder.Entity<AppSetting>(entity =>
        {
            entity.ToTable("AppSettings");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(128);
            entity.Property(e => e.Value).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
        });
    }
}
