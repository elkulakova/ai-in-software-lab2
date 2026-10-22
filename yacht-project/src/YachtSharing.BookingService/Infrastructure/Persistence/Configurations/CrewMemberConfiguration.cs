using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Infrastructure.Persistence.Configurations;

public class CrewMemberConfiguration : IEntityTypeConfiguration<CrewMember>
{
    public void Configure(EntityTypeBuilder<CrewMember> builder)
    {
        builder.ToTable("crew_members");

        builder.HasKey(cm => cm.Id);

        builder.Property(cm => cm.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(cm => cm.InsuranceExpiry)
            .IsRequired();

        builder.Property(cm => cm.BookingId).IsRequired();

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(cm => cm.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}