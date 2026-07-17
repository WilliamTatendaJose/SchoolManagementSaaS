using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SMS.Application.Features.Transport.Commands;
using SMS.Application.Features.Transport.Queries;
using SMS.Domain.Entities;
using SMS.Domain.Enums;
using Xunit;

namespace SMS.Infrastructure.Tests;

public class TransportTests : IAsyncLifetime
{
    private readonly TenantTestContext _harness = new();
    private Guid _studentAId;
    private Guid _studentBId;

    public async Task InitializeAsync()
    {
        var tenant = new Tenant { Name = "Transport School", Code = "TRN" };
        await using (var db = _harness.CreateDbContext())
        {
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }
        _harness.UseTenant(tenant.Id);

        await using (var db = _harness.CreateDbContext())
        {
            var cls = new Class { Name = "Form 1", Level = 1, Capacity = 40 };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();

            var a = new Student { StudentNumber = "S-TRN-1", FirstName = "Ali", LastName = "Rider", DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Male, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active, CurrentClassId = cls.Id };
            var b = new Student { StudentNumber = "S-TRN-2", FirstName = "Beth", LastName = "Rider", DateOfBirth = new DateTime(2012, 1, 1), Gender = Gender.Female, AdmissionDate = new DateTime(2026, 1, 1), Status = StudentStatus.Active, CurrentClassId = cls.Id };
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

    private async Task<(Guid routeId, Guid stop1Id, Guid stop2Id)> SeedRouteWithTwoStopsAsync(int capacity)
    {
        await using var db = _harness.CreateDbContext();

        var route = await new CreateTransportRouteCommandHandler(db).Handle(new CreateTransportRouteCommand
        {
            Name = "Route A",
            VehicleRegistration = "ABC 1234",
            Capacity = capacity
        }, CancellationToken.None);

        var stop1 = await new CreateRouteStopCommandHandler(db).Handle(new CreateRouteStopCommand
        {
            TransportRouteId = route.Data,
            Name = "Stop 1",
            SequenceNumber = 1,
            PickupTime = new TimeSpan(6, 30, 0)
        }, CancellationToken.None);

        var stop2 = await new CreateRouteStopCommandHandler(db).Handle(new CreateRouteStopCommand
        {
            TransportRouteId = route.Data,
            Name = "Stop 2",
            SequenceNumber = 2,
            PickupTime = new TimeSpan(6, 45, 0)
        }, CancellationToken.None);

        return (route.Data, stop1.Data, stop2.Data);
    }

    [Fact]
    public async Task Creating_a_route_and_stops_succeeds_and_stops_are_ordered()
    {
        var (routeId, _, _) = await SeedRouteWithTwoStopsAsync(capacity: 10);

        await using var db = _harness.CreateDbContext();
        var stops = await new GetRouteStopsQueryHandler(db).Handle(new GetRouteStopsQuery(routeId), CancellationToken.None);

        stops.IsSuccess.Should().BeTrue();
        stops.Data.Should().HaveCount(2);
        stops.Data![0].Name.Should().Be("Stop 1");
        stops.Data[1].Name.Should().Be("Stop 2");
    }

    [Fact]
    public async Task A_duplicate_sequence_number_on_the_same_route_is_rejected()
    {
        var (routeId, _, _) = await SeedRouteWithTwoStopsAsync(capacity: 10);

        await using var db = _harness.CreateDbContext();
        var result = await new CreateRouteStopCommandHandler(db).Handle(new CreateRouteStopCommand
        {
            TransportRouteId = routeId,
            Name = "Duplicate",
            SequenceNumber = 1
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("sequence");
    }

    [Fact]
    public async Task Assigning_a_student_to_a_stop_succeeds_and_shows_up_in_occupants()
    {
        var (_, stop1Id, _) = await SeedRouteWithTwoStopsAsync(capacity: 10);

        await using (var db = _harness.CreateDbContext())
        {
            var result = await new AssignStudentToRouteStopCommandHandler(db).Handle(new AssignStudentToRouteStopCommand
            {
                StudentId = _studentAId,
                RouteStopId = stop1Id
            }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
        }

        await using var db2 = _harness.CreateDbContext();
        var occupants = await new GetRouteStopOccupantsQueryHandler(db2)
            .Handle(new GetRouteStopOccupantsQuery(stop1Id), CancellationToken.None);

        occupants.IsSuccess.Should().BeTrue();
        occupants.Data.Should().ContainSingle();
        occupants.Data![0].FullName.Should().Contain("Ali");
    }

    [Fact]
    public async Task Capacity_is_enforced_across_the_whole_route_not_per_stop()
    {
        var (_, stop1Id, stop2Id) = await SeedRouteWithTwoStopsAsync(capacity: 1);

        await using (var db = _harness.CreateDbContext())
        {
            var first = await new AssignStudentToRouteStopCommandHandler(db).Handle(new AssignStudentToRouteStopCommand
            {
                StudentId = _studentAId,
                RouteStopId = stop1Id
            }, CancellationToken.None);
            first.IsSuccess.Should().BeTrue();
        }

        // Route capacity is 1 and already has a rider on stop1; a second rider on a
        // *different* stop of the same route should still be blocked.
        await using var db2 = _harness.CreateDbContext();
        var second = await new AssignStudentToRouteStopCommandHandler(db2).Handle(new AssignStudentToRouteStopCommand
        {
            StudentId = _studentBId,
            RouteStopId = stop2Id
        }, CancellationToken.None);

        second.IsSuccess.Should().BeFalse();
        second.Error.Should().Contain("capacity");
    }

    [Fact]
    public async Task Unassigning_a_student_frees_up_a_seat()
    {
        var (_, stop1Id, stop2Id) = await SeedRouteWithTwoStopsAsync(capacity: 1);

        await using (var db = _harness.CreateDbContext())
        {
            await new AssignStudentToRouteStopCommandHandler(db).Handle(new AssignStudentToRouteStopCommand
            {
                StudentId = _studentAId,
                RouteStopId = stop1Id
            }, CancellationToken.None);
        }

        await using (var db = _harness.CreateDbContext())
        {
            var unassign = await new AssignStudentToRouteStopCommandHandler(db).Handle(new AssignStudentToRouteStopCommand
            {
                StudentId = _studentAId,
                RouteStopId = null
            }, CancellationToken.None);
            unassign.IsSuccess.Should().BeTrue();
        }

        await using var db2 = _harness.CreateDbContext();
        var second = await new AssignStudentToRouteStopCommandHandler(db2).Handle(new AssignStudentToRouteStopCommand
        {
            StudentId = _studentBId,
            RouteStopId = stop2Id
        }, CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Routes_list_reports_riders_and_available_seats()
    {
        var (routeId, stop1Id, _) = await SeedRouteWithTwoStopsAsync(capacity: 5);

        await using (var db = _harness.CreateDbContext())
        {
            await new AssignStudentToRouteStopCommandHandler(db).Handle(new AssignStudentToRouteStopCommand
            {
                StudentId = _studentAId,
                RouteStopId = stop1Id
            }, CancellationToken.None);
        }

        await using var db2 = _harness.CreateDbContext();
        var routes = await new GetTransportRoutesQueryHandler(db2).Handle(new GetTransportRoutesQuery(), CancellationToken.None);

        routes.IsSuccess.Should().BeTrue();
        var route = routes.Data!.Single(r => r.Id == routeId);
        route.StopCount.Should().Be(2);
        route.Riders.Should().Be(1);
        route.AvailableSeats.Should().Be(4);
    }
}
