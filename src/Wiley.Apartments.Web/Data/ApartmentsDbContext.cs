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
    }
}
