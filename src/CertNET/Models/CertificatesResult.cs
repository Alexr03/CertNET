using CertNET.Execution;

namespace CertNET.Models;

/// <summary>
/// The result of a <c>certbot certificates</c> command, containing parsed certificate information.
/// </summary>
public record CertificatesResult
{
    /// <summary>
    /// The list of certificates managed by certbot.
    /// </summary>
    public required IReadOnlyList<CertificateInfo> Certificates { get; init; }

    /// <summary>
    /// The raw certbot result, including stdout, stderr, and exit code.
    /// </summary>
    public required CertnetResult RawResult { get; init; }
}
