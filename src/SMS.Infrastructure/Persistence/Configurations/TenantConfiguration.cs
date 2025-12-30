using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(t => t.Code)
            .IsUnique();

        builder.Property(t => t.Email)
            .HasMaxLength(200);

        builder.Property(t => t.Phone)
            .HasMaxLength(50);

        builder.Property(t => t.Address)
            .HasMaxLength(500);

        builder.Property(t => t.City)
            .HasMaxLength(100);

        builder.Property(t => t.Country)
            .HasMaxLength(100);

        builder.Property(t => t.TimeZone)
            .HasMaxLength(100);

        builder.Property(t => t.Currency)
            .HasMaxLength(10);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
