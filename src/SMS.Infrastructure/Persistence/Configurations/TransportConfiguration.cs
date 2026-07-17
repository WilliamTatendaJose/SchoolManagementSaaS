using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMS.Domain.Entities;

namespace SMS.Infrastructure.Persistence.Configurations;

public class TransportRouteConfiguration : IEntityTypeConfiguration<TransportRoute>
{
    public void Configure(EntityTypeBuilder<TransportRoute> builder)
    {
        builder.ToTable("TransportRoutes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.VehicleRegistration)
            .HasMaxLength(20);

        builder.HasOne(r => r.Driver)
            .WithMany()
            .HasForeignKey(r => r.DriverId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.ToTable("RouteStops");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(s => new { s.TransportRouteId, s.SequenceNumber })
            .IsUnique();

        builder.HasOne(s => s.TransportRoute)
            .WithMany(r => r.Stops)
            .HasForeignKey(s => s.TransportRouteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
