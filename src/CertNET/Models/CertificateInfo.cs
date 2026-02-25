namespace CertNET.Models;

/// <summary>
/// Represents information about a certificate managed by certbot.
/// Parsed from the output of <c>certbot certificates</c>.
/// </summary>
public record CertificateInfo
{
    /// <summary>
    /// The name used by certbot to identify this certificate.
    /// </summary>
    public required string CertificateName { get; init; }

    /// <summary>
    /// The serial number of the certificate.
    /// </summary>
    public string? SerialNumber { get; init; }

    /// <summary>
    /// The type of key used for this certificate (RSA or ECDSA).
    /// </summary>
    public required KeyType KeyType { get; init; }

    /// <summary>
    /// The domain names covered by this certificate.
    /// </summary>
    public required string[] Domains { get; init; }

    /// <summary>
    /// The expiry date and time of the certificate.
    /// </summary>
    public required DateTimeOffset ExpiryDate { get; init; }

    /// <summary>
    /// Whether the certificate is currently valid (not expired, not a test cert, etc.).
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// The file system path to the full certificate chain (fullchain.pem).
    /// </summary>
    public required string CertificatePath { get; init; }

    /// <summary>
    /// The file system path to the private key (privkey.pem).
    /// </summary>
    public required string PrivateKeyPath { get; init; }
}
