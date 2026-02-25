namespace CertNET.Execution;

/// <summary>
/// Abstraction for executing certbot CLI commands.
/// Implement this interface to customize how certbot is invoked, or use the default
/// <see cref="CertnetExecutor"/> for standard process-based execution.
/// </summary>
public interface ICertnetExecutor
{
    /// <summary>
    /// Executes a certbot command with the specified arguments.
    /// </summary>
    /// <param name="arguments">The command-line arguments to pass to certbot.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The result of the certbot execution.</returns>
    Task<CertnetResult> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default);
}
