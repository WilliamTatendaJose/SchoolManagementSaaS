using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.ParentPortal.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

public class ParentPortalTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _parentUserId;
    private Guid _myChildId;
    private Guid _otherChildId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Portal School", Code = "PRT" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var parentUser = new User { Email = "parent@prt.zw", PasswordHash = "x", FirstName = "Pam", LastName = "Parent" };
            db.Users.Add(parentUser);
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);

            var myChild = NewStudent("S-MINE", "Mine");
            var otherChild = NewStudent("S-OTHER", "Other");
            db.Students.AddRange(myChild, otherChild);

            // The parent's guardian record is linked to their user account and to their child only.
            var guardian = new Guardian { FirstName = "Pam", LastName = "Parent", Gender = Gender.Other, UserId = parentUser.Id };
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();

            db.StudentGuardians.Add(new StudentGuardian { StudentId = myChild.Id, GuardianId = guardian.Id, Relationship = "Parent", IsPrimaryContact = true });
            db.Invoices.Add(new Invoice { InvoiceNumber = "INV-MINE", StudentId = myChild.Id, AcademicTermId = term.Id, InvoiceDate = new DateTime(2026, 1, 15), DueDate = new DateTime(2026, 2, 15), TotalAmount = 500, DiscountAmount = 0, PaidAmount = 100 });
            db.Invoices.Add(new Invoice { InvoiceNumber = "INV-OTHER", StudentId = otherChild.Id, AcademicTermId = term.Id, InvoiceDate = new DateTime(2026, 1, 15), DueDate = new DateTime(2026, 2, 15), TotalAmount = 300, DiscountAmount = 0, PaidAmount = 0 });
            await db.SaveChangesAsync();

            _parentUserId = parentUser.Id;
            _myChildId = myChild.Id;
            _otherChildId = otherChild.Id;
            _harness.CurrentUser.UserId = parentUser.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetMyChildren_returns_only_the_parents_own_children_with_balances()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new GetMyChildrenQueryHandler(db, _harness.CurrentUser)
            .Handle(new GetMyChildrenQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().ContainSingle();
        result.Data[0].StudentId.Should().Be(_myChildId);
        result.Data[0].OutstandingBalance.Should().Be(400m); // 500 - 100
    }

    [Fact]
    public async Task Finance_is_available_for_an_own_child()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new GetMyChildFinanceQueryHandler(db, _harness.CurrentUser)
            .Handle(new GetMyChildFinanceQuery(_myChildId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.OutstandingBalance.Should().Be(400m);
        result.Data.Invoices.Should().ContainSingle(i => i.Payable);
    }

    [Fact]
    public async Task Finance_for_another_persons_child_is_denied()
    {
        await using var db = _harness.CreateDbContext();
        var result = await new GetMyChildFinanceQueryHandler(db, _harness.CurrentUser)
            .Handle(new GetMyChildFinanceQuery(_otherChildId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse("a parent must not reach another family's child");
    }

    [Fact]
    public async Task A_user_who_is_not_a_guardian_has_no_children()
    {
        _harness.CurrentUser.UserId = Guid.NewGuid(); // some non-guardian user

        await using var db = _harness.CreateDbContext();
        var result = await new GetMyChildrenQueryHandler(db, _harness.CurrentUser)
            .Handle(new GetMyChildrenQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().BeEmpty();
    }

    private static Student NewStudent(string number, string first) => new()
    {
        StudentNumber = number,
        FirstName = first,
        LastName = "Child",
        DateOfBirth = new DateTime(2012, 1, 1),
        Gender = Gender.Other,
        AdmissionDate = new DateTime(2026, 1, 1),
        Status = StudentStatus.Active
    };
}
