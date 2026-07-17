using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class PaymentPlanConfiguration : IEntityTypeConfiguration<PaymentPlan>
{
    public void Configure(EntityTypeBuilder<PaymentPlan> builder)
    {
        builder.ToTable("PaymentPlans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(p => p.Invoice)
            .WithMany()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Installments)
            .WithOne(i => i.PaymentPlan)
            .HasForeignKey(i => i.PaymentPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.ToTable("Installments");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Amount)
            .HasPrecision(18, 2);

        builder.HasIndex(i => new { i.PaymentPlanId, i.SequenceNumber })
            .IsUnique();
    }
}
