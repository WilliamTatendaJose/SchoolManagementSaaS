using FluentAssertions;
using MediatR;
using SMS.Application.Common.Models;
using SMS.Application.Features.Lms.Commands;
using SMS.Application.Features.Lms.Queries;
using SMS.Application.Features.ParentPortal.Commands;
using SMS.Application.Features.ParentPortal.Queries;
using SMS.Application.Features.Students.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;
using AcademicTermEntity = SMS.Domain.Entities.AcademicTerm;

namespace SMS.Infrastructure.Tests;

/// <summary>
/// A stand-in for MediatR's ISender that records whether the portal handler delegated to
/// the underlying staff query/command (i.e. the ownership gate let it through) and returns
/// a canned result. The delegated handlers themselves are covered by their own tests; what
/// matters here is the ownership/class check in the portal wrappers.
/// </summary>
internal sealed class StubSender(Func<object, object> respond) : ISender
{
    public bool WasCalled { get; private set; }
    public object? LastRequest { get; private set; }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        LastRequest = request;
        return Task.FromResult((TResponse)respond(request));
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        WasCalled = true;
        LastRequest = request;
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        LastRequest = request;
        return Task.FromResult<object?>(respond(request));
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

public class PortalAssignmentsTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();

    private Guid _parentUserId;
    private Guid _myChildId;       // in myClass
    private Guid _otherChildId;    // not the parent's
    private Guid _myClassAssignmentId;
    private Guid _otherClassAssignmentId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Portal LMS", Code = "PLM" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var parent = new User { Email = "p@plm.zw", PasswordHash = "x", FirstName = "Pam", LastName = "Parent" };
            var myClass = new Class { Name = "Form 1", Level = 1, Capacity = 40 };
            var otherClass = new Class { Name = "Form 2", Level = 2, Capacity = 40 };
            var subject = new Subject { Name = "Maths", Code = "MAT" };
            var year = new AcademicYear { Name = "2026", Year = 2026, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
            db.Users.Add(parent);
            db.Classes.AddRange(myClass, otherClass);
            db.Subjects.Add(subject);
            db.AcademicYears.Add(year);
            await db.SaveChangesAsync();

            var term = new AcademicTermEntity { AcademicYearId = year.Id, Name = "Term 1", TermNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 4, 30) };
            db.AcademicTerms.Add(term);

            var myChild = new Student { StudentNumber = "S-MINE", FirstName = "Mine", LastName = "Child", DateOfBirth = new DateTime(2013, 1, 1), Gender = Gender.Other, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active, CurrentClassId = myClass.Id };
            var otherChild = new Student { StudentNumber = "S-OTHER", FirstName = "Other", LastName = "Child", DateOfBirth = new DateTime(2013, 1, 1), Gender = Gender.Other, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active, CurrentClassId = otherClass.Id };
            var guardian = new Guardian { FirstName = "Pam", LastName = "Parent", Gender = Gender.Other, UserId = parent.Id };
            db.Students.AddRange(myChild, otherChild);
            db.Guardians.Add(guardian);
            await db.SaveChangesAsync();

            db.StudentGuardians.Add(new StudentGuardian { StudentId = myChild.Id, GuardianId = guardian.Id, Relationship = "Parent", IsPrimaryContact = true });

            var myClassAssignment = new Assignment { ClassId = myClass.Id, SubjectId = subject.Id, AcademicTermId = term.Id, Title = "Homework A", DueDate = new DateTime(2026, 2, 20) };
            var otherClassAssignment = new Assignment { ClassId = otherClass.Id, SubjectId = subject.Id, AcademicTermId = term.Id, Title = "Homework B", DueDate = new DateTime(2026, 2, 20) };
            db.Assignments.AddRange(myClassAssignment, otherClassAssignment);
            await db.SaveChangesAsync();

            _parentUserId = parent.Id;
            _myChildId = myChild.Id;
            _otherChildId = otherChild.Id;
            _myClassAssignmentId = myClassAssignment.Id;
            _otherClassAssignmentId = otherClassAssignment.Id;
            _harness.CurrentUser.UserId = parent.Id;
        }
    }

    public Task DisposeAsync()
    {
        _harness.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Assignments_for_an_own_child_delegate_to_the_student_query()
    {
        var sender = new StubSender(_ => Result<List<StudentAssignmentDto>>.Success([new StudentAssignmentDto { Title = "Homework A" }]));

        await using var db = _harness.CreateDbContext();
        var result = await new GetMyChildAssignmentsQueryHandler(db, _harness.CurrentUser, sender)
            .Handle(new GetMyChildAssignmentsQuery(_myChildId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sender.WasCalled.Should().BeTrue();
        sender.LastRequest.Should().BeOfType<GetStudentAssignmentsQuery>();
    }

    [Fact]
    public async Task Assignments_for_another_persons_child_are_denied_without_delegating()
    {
        var sender = new StubSender(_ => throw new InvalidOperationException("must not delegate for a non-owned child"));

        await using var db = _harness.CreateDbContext();
        var result = await new GetMyChildAssignmentsQueryHandler(db, _harness.CurrentUser, sender)
            .Handle(new GetMyChildAssignmentsQuery(_otherChildId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        sender.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Profile_for_another_persons_child_is_denied_without_delegating()
    {
        var sender = new StubSender(_ => throw new InvalidOperationException("must not delegate"));

        await using var db = _harness.CreateDbContext();
        var result = await new GetMyChildProfileQueryHandler(db, _harness.CurrentUser, sender)
            .Handle(new GetMyChildProfileQuery(_otherChildId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        sender.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Submitting_for_an_own_child_and_own_class_assignment_delegates()
    {
        var submissionId = Guid.NewGuid();
        var sender = new StubSender(_ => Result<Guid>.Success(submissionId));

        await using var db = _harness.CreateDbContext();
        var result = await new SubmitMyChildAssignmentCommandHandler(db, _harness.CurrentUser, sender)
            .Handle(new SubmitMyChildAssignmentCommand { StudentId = _myChildId, AssignmentId = _myClassAssignmentId, Comment = "Done" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(submissionId);
        sender.LastRequest.Should().BeOfType<RecordAssignmentSubmissionCommand>();
    }

    [Fact]
    public async Task Submitting_against_an_assignment_from_another_class_is_rejected()
    {
        var sender = new StubSender(_ => throw new InvalidOperationException("must not delegate"));

        await using var db = _harness.CreateDbContext();
        var result = await new SubmitMyChildAssignmentCommandHandler(db, _harness.CurrentUser, sender)
            .Handle(new SubmitMyChildAssignmentCommand { StudentId = _myChildId, AssignmentId = _otherClassAssignmentId }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        sender.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Submitting_for_another_persons_child_is_denied()
    {
        var sender = new StubSender(_ => throw new InvalidOperationException("must not delegate"));

        await using var db = _harness.CreateDbContext();
        var result = await new SubmitMyChildAssignmentCommandHandler(db, _harness.CurrentUser, sender)
            .Handle(new SubmitMyChildAssignmentCommand { StudentId = _otherChildId, AssignmentId = _otherClassAssignmentId }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        sender.WasCalled.Should().BeFalse();
    }
}
