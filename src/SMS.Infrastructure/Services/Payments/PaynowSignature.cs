using System.Security.Cryptography;
using System.Text;

namespace SMS.Infrastructure.Services.Payments;

/// <summary>
/// Implements Paynow's hashing scheme: concatenate the field values in order, append the
/// integration key, SHA-512 the UTF-8 bytes, and hex-encode in uppercase.
/// </summary>
internal static class PaynowSignature
{
    public static string Hash(IEnumerable<string> orderedValues, string integrationKey)
    {
        var concatenated = string.Concat(orderedValues) + integrationKey;
        var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(concatenated));
        return Convert.ToHexString(bytes); // uppercase hex
    }

    /// <summary>
    /// Verifies the "hash" entry against the remaining fields (in their given order).
    /// </summary>
    public static bool Verify(IReadOnlyList<KeyValuePair<string, string>> fields, string integrationKey)
    {
        string? provided = null;
        var values = new List<string>(fields.Count);

        foreach (var (key, value) in fields)
        {
            if (string.Equals(key, "hash", StringComparison.OrdinalIgnoreCase))
            {
                provided = value;
                continue;
            }

            values.Add(value);
        }

        if (string.IsNullOrEmpty(provided))
        {
            return false;
        }

        var expected = Hash(values, integrationKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided.ToUpperInvariant()));
    }
}
