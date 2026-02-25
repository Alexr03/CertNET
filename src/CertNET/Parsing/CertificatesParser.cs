using System.Globalization;
using System.Text.RegularExpressions;
using CertNET.Models;

namespace CertNET.Parsing;

/// <summary>
/// Parses the text output of <c>certbot certificates</c> into strongly-typed <see cref="CertificateInfo"/> objects.
/// </summary>
public static partial class CertificatesParser
{
    /// <summary>
    /// Parses the raw stdout from <c>certbot certificates</c>.
    /// </summary>
    /// <param name="output">The standard output text from certbot.</param>
    /// <returns>A list of parsed certificate information.</returns>
    public static IReadOnlyList<CertificateInfo> Parse(string output)
    {
        var certificates = new List<CertificateInfo>();

        // Find all "Certificate Name:" occurrences and split into blocks
        var matches = CertNameRegex().Matches(output);
        if (matches.Count == 0)
            return certificates;

        for (var i = 0; i < matches.Count; i++)
        {
            var certName = matches[i].Groups[1].Value.Trim();
            var blockStart = matches[i].Index;
            var blockEnd = i + 1 < matches.Count ? matches[i + 1].Index : output.Length;
            var block = output[blockStart..blockEnd];

            var cert = TryParseBlock(certName, block);
            if (cert is not null)
                certificates.Add(cert);
        }

        return certificates;
    }

    private static CertificateInfo? TryParseBlock(string certName, string block)
    {
        var serialNumber = ExtractValue(block, SerialNumberRegex());
        var keyTypeStr = ExtractValue(block, KeyTypeRegex());
        var domainsStr = ExtractValue(block, DomainsRegex());
        var expiryStr = ExtractValue(block, ExpiryDateRegex());
        var certPath = ExtractValue(block, CertPathRegex());
        var keyPath = ExtractValue(block, KeyPathRegex());

        // We need at minimum domains, expiry, cert path, and key path
        if (domainsStr is null || expiryStr is null || certPath is null || keyPath is null)
            return null;

        var keyType = keyTypeStr?.ToUpperInvariant() switch
        {
            "RSA" => KeyType.Rsa,
            "ECDSA" => KeyType.Ecdsa,
            _ => KeyType.Ecdsa
        };

        var domains = domainsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Parse expiry date: "2025-11-16 20:27:27+00:00 (VALID: 30 days)" or "(INVALID: TEST_CERT)"
        var isValid = !expiryStr.Contains("INVALID", StringComparison.OrdinalIgnoreCase);
        var dateMatch = ExpiryDateValueRegex().Match(expiryStr);
        var expiryDate = dateMatch.Success
            ? DateTimeOffset.Parse(dateMatch.Groups[1].Value, CultureInfo.InvariantCulture)
            : DateTimeOffset.MinValue;

        return new CertificateInfo
        {
            CertificateName = certName,
            SerialNumber = serialNumber,
            KeyType = keyType,
            Domains = domains,
            ExpiryDate = expiryDate,
            IsValid = isValid,
            CertificatePath = certPath,
            PrivateKeyPath = keyPath
        };
    }

    private static string? ExtractValue(string text, Regex regex)
    {
        var match = regex.Match(text);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    [GeneratedRegex(@"Certificate Name:\s*(.+)", RegexOptions.Multiline)]
    private static partial Regex CertNameRegex();

    [GeneratedRegex(@"Serial Number:\s*(.+)", RegexOptions.Multiline)]
    private static partial Regex SerialNumberRegex();

    [GeneratedRegex(@"Key Type:\s*(.+)", RegexOptions.Multiline)]
    private static partial Regex KeyTypeRegex();

    [GeneratedRegex(@"Domains:\s*(.+)", RegexOptions.Multiline)]
    private static partial Regex DomainsRegex();

    [GeneratedRegex(@"Expiry Date:\s*(.+)", RegexOptions.Multiline)]
    private static partial Regex ExpiryDateRegex();

    [GeneratedRegex(@"Certificate Path:\s*(.+)", RegexOptions.Multiline)]
    private static partial Regex CertPathRegex();

    [GeneratedRegex(@"Private Key Path:\s*(.+)", RegexOptions.Multiline)]
    private static partial Regex KeyPathRegex();

    [GeneratedRegex(@"^([\d\-]+\s+[\d:]+[+\-][\d:]+)")]
    private static partial Regex ExpiryDateValueRegex();
}
