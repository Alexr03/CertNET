namespace CertNET;

/// <summary>
/// Specifies the elliptic curve to use for ECDSA certificate keys.
/// </summary>
public enum EllipticCurve
{
    /// <summary>
    /// NIST P-256 curve (also known as prime256v1). This is the default.
    /// </summary>
    Secp256r1,

    /// <summary>
    /// NIST P-384 curve (also known as secp384r1).
    /// </summary>
    Secp384r1
}
