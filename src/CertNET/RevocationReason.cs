namespace CertNET;

/// <summary>
/// Specifies the reason for revoking a certificate.
/// </summary>
public enum RevocationReason
{
    /// <summary>
    /// No specific reason given. This is the default.
    /// </summary>
    Unspecified,

    /// <summary>
    /// The certificate's private key has been compromised.
    /// </summary>
    KeyCompromise,

    /// <summary>
    /// The certificate holder's affiliation has changed.
    /// </summary>
    AffiliationChanged,

    /// <summary>
    /// The certificate has been replaced by a newer one.
    /// </summary>
    Superseded,

    /// <summary>
    /// The certificate holder has ceased operations.
    /// </summary>
    CessationOfOperation
}
