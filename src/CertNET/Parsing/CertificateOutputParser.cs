using System.Text.RegularExpressions;
using CertNET.Models;

namespace CertNET.Parsing;

/// <summary>
/// Parses the stdout of certbot and win-acme to extract certificate file paths,
/// then reads the PEM contents from disk when available.
/// </summary>
internal static partial class CertificateOutputParser
{
    /// <summary>
    /// Attempts to build a <see cref="CertificateOutput"/> from a successful <c>certonly</c> result.
    /// <para>
    /// Resolution order:
    /// <list type="number">
    ///   <item>If an explicit output directory was provided via <c>WithOutputDirectory</c>,
    ///         the well-known file names are probed there.</item>
    ///   <item>Otherwise, paths are extracted from the tool's stdout:
    ///         <list type="bullet">
    ///           <item><b>certbot:</b> "Certificate is saved at:" / "Key is saved at:"</item>
    ///           <item><b>win-acme:</b> "Exporting .pem files to {dir}" + name-derived file names</item>
    ///         </list></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="stdout">The full standard output text.</param>
    /// <param name="outputDirectory">The output directory set by the user, or <c>null</c>.</param>
    /// <param name="primaryDomain">The first domain in the certificate request, used to derive
    /// win-acme PEM file names.</param>
    /// <returns>A <see cref="CertificateOutput"/> with paths and PEM contents,
    /// or <c>null</c> if no paths could be determined.</returns>
    internal static CertificateOutput? Parse(string stdout, string? outputDirectory, string? primaryDomain)
    {
        // Strategy 1: explicit output directory with well-known names
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            return FromOutputDirectory(outputDirectory);
        }

        // Strategy 2: parse certbot output
        var certbotResult = TryParseCertbot(stdout);
        if (certbotResult is not null)
            return certbotResult;

        // Strategy 3: parse win-acme output
        var winAcmeResult = TryParseWinAcme(stdout, primaryDomain);
        if (winAcmeResult is not null)
            return winAcmeResult;

        return null;
    }

    /// <summary>
    /// Builds output from a known output directory with standard certbot file names.
    /// </summary>
    private static CertificateOutput FromOutputDirectory(string dir)
    {
        var certPath = Path.Combine(dir, "cert.pem");
        var keyPath = Path.Combine(dir, "privkey.pem");
        var fullChainPath = Path.Combine(dir, "fullchain.pem");
        var chainPath = Path.Combine(dir, "chain.pem");

        return CertificateOutput.FromPaths(dir, certPath, keyPath, fullChainPath, chainPath);
    }

    /// <summary>
    /// Parses certbot stdout for certificate file paths.
    /// <para>
    /// Certbot outputs:
    /// <code>
    /// Certificate is saved at: /etc/letsencrypt/live/example.com/fullchain.pem
    /// Key is saved at:         /etc/letsencrypt/live/example.com/privkey.pem
    /// </code>
    /// </para>
    /// </summary>
    private static CertificateOutput? TryParseCertbot(string stdout)
    {
        var certMatch = CertbotCertPathRegex().Match(stdout);
        var keyMatch = CertbotKeyPathRegex().Match(stdout);

        if (!certMatch.Success && !keyMatch.Success)
            return null;

        string? fullChainPath = certMatch.Success ? certMatch.Groups[1].Value.Trim() : null;
        string? keyPath = keyMatch.Success ? keyMatch.Groups[1].Value.Trim() : null;

        // Derive other paths from the fullchain path directory
        string? certPath = null;
        string? chainPath = null;
        string? outputDir = null;

        if (fullChainPath is not null)
        {
            outputDir = Path.GetDirectoryName(fullChainPath);
            if (outputDir is not null)
            {
                certPath = Path.Combine(outputDir, "cert.pem");
                chainPath = Path.Combine(outputDir, "chain.pem");
            }
        }

        return CertificateOutput.FromPaths(outputDir, certPath, keyPath, fullChainPath, chainPath);
    }

    /// <summary>
    /// Parses win-acme stdout for PEM file output directory and derives file names.
    /// <para>
    /// Win-acme outputs:
    /// <code>
    /// Exporting .pem files to C:\Certificates\
    /// </code>
    /// Files are named: <c>{name}-crt.pem</c>, <c>{name}-key.pem</c>,
    /// <c>{name}-chain.pem</c> (full chain), <c>{name}-chain-only.pem</c>.
    /// Where <c>{name}</c> is the primary domain with <c>*</c> replaced by <c>_</c>.
    /// </para>
    /// </summary>
    private static CertificateOutput? TryParseWinAcme(string stdout, string? primaryDomain)
    {
        var dirMatch = WinAcmePemDirRegex().Match(stdout);
        if (!dirMatch.Success)
            return null;

        var dir = dirMatch.Groups[1].Value.Trim();

        // Derive PEM file name from the primary domain
        // win-acme replaces * with _ in file names
        var name = primaryDomain?.Replace("*", "_") ?? "certificate";

        var certPath = Path.Combine(dir, $"{name}-crt.pem");
        var keyPath = Path.Combine(dir, $"{name}-key.pem");
        var fullChainPath = Path.Combine(dir, $"{name}-chain.pem");
        var chainPath = Path.Combine(dir, $"{name}-chain-only.pem");

        return CertificateOutput.FromPaths(dir, certPath, keyPath, fullChainPath, chainPath);
    }

    // ── Regex patterns ──────────────────────────────────────────────

    /// <summary>
    /// Matches certbot's "Certificate is saved at: /path/to/fullchain.pem"
    /// </summary>
    [GeneratedRegex(@"Certificate is saved at:\s*(.+)", RegexOptions.Multiline)]
    private static partial Regex CertbotCertPathRegex();

    /// <summary>
    /// Matches certbot's "Key is saved at:         /path/to/privkey.pem"
    /// </summary>
    [GeneratedRegex(@"Key is saved at:\s+(.+)", RegexOptions.Multiline)]
    private static partial Regex CertbotKeyPathRegex();

    /// <summary>
    /// Matches win-acme's "Exporting .pem files to C:\path\"
    /// </summary>
    [GeneratedRegex(@"Exporting \.pem files to (.+)", RegexOptions.Multiline)]
    private static partial Regex WinAcmePemDirRegex();
}
