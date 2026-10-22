using Microsoft.EntityFrameworkCore;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Infrastructure.Persistence;

public class BookingDbContext : DbContext
{
    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<CrewMember> CrewMembers => Set<CrewMember>();

    public DbSet<UserClient> Clients => Set<UserClient>();

    public BookingDbContext(DbContextOptions<BookingDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}