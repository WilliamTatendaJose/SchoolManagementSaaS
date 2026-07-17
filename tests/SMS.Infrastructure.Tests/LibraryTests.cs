using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.Library;
using SMS.Application.Features.Library.Commands;
using SMS.Application.Features.Library.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.Infrastructure.Tests;

public class LibraryTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _studentAId;
    private Guid _studentBId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Library School", Code = "LIB" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var a = new Student { StudentNumber = "S-LIB-1", FirstName = "Anna", LastName = "Reader", DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Female, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active };
            var b = new Student { StudentNumber = "S-LIB-2", FirstName = "Ben", LastName = "Reader", DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Male, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active };
            db.Students.Add(a);
            db.Students.Add(b);
            await db.SaveChangesAsync();
            _studentAId = a.Id;
            _studentBId = b.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    private async Task<Guid> SeedBookAsync(int totalCopies)
    {
        await using var db = _harness.CreateDbContext();
        var result = await new CreateBookCommandHandler(db).Handle(new CreateBookCommand
        {
            Title = "Things Fall Apart",
            Author = "Chinua Achebe",
            Isbn = "978-0-435-90525-0",
            Category = "Fiction",
            TotalCopies = totalCopies
        }, CancellationToken.None);
        return result.Data;
    }

    [Fact]
    public async Task Borrowing_a_book_defaults_a_fourteen_day_due_date()
    {
        var bookId = await SeedBookAsync(totalCopies: 2);

        await using var db = _harness.CreateDbContext();
        var borrowed = new DateTime(2026, 3, 1);
        var result = await new BorrowBookCommandHandler(db).Handle(new BorrowBookCommand
        {
            BookId = bookId,
            StudentId = _studentAId,
            BorrowedDate = borrowed
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _harness.CreateDbContext();
        var loan = await verify.BookLoans.AsNoTracking().FirstAsync(l => l.Id == result.Data);
        loan.DueDate.Should().Be(borrowed.AddDays(14));
        loan.Status.Should().Be(BookLoanStatuses.Borrowed);
    }

    [Fact]
    public async Task Cannot_borrow_more_copies_than_the_catalog_holds()
    {
        var bookId = await SeedBookAsync(totalCopies: 1);

        await using (var db = _harness.CreateDbContext())
        {
            var first = await new BorrowBookCommandHandler(db).Handle(new BorrowBookCommand
            {
                BookId = bookId,
                StudentId = _studentAId
            }, CancellationToken.None);
            first.IsSuccess.Should().BeTrue();
        }

        await using var db2 = _harness.CreateDbContext();
        var second = await new BorrowBookCommandHandler(db2).Handle(new BorrowBookCommand
        {
            BookId = bookId,
            StudentId = _studentBId
        }, CancellationToken.None);

        second.IsSuccess.Should().BeFalse();
        second.Error.Should().Contain("available");
    }

    [Fact]
    public async Task A_student_cannot_borrow_two_copies_of_the_same_book_at_once()
    {
        var bookId = await SeedBookAsync(totalCopies: 5);

        await using (var db = _harness.CreateDbContext())
        {
            await new BorrowBookCommandHandler(db).Handle(new BorrowBookCommand
            {
                BookId = bookId,
                StudentId = _studentAId
            }, CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var again = await new BorrowBookCommandHandler(db2).Handle(new BorrowBookCommand
        {
            BookId = bookId,
            StudentId = _studentAId
        }, CancellationToken.None);

        again.IsSuccess.Should().BeFalse();
        again.Error.Should().Contain("already");
    }

    [Fact]
    public async Task Returning_a_book_frees_a_copy_for_another_student()
    {
        var bookId = await SeedBookAsync(totalCopies: 1);

        Guid loanId;
        await using (var db = _harness.CreateDbContext())
        {
            var first = await new BorrowBookCommandHandler(db).Handle(new BorrowBookCommand
            {
                BookId = bookId,
                StudentId = _studentAId
            }, CancellationToken.None);
            loanId = first.Data;
        }

        await using (var db = _harness.CreateDbContext())
        {
            var returned = await new ReturnBookCommandHandler(db).Handle(new ReturnBookCommand { LoanId = loanId }, CancellationToken.None);
            returned.IsSuccess.Should().BeTrue();
        }

        await using var db2 = _harness.CreateDbContext();
        var second = await new BorrowBookCommandHandler(db2).Handle(new BorrowBookCommand
        {
            BookId = bookId,
            StudentId = _studentBId
        }, CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Marking_a_loan_lost_permanently_reduces_the_catalog_total()
    {
        var bookId = await SeedBookAsync(totalCopies: 1);

        Guid loanId;
        await using (var db = _harness.CreateDbContext())
        {
            var first = await new BorrowBookCommandHandler(db).Handle(new BorrowBookCommand
            {
                BookId = bookId,
                StudentId = _studentAId
            }, CancellationToken.None);
            loanId = first.Data;
        }

        await using (var db = _harness.CreateDbContext())
        {
            await new ReturnBookCommandHandler(db).Handle(new ReturnBookCommand { LoanId = loanId, Lost = true }, CancellationToken.None);
        }

        await using (var db = _harness.CreateDbContext())
        {
            var book = await db.Books.AsNoTracking().FirstAsync(b => b.Id == bookId);
            book.TotalCopies.Should().Be(0);

            var loan = await db.BookLoans.AsNoTracking().FirstAsync(l => l.Id == loanId);
            loan.Status.Should().Be(BookLoanStatuses.Lost);
        }

        // The lost copy no longer counts as available, unlike a normal return.
        await using var db2 = _harness.CreateDbContext();
        var another = await new BorrowBookCommandHandler(db2).Handle(new BorrowBookCommand
        {
            BookId = bookId,
            StudentId = _studentBId
        }, CancellationToken.None);

        another.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Overdue_loans_are_listed_and_a_returned_loan_drops_off()
    {
        var bookId = await SeedBookAsync(totalCopies: 2);

        Guid overdueLoanId;
        await using (var db = _harness.CreateDbContext())
        {
            var loan = await new BorrowBookCommandHandler(db).Handle(new BorrowBookCommand
            {
                BookId = bookId,
                StudentId = _studentAId,
                BorrowedDate = DateTime.UtcNow.AddDays(-30),
                DueDate = DateTime.UtcNow.AddDays(-16)
            }, CancellationToken.None);
            overdueLoanId = loan.Data;
        }

        await using (var db = _harness.CreateDbContext())
        {
            var overdue = await new GetOverdueLoansQueryHandler(db).Handle(new GetOverdueLoansQuery(), CancellationToken.None);
            overdue.IsSuccess.Should().BeTrue();
            overdue.Data.Should().ContainSingle(l => l.Id == overdueLoanId);
        }

        await using (var db = _harness.CreateDbContext())
        {
            await new ReturnBookCommandHandler(db).Handle(new ReturnBookCommand { LoanId = overdueLoanId }, CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var afterReturn = await new GetOverdueLoansQueryHandler(db2).Handle(new GetOverdueLoansQuery(), CancellationToken.None);
        afterReturn.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Books_can_be_searched_by_title_or_author()
    {
        await SeedBookAsync(totalCopies: 3);

        await using var db = _harness.CreateDbContext();
        var byTitle = await new GetBooksQueryHandler(db).Handle(new GetBooksQuery { Search = "fall apart" }, CancellationToken.None);
        byTitle.Data.Should().ContainSingle();

        var byAuthor = await new GetBooksQueryHandler(db).Handle(new GetBooksQuery { Search = "achebe" }, CancellationToken.None);
        byAuthor.Data.Should().ContainSingle();

        var noMatch = await new GetBooksQueryHandler(db).Handle(new GetBooksQuery { Search = "nonexistent" }, CancellationToken.None);
        noMatch.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Student_loan_history_lists_most_recent_first()
    {
        var bookId = await SeedBookAsync(totalCopies: 3);

        Guid firstLoanId, secondLoanId;
        await using (var db = _harness.CreateDbContext())
        {
            var first = await new BorrowBookCommandHandler(db).Handle(new BorrowBookCommand
            {
                BookId = bookId,
                StudentId = _studentAId,
                BorrowedDate = new DateTime(2026, 1, 1)
            }, CancellationToken.None);
            firstLoanId = first.Data;
            await new ReturnBookCommandHandler(db).Handle(new ReturnBookCommand { LoanId = firstLoanId, ReturnedDate = new DateTime(2026, 1, 10) }, CancellationToken.None);
        }

        await using (var db = _harness.CreateDbContext())
        {
            var second = await new BorrowBookCommandHandler(db).Handle(new BorrowBookCommand
            {
                BookId = bookId,
                StudentId = _studentAId,
                BorrowedDate = new DateTime(2026, 2, 1)
            }, CancellationToken.None);
            secondLoanId = second.Data;
        }

        await using var db2 = _harness.CreateDbContext();
        var history = await new GetStudentLoansQueryHandler(db2).Handle(new GetStudentLoansQuery(_studentAId), CancellationToken.None);

        history.IsSuccess.Should().BeTrue();
        history.Data.Should().HaveCount(2);
        history.Data![0].Id.Should().Be(secondLoanId);
        history.Data[1].Id.Should().Be(firstLoanId);
    }
}
