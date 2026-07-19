using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.Students.Commands;
using SMS.Domain.Entities;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Covers the bulk student import wizard: two-phase validate-then-commit, class/guardian
/// resolution, duplicate detection (within the file and against the database), and
/// opening-balance invoices - run against a real (SQLite) ApplicationDbContext.
/// </summary>
public class StudentImportTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();

    private Guid _classId;
    private Guid _yearId;
    private Guid _termId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Import School", Code = "IMP" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31), IsCurrent = true };
            db.AcademicYears.Add(year);
            var cls = new Class { Name = "Form 1", Code = "F1", Level = 1, Capacity = 40 };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = year.StartDate, EndDate = new DateTime(2026, 4, 30), IsCurrent = true };
            db.AcademicTerms.Add(term);
            await db.SaveChangesAsync();

            _yearId = year.Id;
            _classId = cls.Id;
            _termId = term.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    private static StudentImportRowDto ValidRow(string first = "Tino", string last = "Moyo", string dob = "2015-03-01") => new()
    {
        FirstName = first,
        LastName = last,
        Gender = "Male",
        DateOfBirth = dob,
        ClassName = "Form 1",
        GuardianFirstName = "Grace",
        GuardianLastName = "Moyo",
        GuardianPhone = "0772000111",
        GuardianRelationship = "Mother",
    };

    [Fact]
    public async Task Validate_only_reports_errors_without_writing_anything()
    {
        await using var db = _harness.CreateDbContext();
        var handler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);

        var outcome = await handler.Handle(new ImportStudentsCommand
        {
            Commit = false,
            Rows = [ValidRow(), new StudentImportRowDto { FirstName = "", LastName = "Nobody", Gender = "Male", DateOfBirth = "2015-01-01" }]
        }, CancellationToken.None);

        outcome.IsSuccess.Should().BeTrue();
        var report = outcome.Data!;
        report.Committed.Should().BeFalse();
        report.TotalRows.Should().Be(2);
        report.ValidRows.Should().Be(1);
        report.ErrorRows.Should().Be(1);
        report.Rows[1].Errors.Should().Contain(e => e.Contains("First name"));

        (await db.Students.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Commit_refuses_when_any_row_has_errors_and_writes_nothing()
    {
        await using var db = _harness.CreateDbContext();
        var handler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);

        var outcome = await handler.Handle(new ImportStudentsCommand
        {
            Commit = true,
            Rows = [ValidRow(), new StudentImportRowDto { FirstName = "", LastName = "Nobody", Gender = "Male", DateOfBirth = "2015-01-01" }]
        }, CancellationToken.None);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Data!.Committed.Should().BeFalse();
        (await db.Students.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Commit_creates_student_guardian_link_and_enrollment()
    {
        Guid studentId;
        await using (var db = _harness.CreateDbContext())
        {
            var handler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);
            var outcome = await handler.Handle(new ImportStudentsCommand { Commit = true, Rows = [ValidRow()] }, CancellationToken.None);

            outcome.IsSuccess.Should().BeTrue();
            var report = outcome.Data!;
            report.Committed.Should().BeTrue();
            report.StudentsCreated.Should().Be(1);
            report.GuardiansCreated.Should().Be(1);
            report.EnrollmentsCreated.Should().Be(1);
        }

        await using var verify = _harness.CreateDbContext();
        var student = await verify.Students.Include(s => s.Guardians).ThenInclude(g => g.Guardian).AsNoTracking().SingleAsync();
        studentId = student.Id;
        student.StudentNumber.Should().StartWith($"STU-{DateTime.UtcNow.Year}-"); // admission date defaults to today when not supplied
        student.CurrentClassId.Should().Be(_classId);
        student.Guardians.Should().ContainSingle();
        student.Guardians.First().Guardian.Phone.Should().Be("0772000111");

        var enrollment = await verify.Enrollments.AsNoTracking().SingleAsync(e => e.StudentId == studentId);
        enrollment.ClassId.Should().Be(_classId);
        enrollment.AcademicYearId.Should().Be(_yearId);
    }

    [Fact]
    public async Task Siblings_sharing_a_guardian_phone_link_to_one_new_guardian_instead_of_duplicating()
    {
        await using var db = _harness.CreateDbContext();
        var handler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);

        var outcome = await handler.Handle(new ImportStudentsCommand
        {
            Commit = true,
            Rows =
            [
                ValidRow("Elder", "Sibling", "2012-01-01"),
                ValidRow("Younger", "Sibling", "2015-01-01"),
            ]
        }, CancellationToken.None);

        outcome.IsSuccess.Should().BeTrue();
        var report = outcome.Data!;
        report.StudentsCreated.Should().Be(2);
        report.GuardiansCreated.Should().Be(1);
        report.GuardiansLinked.Should().Be(1);

        (await db.Guardians.CountAsync()).Should().Be(1);
        (await db.StudentGuardians.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Guardian_phone_matching_an_existing_guardian_links_instead_of_creating_a_duplicate()
    {
        await using (var seed = _harness.CreateDbContext())
        {
            seed.Guardians.Add(new Guardian { FirstName = "Existing", LastName = "Parent", Phone = "0772000111" });
            await seed.SaveChangesAsync();
        }

        await using var db = _harness.CreateDbContext();
        var handler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);
        var outcome = await handler.Handle(new ImportStudentsCommand { Commit = false, Rows = [ValidRow()] }, CancellationToken.None);

        outcome.Data!.Rows[0].GuardianAction.Should().Be("link-existing");
        outcome.Data!.Rows[0].Warnings.Should().Contain(w => w.Contains("Existing Parent"));

        var commitOutcome = await handler.Handle(new ImportStudentsCommand { Commit = true, Rows = [ValidRow()] }, CancellationToken.None);
        commitOutcome.Data!.GuardiansCreated.Should().Be(0);
        commitOutcome.Data!.GuardiansLinked.Should().Be(1);
        (await db.Guardians.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Duplicate_rows_in_the_same_file_are_flagged()
    {
        await using var db = _harness.CreateDbContext();
        var handler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);

        var outcome = await handler.Handle(new ImportStudentsCommand
        {
            Commit = false,
            Rows = [ValidRow(), ValidRow()]
        }, CancellationToken.None);

        var report = outcome.Data!;
        report.ErrorRows.Should().Be(1);
        report.Rows[1].Errors.Should().Contain(e => e.Contains("Duplicate of row 1"));
    }

    [Fact]
    public async Task Row_matching_an_existing_student_by_name_and_dob_is_rejected()
    {
        await using (var seed = _harness.CreateDbContext())
        {
            var handler = new ImportStudentsCommandHandler(seed, _harness.CurrentUser);
            await handler.Handle(new ImportStudentsCommand { Commit = true, Rows = [ValidRow()] }, CancellationToken.None);
        }

        await using var db = _harness.CreateDbContext();
        var reHandler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);
        var outcome = await reHandler.Handle(new ImportStudentsCommand { Commit = false, Rows = [ValidRow()] }, CancellationToken.None);

        outcome.Data!.Rows[0].Errors.Should().Contain(e => e.Contains("already exists"));
    }

    [Fact]
    public async Task Unknown_class_name_is_reported_and_row_gets_no_enrollment()
    {
        await using var db = _harness.CreateDbContext();
        var handler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);

        var row = ValidRow();
        row = row with { ClassName = "Nonexistent Class" };

        var outcome = await handler.Handle(new ImportStudentsCommand { Commit = false, Rows = [row] }, CancellationToken.None);

        outcome.Data!.Rows[0].Errors.Should().Contain(e => e.Contains("Nonexistent Class"));
    }

    [Fact]
    public async Task Row_without_a_guardian_is_valid_but_carries_a_warning()
    {
        await using var db = _harness.CreateDbContext();
        var handler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);

        var row = ValidRow() with { GuardianFirstName = null, GuardianLastName = null, GuardianPhone = null, GuardianEmail = null };
        var outcome = await handler.Handle(new ImportStudentsCommand { Commit = false, Rows = [row] }, CancellationToken.None);

        var result = outcome.Data!.Rows[0];
        result.IsValid.Should().BeTrue();
        result.GuardianAction.Should().BeNull();
        result.Warnings.Should().Contain(w => w.Contains("No guardian"));
    }

    [Fact]
    public async Task Opening_balance_creates_an_invoice_against_the_current_term()
    {
        var row = ValidRow() with { OpeningBalance = "150.50" };

        await using var db = _harness.CreateDbContext();
        var handler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);
        var outcome = await handler.Handle(new ImportStudentsCommand { Commit = true, Rows = [row] }, CancellationToken.None);

        outcome.Data!.OpeningBalanceInvoices.Should().Be(1);

        var invoice = await db.Invoices.AsNoTracking().SingleAsync();
        invoice.AcademicTermId.Should().Be(_termId);
        invoice.TotalAmount.Should().Be(150.50m);
        invoice.Notes.Should().Contain("Opening balance");
    }

    [Fact]
    public async Task Invalid_opening_balance_text_is_rejected()
    {
        var row = ValidRow() with { OpeningBalance = "not-a-number" };

        await using var db = _harness.CreateDbContext();
        var handler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);
        var outcome = await handler.Handle(new ImportStudentsCommand { Commit = false, Rows = [row] }, CancellationToken.None);

        outcome.Data!.Rows[0].Errors.Should().Contain(e => e.Contains("not a valid amount"));
    }

    [Theory]
    [InlineData("2015-03-01")]
    [InlineData("01/03/2015")]
    [InlineData("1/3/2015")]
    public async Task Accepts_iso_and_common_slash_date_formats(string dob)
    {
        var row = ValidRow(dob: dob);

        await using var db = _harness.CreateDbContext();
        var handler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);
        var outcome = await handler.Handle(new ImportStudentsCommand { Commit = false, Rows = [row] }, CancellationToken.None);

        outcome.Data!.Rows[0].IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Future_date_of_birth_is_rejected()
    {
        var row = ValidRow(dob: DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"));

        await using var db = _harness.CreateDbContext();
        var handler = new ImportStudentsCommandHandler(db, _harness.CurrentUser);
        var outcome = await handler.Handle(new ImportStudentsCommand { Commit = false, Rows = [row] }, CancellationToken.None);

        outcome.Data!.Rows[0].Errors.Should().Contain(e => e.Contains("future"));
    }
}
