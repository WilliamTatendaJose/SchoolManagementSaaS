using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.LeaveType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(l => l.Reason)
            .HasMaxLength(1000);

        builder.Property(l => l.ApproverComments)
            .HasMaxLength(500);

        // Relationship to Staff (requester)
        builder.HasOne(l => l.Staff)
            .WithMany(s => s.LeaveRequests)
            .HasForeignKey(l => l.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship to Staff (approver)
        builder.HasOne(l => l.ApprovedBy)
            .WithMany()
            .HasForeignKey(l => l.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(l => l.NumberOfDays);
    }
}
