using FluentValidation.TestHelper;
using SMS.Application.Features.Transport.Commands;
using Xunit;

namespace SMS.Application.Tests.Features.Transport;

public class CreateTransportRouteCommandValidatorTests
{
    private readonly CreateTransportRouteCommandValidator _validator = new();

    private static CreateTransportRouteCommand ValidCommand() => new()
    {
        Name = "Route A",
        VehicleRegistration = "ABC 1234",
        Capacity = 20
    };

    [Fact]
    public void Validate_ShouldPass_WhenValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameEmpty()
    {
        _validator.TestValidate(ValidCommand() with { Name = "" })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_ShouldFail_WhenCapacityNotPositive(int capacity)
    {
        _validator.TestValidate(ValidCommand() with { Capacity = capacity })
            .ShouldHaveValidationErrorFor(x => x.Capacity);
    }
}
