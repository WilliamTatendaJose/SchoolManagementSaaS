using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(b => b.Author)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.Isbn)
            .HasMaxLength(20);

        builder.Property(b => b.Category)
            .HasMaxLength(100);
    }
}

public class BookLoanConfiguration : IEntityTypeConfiguration<BookLoan>
{
    public void Configure(EntityTypeBuilder<BookLoan> builder)
    {
        builder.ToTable("BookLoans");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(l => l.Book)
            .WithMany(b => b.Loans)
            .HasForeignKey(l => l.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Student)
            .WithMany()
            .HasForeignKey(l => l.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
