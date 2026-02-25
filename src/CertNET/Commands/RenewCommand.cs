using CertNET.Execution;

namespace CertNET.Commands;

/// <summary>
/// Fluent builder for the <c>certbot renew</c> command.
/// Renews previously obtained certificates that are near expiry.
/// </summary>
public class RenewCommand : CommandBase
{
    /// <inheritdoc />
    protected override string SubCommand => "renew";

    internal RenewCommand(ICertnetExecutor executor, CertnetClientOptions options)
        : base(executor, options)
    {
    }

    /// <summary>
    /// Renews only the certificate with the specified name.
    /// Maps to <c>--cert-name</c>.
    /// </summary>
    public RenewCommand ForCertificate(string certName)
    {
        Arguments.Add("--cert-name");
        Arguments.Add(certName);
        return this;
    }

    /// <summary>
    /// Forces renewal of the certificate, even if it is not near expiry.
    /// Maps to <c>--force-renewal</c>.
    /// </summary>
    public RenewCommand ForceRenewal()
    {
        Arguments.Add("--force-renewal");
        return this;
    }

    /// <summary>
    /// Changes the key type during renewal. Maps to <c>--key-type</c>.
    /// </summary>
    public RenewCommand WithKeyType(KeyType keyType)
    {
        Arguments.Add("--key-type");
        Arguments.Add(keyType switch
        {
            KeyType.Rsa => "rsa",
            KeyType.Ecdsa => "ecdsa",
            _ => "ecdsa"
        });
        return this;
    }

    /// <summary>
    /// Performs a test renewal against the staging server without saving certificates.
    /// Maps to <c>--dry-run</c>.
    /// </summary>
    public RenewCommand DryRun()
    {
        Arguments.Add("--dry-run");
        return this;
    }

    /// <summary>
    /// Sets a command to run before attempting renewal.
    /// Maps to <c>--pre-hook</c>.
    /// </summary>
    public RenewCommand WithPreHook(string command)
    {
        Arguments.Add("--pre-hook");
        Arguments.Add(command);
        return this;
    }

    /// <summary>
    /// Sets a command to run after attempting renewal.
    /// Maps to <c>--post-hook</c>.
    /// </summary>
    public RenewCommand WithPostHook(string command)
    {
        Arguments.Add("--post-hook");
        Arguments.Add(command);
        return this;
    }

    /// <summary>
    /// Sets a command to run after a successful renewal.
    /// Maps to <c>--deploy-hook</c>.
    /// </summary>
    public RenewCommand WithDeployHook(string command)
    {
        Arguments.Add("--deploy-hook");
        Arguments.Add(command);
        return this;
    }

    /// <summary>
    /// Silences all output except errors. Implies non-interactive mode.
    /// Maps to <c>--quiet</c> / <c>-q</c>.
    /// </summary>
    public RenewCommand Quiet()
    {
        Arguments.Add("--quiet");
        return this;
    }

    /// <summary>
    /// Disables automatic renewal scheduling. Maps to <c>--no-autorenew</c>.
    /// </summary>
    public RenewCommand NoAutoRenew()
    {
        Arguments.Add("--no-autorenew");
        return this;
    }

    /// <summary>
    /// Reuses the existing private key during renewal. Maps to <c>--reuse-key</c>.
    /// </summary>
    public RenewCommand ReuseKey()
    {
        Arguments.Add("--reuse-key");
        return this;
    }

    /// <summary>
    /// Executes the <c>certbot renew</c> command with the configured options.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The result of the certbot execution.</returns>
    /// <exception cref="Exceptions.CertnetExecutionException">Thrown when certbot returns a non-zero exit code.</exception>
    public Task<CertnetResult> ExecuteAsync(CancellationToken cancellationToken = default)
        => ExecuteCoreAsync(cancellationToken);
}
