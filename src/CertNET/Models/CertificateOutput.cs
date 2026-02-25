namespace CertNET.Models;

/// <summary>
/// Contains the certificate files produced by a <c>certonly</c> command.
/// Provides both file paths and, when the files exist on disk, their PEM-encoded contents.
/// </summary>
public class CertificateOutput
{
    /// <summary>
    /// The directory containing the certificate files, or <c>null</c> if individual
    /// paths were specified instead of an output directory.
    /// </summary>
    public string? OutputDirectory { get; init; }

    /// <summary>
    /// Path to the server certificate file (e.g., <c>cert.pem</c>).
    /// </summary>
    public string? CertificatePath { get; init; }

    /// <summary>
    /// Path to the private key file (e.g., <c>privkey.pem</c>).
    /// </summary>
    public string? PrivateKeyPath { get; init; }

    /// <summary>
    /// Path to the full certificate chain file (e.g., <c>fullchain.pem</c>).
    /// This is the file most TLS servers need — it contains the server certificate
    /// followed by all intermediate certificates.
    /// </summary>
    public string? FullChainPath { get; init; }

    /// <summary>
    /// Path to the intermediate chain file (e.g., <c>chain.pem</c>).
    /// Contains only the intermediate certificates, without the server certificate.
    /// </summary>
    public string? ChainPath { get; init; }

    /// <summary>
    /// PEM-encoded server certificate content, or <c>null</c> if the file was not found on disk.
    /// </summary>
    public string? Certificate { get; init; }

    /// <summary>
    /// PEM-encoded private key content, or <c>null</c> if the file was not found on disk.
    /// </summary>
    public string? PrivateKey { get; init; }

    /// <summary>
    /// PEM-encoded full certificate chain content, or <c>null</c> if the file was not found on disk.
    /// </summary>
    public string? FullChain { get; init; }

    /// <summary>
    /// PEM-encoded intermediate chain content, or <c>null</c> if the file was not found on disk.
    /// </summary>
    public string? Chain { get; init; }

    /// <summary>
    /// Whether all four certificate files were found on disk.
    /// </summary>
    public bool HasAllFiles =>
        Certificate is not null
        && PrivateKey is not null
        && FullChain is not null
        && Chain is not null;

    /// <summary>
    /// Reads certificate file contents from disk for the given paths.
    /// Files that do not exist are set to <c>null</c>.
    /// </summary>
    internal static CertificateOutput FromPaths(
        string? outputDirectory,
        string? certPath,
        string? keyPath,
        string? fullChainPath,
        string? chainPath)
    {
        return new CertificateOutput
        {
            OutputDirectory = outputDirectory,
            CertificatePath = certPath,
            PrivateKeyPath = keyPath,
            FullChainPath = fullChainPath,
            ChainPath = chainPath,
            Certificate = TryReadFile(certPath),
            PrivateKey = TryReadFile(keyPath),
            FullChain = TryReadFile(fullChainPath),
            Chain = TryReadFile(chainPath),
        };
    }

    private static string? TryReadFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch
        {
            return null;
        }
    }
}
