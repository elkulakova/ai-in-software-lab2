using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YachtSharing.BookingService.Domain.Entities;

namespace YachtSharing.BookingService.Infrastructure.Persistence.Configurations;

public class UserClientConfiguration : IEntityTypeConfiguration<UserClient>
{
    public void Configure(EntityTypeBuilder<UserClient> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(150)
            .IsRequired();
    }
}