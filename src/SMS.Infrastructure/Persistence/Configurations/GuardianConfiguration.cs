using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> builder)
    {
        builder.ToTable("Guardians");

        builder.HasKey(g => g.Id);

        // Filtered (not plain) unique index: a soft-deleted Guardian profile must not
        // permanently block re-creating a profile for the same user afterward.
        builder.HasIndex(g => g.UserId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
