namespace CertNET;

/// <summary>
/// Specifies the type of private key to generate for the certificate.
/// </summary>
public enum KeyType
{
    /// <summary>
    /// RSA key. Use <see cref="CertNET.Commands.CertonlyCommand.WithRsaKeySize"/> to control key size.
    /// </summary>
    Rsa,

    /// <summary>
    /// ECDSA key (default since certbot 2.0). Use <see cref="CertNET.Commands.CertonlyCommand.WithEllipticCurve"/> to control the curve.
    /// </summary>
    Ecdsa
}
