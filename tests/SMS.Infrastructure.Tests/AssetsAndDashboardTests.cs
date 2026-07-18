using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.Assets.Commands;
using SMS.Application.Features.Assets.Queries;
using SMS.Application.Features.Dashboard.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

public class AssetsAndDashboardTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Asset School", Code = "AST" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Assets_get_a_generated_number_and_appear_in_the_register()
    {
        await using var db = _harness.CreateDbContext();
        var create = await new CreateAssetCommandHandler(db, _harness.CurrentUser)
            .Handle(new CreateAssetCommand { Name = "Projector", Category = "Electronics", PurchasePrice = 450m }, CancellationToken.None);
        create.IsSuccess.Should().BeTrue();

        var created = await db.Assets.AsNoTracking().FirstAsync(a => a.Id == create.Data);
        created.AssetNumber.Should().StartWith("AST-");

        var list = await new GetAssetsQueryHandler(db).Handle(new GetAssetsQuery(), CancellationToken.None);
        list.Data!.Items.Should().ContainSingle(a => a.Name == "Projector");
    }

    [Fact]
    public async Task Dashboard_reports_students_finance_and_enrollment_by_class()
    {
        await using (var db = _harness.CreateDbContext())
        {
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            var cls = new Class { Name = "Form 1", Level = 1, Capacity = 40 };
            db.AcademicYears.Add(year);
            db.Classes.Add(cls);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);
            var s1 = NewStudent("S1", cls.Id);
            var s2 = NewStudent("S2", cls.Id);
            db.Students.AddRange(s1, s2);
            await db.SaveChangesAsync();

            // Billed 1000, collected 250 => 25% collection rate.
            db.Invoices.Add(new Invoice { InvoiceNumber = "INV-1", StudentId = s1.Id, AcademicTermId = term.Id, InvoiceDate = new DateTime(2026, 1, 15), DueDate = new DateTime(2026, 2, 15), TotalAmount = 600, PaidAmount = 150 });
            db.Invoices.Add(new Invoice { InvoiceNumber = "INV-2", StudentId = s2.Id, AcademicTermId = term.Id, InvoiceDate = new DateTime(2026, 1, 15), DueDate = new DateTime(2026, 2, 15), TotalAmount = 400, PaidAmount = 100 });
            await db.SaveChangesAsync();
        }

        await using var db2 = _harness.CreateDbContext();
        var result = await new GetDashboardQueryHandler(db2).Handle(new GetDashboardQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Data!;
        dto.ActiveStudents.Should().Be(2);
        dto.TotalBilled.Should().Be(1000m);
        dto.TotalCollected.Should().Be(250m);
        dto.TotalOutstanding.Should().Be(750m);
        dto.CollectionRatePercent.Should().Be(25m);
        dto.EnrollmentByClass.Should().ContainSingle(c => c.ClassName == "Form 1" && c.StudentCount == 2);
    }

    private static Student NewStudent(string number, Guid classId) => new()
    {
        StudentNumber = number,
        FirstName = number,
        LastName = "Student",
        DateOfBirth = new DateTime(2012, 1, 1),
        Gender = Gender.Other,
        AdmissionDate = new DateTime(2026, 1, 1),
        Status = StudentStatus.Active,
        CurrentClassId = classId
    };
}
