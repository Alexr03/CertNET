using CertNET.Models;

namespace CertNET.Execution;

/// <summary>
/// Represents the result of a certbot CLI execution.
/// </summary>
public class CertnetResult
{
    /// <summary>
    /// Whether the certbot command completed successfully (exit code 0).
    /// </summary>
    public bool Success => ExitCode == 0;

    /// <summary>
    /// The process exit code returned by certbot.
    /// </summary>
    public required int ExitCode { get; init; }

    /// <summary>
    /// The standard output captured from the certbot process.
    /// </summary>
    public required string StandardOutput { get; init; }

    /// <summary>
    /// The standard error captured from the certbot process.
    /// </summary>
    public required string StandardError { get; init; }

    /// <summary>
    /// The full command line that was executed, useful for debugging.
    /// </summary>
    public required string CommandLine { get; init; }

    /// <summary>
    /// The certificate files produced by a <c>certonly</c> command.
    /// Contains file paths and, when the files exist on disk, their PEM-encoded contents.
    /// <para>
    /// This is <c>null</c> for non-certonly commands (renew, revoke, delete, certificates)
    /// and when certificate file paths could not be determined.
    /// </para>
    /// </summary>
    public CertificateOutput? Certificate { get; internal set; }

    public override string ToString() =>
        Success
            ? $"certbot succeeded (exit code {ExitCode})"
            : $"certbot failed (exit code {ExitCode}): {StandardError}";
}
