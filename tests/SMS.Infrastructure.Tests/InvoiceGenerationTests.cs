using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.Fees.Commands;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// End-to-end checks for bulk invoice generation, including sibling discounts and
/// arrears carry-forward, run against a real (SQLite) ApplicationDbContext.
/// </summary>
public class InvoiceGenerationTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();

    private Guid _currentTermId;
    private Guid _priorTermId;
    private Guid _elderId;
    private Guid _youngerId;
    private Guid _onlyChildId;

    // Two mandatory fees => 600 subtotal per student.
    private const decimal Tuition = 500m;
    private const decimal Levy = 100m;
    private const decimal FeeSubtotal = Tuition + Levy;
    private const decimal ElderArrears = 200m;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Test School", Code = "TEST" };
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
            await db.SaveChangesAsync();

            var priorTerm = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            var currentTerm = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 2", TermNumber = 2, StartDate = new DateTime(2026, 5, 1), EndDate = new DateTime(2026, 8, 31) };
            db.AcademicTerms.AddRange(priorTerm, currentTerm);

            var cls = new Class { Name = "Form 1", Level = 1, Capacity = 40 };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();
            _priorTermId = priorTerm.Id;
            _currentTermId = currentTerm.Id;

            db.FeeStructures.AddRange(
                new FeeStructure { ClassId = cls.Id, AcademicYearId = year.Id, Name = "Tuition", Amount = Tuition, FeeType = "Tuition" },
                new FeeStructure { ClassId = cls.Id, AcademicYearId = year.Id, Name = "Development Levy", Amount = Levy, FeeType = "Levy" });

            // Elder (older) + younger share guardian G1 => siblings; only-child has no guardian.
            var elder = NewStudent("S-ELDER", "Elder", new DateTime(2010, 3, 1));
            var younger = NewStudent("S-YOUNGER", "Younger", new DateTime(2013, 9, 1));
            var onlyChild = NewStudent("S-ONLY", "Only", new DateTime(2011, 6, 1));
            db.Students.AddRange(elder, younger, onlyChild);

            var guardian = new Guardian { FirstName = "Parent", LastName = "One", Gender = Gender.Other };
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();
            _elderId = elder.Id;
            _youngerId = younger.Id;
            _onlyChildId = onlyChild.Id;

            db.StudentGuardians.AddRange(
                new StudentGuardian { StudentId = elder.Id, GuardianId = guardian.Id, Relationship = "Parent" },
                new StudentGuardian { StudentId = younger.Id, GuardianId = guardian.Id, Relationship = "Parent" });

            foreach (var studentId in new[] { elder.Id, younger.Id, onlyChild.Id })
            {
                db.Enrollments.Add(new Enrollment
                {
                    StudentId = studentId,
                    ClassId = cls.Id,
                    AcademicYearId = year.Id,
                    EnrollmentDate = new DateTime(2026, 1, 10),
                    IsActive = true
                });
            }

            // Prior-term unpaid invoice for the elder => 200 arrears to carry forward.
            db.Invoices.Add(new Invoice
            {
                InvoiceNumber = "INV-PRIOR-0001",
                StudentId = elder.Id,
                AcademicTermId = priorTerm.Id,
                InvoiceDate = new DateTime(2026, 1, 15),
                DueDate = new DateTime(2026, 2, 15),
                TotalAmount = ElderArrears,
                DiscountAmount = 0,
                PaidAmount = 0
            });

            await db.SaveChangesAsync();
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Generate_applies_sibling_discount_and_carries_arrears_forward()
    {
        InvoiceGenerationResultDto result;
        await using (var db = _harness.CreateDbContext())
        {
            var handler = new GenerateInvoicesCommandHandler(db);
            var outcome = await handler.Handle(new GenerateInvoicesCommand
            {
                AcademicTermId = _currentTermId,
                DueDate = new DateTime(2026, 6, 1),
                SiblingDiscountPercent = 10m,
                CarryForwardArrears = true
            }, CancellationToken.None);

            outcome.IsSuccess.Should().BeTrue();
            result = outcome.Data!;
        }

        result.InvoicesCreated.Should().Be(3);
        result.TotalBilled.Should().Be(FeeSubtotal * 3);          // gross fees, before discount
        result.TotalDiscount.Should().Be(60m);                    // 10% of 600 for the younger sibling only
        result.TotalArrearsCarriedForward.Should().Be(ElderArrears);

        await using (var db = _harness.CreateDbContext())
        {
            var invoices = await db.Invoices
                .AsNoTracking()
                .Include(i => i.Items)
                .Where(i => i.AcademicTermId == _currentTermId)
                .ToListAsync();

            var elder = invoices.Single(i => i.StudentId == _elderId);
            elder.DiscountAmount.Should().Be(0m, "the eldest sibling pays full fees");
            elder.TotalAmount.Should().Be(FeeSubtotal + ElderArrears);
            elder.Items.Should().Contain(it => it.Description == "Arrears brought forward" && it.Amount == ElderArrears);

            var younger = invoices.Single(i => i.StudentId == _youngerId);
            younger.DiscountAmount.Should().Be(60m);
            younger.TotalAmount.Should().Be(FeeSubtotal);
            younger.Items.Should().NotContain(it => it.Description == "Arrears brought forward");

            var onlyChild = invoices.Single(i => i.StudentId == _onlyChildId);
            onlyChild.DiscountAmount.Should().Be(0m, "an only child has no sibling discount");
            onlyChild.TotalAmount.Should().Be(FeeSubtotal);
        }
    }

    [Fact]
    public async Task Generate_is_idempotent_for_a_term()
    {
        var command = new GenerateInvoicesCommand
        {
            AcademicTermId = _currentTermId,
            DueDate = new DateTime(2026, 6, 1)
        };

        await using (var db = _harness.CreateDbContext())
        {
            var first = await new GenerateInvoicesCommandHandler(db).Handle(command, CancellationToken.None);
            first.Data!.InvoicesCreated.Should().Be(3);
        }

        await using (var db = _harness.CreateDbContext())
        {
            var second = await new GenerateInvoicesCommandHandler(db).Handle(command, CancellationToken.None);
            second.Data!.InvoicesCreated.Should().Be(0);
            second.Data!.StudentsSkipped.Should().Be(3);
        }
    }

    private static Student NewStudent(string number, string first, DateTime dob) => new()
    {
        StudentNumber = number,
        FirstName = first,
        LastName = "Family",
        DateOfBirth = dob,
        Gender = Gender.Other,
        AdmissionDate = new DateTime(2026, 1, 10),
        Status = StudentStatus.Active
    };
}
