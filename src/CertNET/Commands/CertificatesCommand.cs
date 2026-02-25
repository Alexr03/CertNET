using CertNET.Execution;
using CertNET.Models;
using CertNET.Parsing;

namespace CertNET.Commands;

/// <summary>
/// Fluent builder for the <c>certbot certificates</c> command.
/// Lists all certificates managed by certbot and returns parsed information.
/// </summary>
public class CertificatesCommand : CommandBase
{
    /// <inheritdoc />
    protected override string SubCommand => "certificates";

    internal CertificatesCommand(ICertnetExecutor executor, CertnetClientOptions options)
        : base(executor, options)
    {
    }

    /// <summary>
    /// Executes the <c>certbot certificates</c> command and parses the output
    /// into strongly-typed <see cref="CertificateInfo"/> objects.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A result containing the parsed certificate list and raw output.</returns>
    /// <exception cref="Exceptions.CertnetExecutionException">Thrown when certbot returns a non-zero exit code.</exception>
    public async Task<CertificatesResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await ExecuteCoreAsync(cancellationToken);
        var certificates = CertificatesParser.Parse(result.StandardOutput);

        return new CertificatesResult
        {
            Certificates = certificates,
            RawResult = result
        };
    }
}
