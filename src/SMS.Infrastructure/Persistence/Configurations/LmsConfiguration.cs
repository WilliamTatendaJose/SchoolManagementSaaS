using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasMaxLength(2000);

        builder.Property(a => a.AttachmentKey)
            .HasMaxLength(500);

        builder.Property(a => a.AttachmentFileName)
            .HasMaxLength(255);

        builder.HasOne(a => a.Class)
            .WithMany()
            .HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Subject)
            .WithMany()
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.AcademicTerm)
            .WithMany()
            .HasForeignKey(a => a.AcademicTermId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
    {
        builder.ToTable("AssignmentSubmissions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Comment)
            .HasMaxLength(2000);

        builder.Property(s => s.AttachmentKey)
            .HasMaxLength(500);

        builder.Property(s => s.AttachmentFileName)
            .HasMaxLength(255);

        builder.Property(s => s.Feedback)
            .HasMaxLength(2000);

        builder.Property(s => s.Grade)
            .HasPrecision(5, 2);

        builder.Property(s => s.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => new { s.AssignmentId, s.StudentId })
            .IsUnique();

        builder.HasOne(s => s.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Student)
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
