namespace CertNET;

/// <summary>
/// Specifies the ACME challenge type used for domain validation.
/// </summary>
public enum ChallengeType
{
    /// <summary>
    /// HTTP-01 challenge. Proves domain control by serving a file on port 80.
    /// </summary>
    Http01,

    /// <summary>
    /// DNS-01 challenge. Proves domain control by creating a DNS TXT record.
    /// This is the only challenge type that supports wildcard certificates.
    /// </summary>
    Dns01
}
