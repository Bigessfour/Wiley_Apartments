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
            entity.HasIndex(e => e.Number).IsUnique();
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
    }
}
