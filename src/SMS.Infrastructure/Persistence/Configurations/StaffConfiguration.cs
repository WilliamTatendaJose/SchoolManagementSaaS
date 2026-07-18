using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("Staff");

        builder.HasKey(s => s.Id);

        // Filtered (not plain) unique index: a soft-deleted Staff profile must not
        // permanently block re-creating a profile for the same user afterward.
        builder.HasIndex(s => s.UserId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
