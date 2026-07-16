using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber })
            .IsUnique();

        builder.Property(i => i.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.PaidAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.Notes)
            .HasMaxLength(1000);

        builder.HasOne(i => i.Student)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.AcademicTerm)
            .WithMany(t => t.Invoices)
            .HasForeignKey(i => i.AcademicTermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(i => i.Balance);
        builder.Ignore(i => i.IsPaid);
        builder.Ignore(i => i.DomainEvents);

        // Soft-delete + tenant isolation are applied centrally in
        // ApplicationDbContext.ApplyTenantFilter for all tenant entities.
    }
}
