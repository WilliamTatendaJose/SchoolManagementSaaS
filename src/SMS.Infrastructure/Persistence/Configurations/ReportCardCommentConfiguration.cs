using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class ReportCardCommentConfiguration : IEntityTypeConfiguration<ReportCardComment>
{
    public void Configure(EntityTypeBuilder<ReportCardComment> builder)
    {
        builder.ToTable("ReportCardComments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClassTeacherComment)
            .HasMaxLength(1000);

        builder.Property(c => c.HeadComment)
            .HasMaxLength(1000);

        builder.HasIndex(c => new { c.StudentId, c.AcademicTermId })
            .IsUnique();

        builder.HasOne(c => c.Student)
            .WithMany()
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.AcademicTerm)
            .WithMany()
            .HasForeignKey(c => c.AcademicTermId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
