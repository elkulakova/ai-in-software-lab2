using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.ClientId).IsRequired();
        builder.Property(b => b.YachtId).IsRequired();

        builder.Property(b => b.StartDate).IsRequired();
        builder.Property(b => b.EndDate).IsRequired();

        builder.OwnsOne(b => b.Passport, passport =>
        {
            passport.Property(p => p.Series).IsRequired();
            passport.Property(p => p.Number).IsRequired();
        });

        builder.Property(b => b.IsGimsVerified).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(b => b.FinalCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne<UserClient>()
            .WithMany()
            .HasForeignKey(b => b.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}