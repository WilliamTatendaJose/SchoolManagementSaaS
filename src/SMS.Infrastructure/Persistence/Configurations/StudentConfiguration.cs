using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StudentNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(s => new { s.TenantId, s.StudentNumber })
            .IsUnique();

        builder.Property(s => s.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.MiddleName)
            .HasMaxLength(100);

        builder.Property(s => s.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.NationalId)
            .HasMaxLength(50);

        builder.Property(s => s.BirthCertificateNumber)
            .HasMaxLength(50);

        builder.Property(s => s.Address)
            .HasMaxLength(500);

        builder.Property(s => s.City)
            .HasMaxLength(100);

        builder.Property(s => s.Nationality)
            .HasMaxLength(100);

        builder.Property(s => s.Religion)
            .HasMaxLength(100);

        builder.Property(s => s.MedicalNotes)
            .HasMaxLength(1000);

        builder.Property(s => s.SpecialNeeds)
            .HasMaxLength(1000);

        builder.Property(s => s.PreviousSchool)
            .HasMaxLength(200);

        builder.HasOne(s => s.Tenant)
            .WithMany(t => t.Students)
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CurrentClass)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.CurrentClassId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.House)
            .WithMany(h => h.Students)
            .HasForeignKey(s => s.HouseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Dormitory)
            .WithMany(d => d.Students)
            .HasForeignKey(s => s.DormitoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(s => s.DomainEvents);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
