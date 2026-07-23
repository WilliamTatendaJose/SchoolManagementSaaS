using FluentAssertions;
using SMS.Application.Common.Branding;
using SMS.Domain.Entities;
using Xunit;

namespace SMS.Application.Tests.Common;

public class BrandingBuilderTests
{
    [Fact]
    public void Falls_back_to_defaults_for_a_bare_tenant()
    {
        var b = BrandingBuilder.From(new Tenant { Name = "Demo School" });

        b.SchoolName.Should().Be("Demo School");
        b.PrimaryColor.Should().Be(SchoolBranding.DefaultPrimary);
        b.AccentColor.Should().Be(SchoolBranding.DefaultAccent);
        b.LogoImage.Should().BeNull();
    }

    [Fact]
    public void Null_tenant_yields_safe_defaults()
    {
        var b = BrandingBuilder.From(null);
        b.SchoolName.Should().Be("School");
        b.PrimaryColor.Should().Be(SchoolBranding.DefaultPrimary);
    }

    [Theory]
    [InlineData("#ff0000", "#FF0000")]
    [InlineData("00ff00", "#00FF00")]     // missing hash
    [InlineData("#0af", "#00AAFF")]       // shorthand
    [InlineData("not-a-color", SchoolBranding.DefaultPrimary)]
    [InlineData("", SchoolBranding.DefaultPrimary)]
    public void Normalizes_or_rejects_colours(string input, string expected)
    {
        var b = BrandingBuilder.From(new Tenant { Name = "S", PrimaryColor = input });
        b.PrimaryColor.Should().Be(expected);
    }

    [Fact]
    public void Decodes_a_data_uri_logo_to_bytes()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var dataUri = "data:image/png;base64," + System.Convert.ToBase64String(bytes);

        var b = BrandingBuilder.From(new Tenant { Name = "S", Logo = dataUri });

        b.LogoImage.Should().Equal(bytes);
    }

    [Fact]
    public void A_plain_url_logo_is_not_embedded()
    {
        // QuestPDF embeds bytes, not URLs - a plain URL must not be treated as image data.
        var b = BrandingBuilder.From(new Tenant { Name = "S", Logo = "https://example.com/logo.png" });
        b.LogoImage.Should().BeNull();
    }

    [Fact]
    public void Composes_the_address_line_from_parts()
    {
        var b = BrandingBuilder.From(new Tenant { Name = "S", Address = "1 Main St", City = "Harare", Country = "Zimbabwe" });
        b.AddressLine.Should().Be("1 Main St, Harare, Zimbabwe");
    }
}
