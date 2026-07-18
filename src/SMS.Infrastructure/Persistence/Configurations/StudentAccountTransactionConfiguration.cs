using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class StudentAccountTransactionConfiguration : IEntityTypeConfiguration<StudentAccountTransaction>
{
    public void Configure(EntityTypeBuilder<StudentAccountTransaction> builder)
    {
        builder.ToTable("StudentAccountTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Notes)
            .HasMaxLength(500);

        builder.HasIndex(t => t.StudentId);

        builder.HasOne(t => t.Student)
            .WithMany()
            .HasForeignKey(t => t.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Invoice)
            .WithMany()
            .HasForeignKey(t => t.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Payment)
            .WithMany()
            .HasForeignKey(t => t.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Soft-delete + tenant isolation are applied centrally in
        // ApplicationDbContext.ApplyTenantFilter for all tenant entities.
    }
}
