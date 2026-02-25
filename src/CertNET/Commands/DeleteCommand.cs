using CertNET.Execution;

namespace CertNET.Commands;

/// <summary>
/// Fluent builder for the <c>certbot delete</c> command.
/// Deletes a previously obtained certificate and all related files.
/// </summary>
public class DeleteCommand : CommandBase
{
    /// <inheritdoc />
    protected override string SubCommand => "delete";

    internal DeleteCommand(ICertnetExecutor executor, CertnetClientOptions options)
        : base(executor, options)
    {
    }

    /// <summary>
    /// Specifies the certificate to delete by its name.
    /// Maps to <c>--cert-name</c>.
    /// </summary>
    public DeleteCommand ForCertificate(string certName)
    {
        Arguments.Add("--cert-name");
        Arguments.Add(certName);
        return this;
    }

    /// <summary>
    /// Executes the <c>certbot delete</c> command with the configured options.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The result of the certbot execution.</returns>
    /// <exception cref="Exceptions.CertnetExecutionException">Thrown when certbot returns a non-zero exit code.</exception>
    public Task<CertnetResult> ExecuteAsync(CancellationToken cancellationToken = default)
        => ExecuteCoreAsync(cancellationToken);
}
