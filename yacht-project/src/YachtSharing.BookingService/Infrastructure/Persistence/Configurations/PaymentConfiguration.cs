using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PaymentDate).IsRequired();

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.TransactionId).IsRequired();
        builder.HasIndex(p => p.TransactionId).IsUnique();

        builder.Property(p => p.BookingId).IsRequired();

        builder.HasOne<Booking>()
            .WithOne()
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}