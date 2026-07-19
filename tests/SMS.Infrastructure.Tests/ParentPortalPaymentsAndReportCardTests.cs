using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMS.Application;
using SMS.Application.Features.ParentPortal.Commands;
using SMS.Application.Features.ParentPortal.Queries;
using SMS.Application.Interfaces;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using SMS.Infrastructure.Services.Reports;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// Covers the two parent-facing capabilities added on top of the read-only portal:
/// downloading a child's report card PDF, and paying a child's invoice online via
/// Paynow - both gated by the same guardian-ownership check as the rest of the portal.
/// </summary>
public class ParentPortalPaymentsAndReportCardTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _parentUserId;
    private Guid _myChildId;
    private Guid _otherChildId;
    private Guid _termId;
    private Guid _myInvoiceId;
    private Guid _otherInvoiceId;
    private Guid _myPaymentId;
    private const string MyChildStudentNumber = "S-MINE";

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Portal Pay School", Code = "PPS" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var parentUser = new User { Email = "parent@pps.zw", PasswordHash = "x", FirstName = "Pam", LastName = "Parent" };
            db.Users.Add(parentUser);

            var cls = new Class { Name = "Form 1", Level = 1, Capacity = 40 };
            var subject = new Subject { Name = "Mathematics", Code = "MATH" };
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.Classes.Add(cls);
            db.Subjects.Add(subject);
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);

            var myChild = new Student
            {
                StudentNumber = MyChildStudentNumber, FirstName = "Mine", LastName = "Child",
                DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Other,
                AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active, CurrentClassId = cls.Id
            };
            var otherChild = new Student
            {
                StudentNumber = "S-OTHER", FirstName = "Other", LastName = "Child",
                DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Other,
                AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active
            };
            db.Students.AddRange(myChild, otherChild);

            var guardian = new Guardian { FirstName = "Pam", LastName = "Parent", Gender = Gender.Other, UserId = parentUser.Id };
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();

            db.StudentGuardians.Add(new StudentGuardian { StudentId = myChild.Id, GuardianId = guardian.Id, Relationship = "Parent", IsPrimaryContact = true });

            var assessment = new Assessment
            {
                Name = "End of Term Exam", SubjectId = subject.Id, ClassId = cls.Id, AcademicTermId = term.Id,
                AssessmentType = "Exam", MaxScore = 100, WeightPercentage = 100, IsPublished = true
            };
            db.Assessments.Add(assessment);
            await db.SaveChangesAsync();
            db.Results.Add(new Result { StudentId = myChild.Id, AssessmentId = assessment.Id, Score = 80 });

            var myInvoice = new Invoice
            {
                InvoiceNumber = "INV-MINE", StudentId = myChild.Id, AcademicTermId = term.Id,
                InvoiceDate = new DateTime(2026, 1, 15), DueDate = new DateTime(2026, 2, 15),
                TotalAmount = 500, DiscountAmount = 0, PaidAmount = 0
            };
            var otherInvoice = new Invoice
            {
                InvoiceNumber = "INV-OTHER", StudentId = otherChild.Id, AcademicTermId = term.Id,
                InvoiceDate = new DateTime(2026, 1, 15), DueDate = new DateTime(2026, 2, 15),
                TotalAmount = 300, DiscountAmount = 0, PaidAmount = 0
            };
            db.Invoices.AddRange(myInvoice, otherInvoice);
            await db.SaveChangesAsync();

            var myPayment = new Payment
            {
                ReceiptNumber = "PAY-MINE", InvoiceId = myInvoice.Id, Amount = 500,
                PaymentMethod = PaymentMethod.MobileMoney, Status = PaymentStatus.Pending,
                PaymentDate = DateTime.UtcNow, TransactionReference = "PAY-MINE",
                GatewayPollUrl = "https://www.paynow.co.zw/interface/pollstatus"
            };
            var otherPayment = new Payment
            {
                ReceiptNumber = "PAY-OTHER", InvoiceId = otherInvoice.Id, Amount = 300,
                PaymentMethod = PaymentMethod.MobileMoney, Status = PaymentStatus.Pending,
                PaymentDate = DateTime.UtcNow, TransactionReference = "PAY-OTHER",
                GatewayPollUrl = "https://www.paynow.co.zw/interface/pollstatus"
            };
            db.Payments.AddRange(myPayment, otherPayment);
            await db.SaveChangesAsync();

            _parentUserId = parentUser.Id;
            _myChildId = myChild.Id;
            _otherChildId = otherChild.Id;
            _termId = term.Id;
            _myInvoiceId = myInvoice.Id;
            _otherInvoiceId = otherInvoice.Id;
            _myPaymentId = myPayment.Id;
            _harness.CurrentUser.UserId = parentUser.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    private ServiceProvider BuildProvider(IApplicationDbContext context, IPaymentGatewayService? gateway = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(context);
        services.AddSingleton<IReportCardGenerator, ReportCardPdfGenerator>();
        services.AddSingleton<ICurrentUserService>(_harness.CurrentUser);
        services.AddSingleton(gateway ?? new FakeGatewayService());
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Report_card_downloads_for_an_own_child()
    {
        await using var db = _harness.CreateDbContext();
        await using var provider = BuildProvider(db);
        var sender = provider.GetRequiredService<ISender>();

        var outcome = await sender.Send(new GetMyChildReportCardQuery { StudentId = _myChildId, AcademicTermId = _termId });

        outcome.IsSuccess.Should().BeTrue();
        outcome.Data!.Content.Should().NotBeEmpty();
        Encoding.ASCII.GetString(outcome.Data.Content, 0, 4).Should().Be("%PDF");
        outcome.Data.FileName.Should().Contain(MyChildStudentNumber);
    }

    [Fact]
    public async Task Report_card_for_another_persons_child_is_denied()
    {
        await using var db = _harness.CreateDbContext();
        await using var provider = BuildProvider(db);
        var sender = provider.GetRequiredService<ISender>();

        var outcome = await sender.Send(new GetMyChildReportCardQuery { StudentId = _otherChildId, AcademicTermId = _termId });

        outcome.IsSuccess.Should().BeFalse("a parent must not download another family's child's report card");
    }

    [Fact]
    public async Task Initiating_payment_for_an_own_childs_invoice_succeeds()
    {
        await using var db = _harness.CreateDbContext();
        await using var provider = BuildProvider(db);
        var sender = provider.GetRequiredService<ISender>();

        var outcome = await sender.Send(new InitiateMyChildOnlinePaymentCommand { InvoiceId = _myInvoiceId, Email = "pam@example.com" });

        outcome.IsSuccess.Should().BeTrue();
        outcome.Data!.RedirectUrl.Should().NotBeNullOrEmpty();

        await using var verify = _harness.CreateDbContext();
        (await verify.Payments.CountAsync(p => p.InvoiceId == _myInvoiceId)).Should().Be(2, "the seeded payment plus the newly initiated one");
    }

    [Fact]
    public async Task Initiating_payment_for_another_persons_childs_invoice_is_denied()
    {
        await using var db = _harness.CreateDbContext();
        await using var provider = BuildProvider(db);
        var sender = provider.GetRequiredService<ISender>();

        var outcome = await sender.Send(new InitiateMyChildOnlinePaymentCommand { InvoiceId = _otherInvoiceId });

        outcome.IsSuccess.Should().BeFalse("a parent must not pay another family's invoice");
    }

    [Fact]
    public async Task Checking_status_of_an_own_childs_payment_settles_it()
    {
        var gateway = new FakeGatewayService
        {
            NextStatus = new PaymentStatusResult { IsValid = true, Status = GatewayPaymentStatus.Paid, Reference = "PAY-MINE", Amount = 500m }
        };

        await using var db = _harness.CreateDbContext();
        await using var provider = BuildProvider(db, gateway);
        var sender = provider.GetRequiredService<ISender>();

        var outcome = await sender.Send(new CheckMyChildPaymentStatusCommand(_myPaymentId));

        outcome.IsSuccess.Should().BeTrue();
        outcome.Data!.Settled.Should().BeTrue();
        outcome.Data.InvoiceBalance.Should().Be(0m);
    }

    [Fact]
    public async Task Checking_status_of_another_persons_childs_payment_is_denied()
    {
        var gateway = new FakeGatewayService
        {
            NextStatus = new PaymentStatusResult { IsValid = true, Status = GatewayPaymentStatus.Paid, Reference = "PAY-OTHER", Amount = 300m }
        };

        await using var db = _harness.CreateDbContext();
        await using var provider = BuildProvider(db, gateway);
        var sender = provider.GetRequiredService<ISender>();

        var otherPaymentId = await OtherPaymentIdAsync();
        var outcome = await sender.Send(new CheckMyChildPaymentStatusCommand(otherPaymentId));

        outcome.IsSuccess.Should().BeFalse("a parent must not poll or settle another family's payment");
    }

    private async Task<Guid> OtherPaymentIdAsync()
    {
        await using var db = _harness.CreateDbContext();
        return await db.Payments.Where(p => p.InvoiceId == _otherInvoiceId).Select(p => p.Id).FirstAsync();
    }
}
