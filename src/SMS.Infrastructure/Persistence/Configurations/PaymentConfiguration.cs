using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ReceiptNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(p => new { p.TenantId, p.ReceiptNumber })
            .IsUnique();

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.ExchangeRate)
            .HasPrecision(18, 6);

        builder.Ignore(p => p.AmountInInvoiceCurrency);

        builder.Property(p => p.TransactionReference)
            .HasMaxLength(100);

        builder.Property(p => p.MobileMoneyNumber)
            .HasMaxLength(50);

        builder.Property(p => p.BankName)
            .HasMaxLength(100);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.Property(p => p.GatewayPollUrl)
            .HasMaxLength(500);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ReceivedBy)
            .WithMany()
            .HasForeignKey(p => p.ReceivedById)
            .OnDelete(DeleteBehavior.SetNull);

        // Soft-delete + tenant isolation are applied centrally in
        // ApplicationDbContext.ApplyTenantFilter for all tenant entities.
    }
}
