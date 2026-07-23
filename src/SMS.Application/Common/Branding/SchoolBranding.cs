using SMS.Domain.Entities;

namespace SMS.Application.Common.Branding;

/// <summary>
/// A school's branding for documents (PDFs): name, contact details, logo image bytes and
/// brand colours. Built from the tenant via <see cref="BrandingBuilder"/>.
/// </summary>
public record SchoolBranding
{
    public string SchoolName { get; init; } = "School";
    public string? AddressLine { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }

    /// <summary>Decoded logo image bytes (from the tenant's data-URI logo), or null.</summary>
    public byte[]? LogoImage { get; init; }

    /// <summary>Validated #RRGGBB brand colours (always safe to hand to QuestPDF).</summary>
    public string PrimaryColor { get; init; } = DefaultPrimary;
    public string AccentColor { get; init; } = DefaultAccent;

    public const string DefaultPrimary = "#0F172A"; // slate-900
    public const string DefaultAccent = "#2563EB";  // blue-600
}

public static class BrandingBuilder
{
    public static SchoolBranding From(Tenant? tenant) => new()
    {
        SchoolName = string.IsNullOrWhiteSpace(tenant?.Name) ? "School" : tenant!.Name,
        AddressLine = ComposeAddress(tenant),
        Phone = NullIfBlank(tenant?.Phone),
        Email = NullIfBlank(tenant?.Email),
        Website = NullIfBlank(tenant?.Website),
        LogoImage = DecodeLogo(tenant?.Logo),
        PrimaryColor = NormalizeHex(tenant?.PrimaryColor, SchoolBranding.DefaultPrimary),
        AccentColor = NormalizeHex(tenant?.AccentColor, SchoolBranding.DefaultAccent)
    };

    private static string? ComposeAddress(Tenant? tenant)
    {
        if (tenant == null) return null;
        var parts = new[] { tenant.Address, tenant.City, tenant.Country }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var joined = string.Join(", ", parts);
        return joined.Length == 0 ? null : joined;
    }

    /// <summary>
    /// Extracts image bytes from a <c>data:image/...;base64,...</c> logo URI. A plain URL
    /// (no data payload) returns null - QuestPDF embeds bytes, it can't fetch a URL.
    /// </summary>
    private static byte[]? DecodeLogo(string? logo)
    {
        if (string.IsNullOrWhiteSpace(logo) || !logo.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var comma = logo.IndexOf(',');
        if (comma < 0 || comma == logo.Length - 1)
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(logo[(comma + 1)..]);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>Returns a validated <c>#RRGGBB</c> string, or the fallback for anything invalid.</summary>
    private static string NormalizeHex(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var v = value.Trim();
        if (!v.StartsWith('#')) v = "#" + v;

        if (v.Length == 7 && v[1..].All(Uri.IsHexDigit))
        {
            return v.ToUpperInvariant();
        }

        // #RGB shorthand -> #RRGGBB
        if (v.Length == 4 && v[1..].All(Uri.IsHexDigit))
        {
            return $"#{v[1]}{v[1]}{v[2]}{v[2]}{v[3]}{v[3]}".ToUpperInvariant();
        }

        return fallback;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
