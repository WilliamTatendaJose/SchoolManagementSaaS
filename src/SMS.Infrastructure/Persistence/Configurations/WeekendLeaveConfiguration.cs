using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class WeekendLeaveConfiguration : IEntityTypeConfiguration<WeekendLeave>
{
    public void Configure(EntityTypeBuilder<WeekendLeave> builder)
    {
        builder.ToTable("WeekendLeaves");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Destination)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.Reason)
            .HasMaxLength(500);

        builder.Property(l => l.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.ReviewNote)
            .HasMaxLength(500);

        builder.Property(l => l.CollectedBy)
            .HasMaxLength(200);

        builder.HasIndex(l => new { l.StudentId, l.DepartureDate });

        builder.HasOne(l => l.Student)
            .WithMany()
            .HasForeignKey(l => l.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.AuthorizedBy)
            .WithMany()
            .HasForeignKey(l => l.AuthorizedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
