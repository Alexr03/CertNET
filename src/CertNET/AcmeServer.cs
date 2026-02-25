namespace CertNET;

/// <summary>
/// Specifies which ACME server to use for certificate operations.
/// </summary>
public enum AcmeServer
{
    /// <summary>
    /// The Let's Encrypt production server. Certificates issued are trusted by browsers.
    /// URL: https://acme-v02.api.letsencrypt.org/directory
    /// </summary>
    Production,

    /// <summary>
    /// The Let's Encrypt staging server. Certificates issued are NOT trusted by browsers.
    /// Use this for testing to avoid hitting rate limits.
    /// URL: https://acme-staging-v02.api.letsencrypt.org/directory
    /// </summary>
    Staging
}
