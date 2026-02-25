using CertNET.Execution;

namespace CertNET.Commands;

/// <summary>
/// Fluent builder for the <c>certbot revoke</c> command.
/// Revokes a previously obtained certificate.
/// </summary>
public class RevokeCommand : CommandBase
{
    /// <inheritdoc />
    protected override string SubCommand => "revoke";

    internal RevokeCommand(ICertnetExecutor executor, CertnetClientOptions options)
        : base(executor, options)
    {
    }

    /// <summary>
    /// Specifies the certificate to revoke by its name.
    /// Maps to <c>--cert-name</c>.
    /// </summary>
    public RevokeCommand ForCertificate(string certName)
    {
        Arguments.Add("--cert-name");
        Arguments.Add(certName);
        return this;
    }

    /// <summary>
    /// Specifies the certificate to revoke by its file path.
    /// Maps to <c>--cert-path</c>.
    /// </summary>
    public RevokeCommand ForCertificatePath(string certPath)
    {
        Arguments.Add("--cert-path");
        Arguments.Add(certPath);
        return this;
    }

    /// <summary>
    /// Specifies the private key path to use for revocation.
    /// Maps to <c>--key-path</c>.
    /// </summary>
    public RevokeCommand WithKeyPath(string keyPath)
    {
        Arguments.Add("--key-path");
        Arguments.Add(keyPath);
        return this;
    }

    /// <summary>
    /// Specifies the reason for revoking the certificate. Maps to <c>--reason</c>.
    /// </summary>
    public RevokeCommand WithReason(RevocationReason reason)
    {
        Arguments.Add("--reason");
        Arguments.Add(reason switch
        {
            RevocationReason.Unspecified => "unspecified",
            RevocationReason.KeyCompromise => "keycompromise",
            RevocationReason.AffiliationChanged => "affiliationchanged",
            RevocationReason.Superseded => "superseded",
            RevocationReason.CessationOfOperation => "cessationofoperation",
            _ => "unspecified"
        });
        return this;
    }

    /// <summary>
    /// Automatically deletes the certificate after successful revocation.
    /// Maps to <c>--delete-after-revoke</c>.
    /// </summary>
    public RevokeCommand DeleteAfterRevoke()
    {
        Arguments.Add("--delete-after-revoke");
        return this;
    }

    /// <summary>
    /// Explicitly does not delete the certificate after revocation.
    /// Maps to <c>--no-delete-after-revoke</c>.
    /// </summary>
    public RevokeCommand NoDeleteAfterRevoke()
    {
        Arguments.Add("--no-delete-after-revoke");
        return this;
    }

    /// <summary>
    /// Executes the <c>certbot revoke</c> command with the configured options.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The result of the certbot execution.</returns>
    /// <exception cref="Exceptions.CertnetExecutionException">Thrown when certbot returns a non-zero exit code.</exception>
    public Task<CertnetResult> ExecuteAsync(CancellationToken cancellationToken = default)
        => ExecuteCoreAsync(cancellationToken);
}
