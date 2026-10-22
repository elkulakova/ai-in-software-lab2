using Microsoft.EntityFrameworkCore;
using YachtSharing.CatalogService.Entities;

namespace YachtSharing.CatalogService.DataTable;

public class CatalogDbContext : DbContext
{
    public DbSet<Yacht> Yachts { get; set; }

    public DbSet<BookingProjection> Bookings { get; set; }

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Yacht>()
        .HasKey(e => e.Id);
        modelBuilder.Entity<Yacht>()
        .Property(e => e.PricePerHour)
        .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<BookingProjection>()
        .HasKey(e => e.Id);
        modelBuilder.Entity<BookingProjection>()
        .Property(e => e.StartTime)
        .HasColumnName("start_time");
        modelBuilder.Entity<BookingProjection>()
        .Property(e => e.EndTime)
        .HasColumnName("end_time");

        modelBuilder.Entity<BookingProjection>()
        .HasIndex(e => new { e.YachtId, e.StartTime, e.EndTime })
        .HasDatabaseName("IX_Bookings_Yacht_Time");
    }
}
