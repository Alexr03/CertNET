using System.Runtime.InteropServices;
using CertNET.Commands;
using CertNET.Execution;

namespace CertNET;

/// <summary>
/// The main entry point for interacting with ACME certificate management from .NET.
/// Provides fluent builders for all major operations.
/// <para>
/// On Windows, operations are executed via win-acme (<c>wacs.exe</c>) by default.
/// On Linux/macOS, certbot is executed natively. Docker mode is also available on all platforms.
/// </para>
/// <para>
/// <b>Example usage:</b>
/// <code>
/// var client = new CertnetClient(options =>
/// {
///     options.Server = AcmeServer.Staging;
///     options.Email = "admin@example.com";
///     options.AgreeToTermsOfService = true;
/// });
///
/// var result = await client.Certonly()
///     .ForDomains("example.com", "www.example.com")
///     .WithPlugin&lt;StandalonePlugin&gt;()
///     .WithKeyType(KeyType.Ecdsa)
///     .ExecuteAsync();
/// </code>
/// </para>
/// </summary>
public class CertnetClient
{
    private readonly CertnetClientOptions _options;
    private readonly ICertnetExecutor _executor;

    /// <summary>
    /// Creates a new <see cref="CertnetClient"/> with the specified options.
    /// </summary>
    /// <param name="options">The configuration options for the client.</param>
    public CertnetClient(CertnetClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _executor = CreateExecutor(options);
    }

    /// <summary>
    /// Creates a new <see cref="CertnetClient"/> using a configuration action.
    /// </summary>
    /// <param name="configure">An action to configure the client options.</param>
    public CertnetClient(Action<CertnetClientOptions> configure)
    {
        _options = new CertnetClientOptions();
        configure(_options);
        _executor = CreateExecutor(_options);
    }

    /// <summary>
    /// Creates a new <see cref="CertnetClient"/> with the specified options and a custom executor.
    /// Primarily used for testing.
    /// </summary>
    /// <param name="options">The configuration options for the client.</param>
    /// <param name="executor">A custom executor for running certbot commands.</param>
    public CertnetClient(CertnetClientOptions options, ICertnetExecutor executor)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <summary>
    /// Creates a fluent builder for the <c>certbot certonly</c> command.
    /// Obtains a certificate without installing it to a web server.
    /// </summary>
    public CertonlyCommand Certonly() => new(_executor, _options);

    /// <summary>
    /// Creates a fluent builder for the <c>certbot renew</c> command.
    /// Renews previously obtained certificates that are near expiry.
    /// </summary>
    public RenewCommand Renew() => new(_executor, _options);

    /// <summary>
    /// Creates a fluent builder for the <c>certbot revoke</c> command.
    /// Revokes a previously obtained certificate.
    /// </summary>
    public RevokeCommand Revoke() => new(_executor, _options);

    /// <summary>
    /// Creates a fluent builder for the <c>certbot delete</c> command.
    /// Deletes a previously obtained certificate and all related files.
    /// </summary>
    public DeleteCommand Delete() => new(_executor, _options);

    /// <summary>
    /// Creates a fluent builder for the <c>certbot certificates</c> command.
    /// Lists all certificates managed by certbot with parsed information.
    /// </summary>
    public CertificatesCommand Certificates() => new(_executor, _options);

    /// <summary>
    /// Convenience method that renews all certificates that are near expiry.
    /// Equivalent to running <c>certbot renew</c> with no additional options.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The result of the certbot execution.</returns>
    public Task<CertnetResult> RenewAllAsync(CancellationToken cancellationToken = default)
        => Renew().ExecuteAsync(cancellationToken);

    private static ICertnetExecutor CreateExecutor(CertnetClientOptions options)
    {
        var mode = ResolveExecutionMode(options.ExecutionMode);

        return mode switch
        {
            ExecutionMode.WinAcme => new WinAcmeExecutor(options),
            ExecutionMode.Docker => new DockerCertnetExecutor(options),
            ExecutionMode.Native => new CertnetExecutor(options),
            _ => new WinAcmeExecutor(options)
        };
    }

    private static ExecutionMode ResolveExecutionMode(ExecutionMode mode)
    {
        if (mode != ExecutionMode.Auto)
            return mode;

        // On Windows, certbot is discontinued — use win-acme
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ExecutionMode.WinAcme
            : ExecutionMode.Native;
    }
}
