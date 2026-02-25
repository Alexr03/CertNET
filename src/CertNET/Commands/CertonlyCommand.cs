using CertNET.Execution;
using CertNET.Parsing;
using CertNET.Plugins;

namespace CertNET.Commands;

/// <summary>
/// Fluent builder for the <c>certbot certonly</c> command.
/// Obtains a certificate without installing it to a web server.
/// </summary>
public class CertonlyCommand : CommandBase
{
    /// <inheritdoc />
    protected override string SubCommand => "certonly";

    private string? _primaryDomain;

    internal CertonlyCommand(ICertnetExecutor executor, CertnetClientOptions options)
        : base(executor, options)
    {
    }

    /// <summary>
    /// Adds a single domain to the certificate request.
    /// Can be called multiple times to add multiple domains.
    /// </summary>
    /// <param name="domain">The domain name (e.g., <c>"example.com"</c> or <c>"*.example.com"</c>).</param>
    public CertonlyCommand ForDomain(string domain)
    {
        _primaryDomain ??= domain;
        Arguments.Add("-d");
        Arguments.Add(domain);
        return this;
    }

    /// <summary>
    /// Adds multiple domains to the certificate request.
    /// </summary>
    /// <param name="domains">The domain names to include in the certificate.</param>
    public CertonlyCommand ForDomains(params string[] domains)
    {
        foreach (var domain in domains)
        {
            _primaryDomain ??= domain;
            Arguments.Add("-d");
            Arguments.Add(domain);
        }

        return this;
    }

    /// <summary>
    /// Sets the certificate name used for management and file paths.
    /// Maps to <c>--cert-name</c>. Defaults to the first domain.
    /// </summary>
    public CertonlyCommand WithCertName(string certName)
    {
        Arguments.Add("--cert-name");
        Arguments.Add(certName);
        return this;
    }

    /// <summary>
    /// Sets the type of private key to generate. Maps to <c>--key-type</c>.
    /// </summary>
    public CertonlyCommand WithKeyType(KeyType keyType)
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
    /// Sets the RSA key size. Only applies when <see cref="KeyType.Rsa"/> is used.
    /// Maps to <c>--rsa-key-size</c>. Common values: 2048, 4096.
    /// </summary>
    public CertonlyCommand WithRsaKeySize(int size)
    {
        Arguments.Add("--rsa-key-size");
        Arguments.Add(size.ToString());
        return this;
    }

    /// <summary>
    /// Sets the elliptic curve for ECDSA keys. Only applies when <see cref="KeyType.Ecdsa"/> is used.
    /// Maps to <c>--elliptic-curve</c>.
    /// </summary>
    public CertonlyCommand WithEllipticCurve(EllipticCurve curve)
    {
        Arguments.Add("--elliptic-curve");
        Arguments.Add(curve switch
        {
            EllipticCurve.Secp256r1 => "secp256r1",
            EllipticCurve.Secp384r1 => "secp384r1",
            _ => "secp256r1"
        });
        return this;
    }

    /// <summary>
    /// Sets the preferred certificate chain by subject Common Name.
    /// Maps to <c>--preferred-chain</c>.
    /// </summary>
    public CertonlyCommand WithPreferredChain(string chain)
    {
        Arguments.Add("--preferred-chain");
        Arguments.Add(chain);
        return this;
    }

    /// <summary>
    /// Forces renewal of the certificate, even if it is not near expiry.
    /// Maps to <c>--force-renewal</c>.
    /// </summary>
    public CertonlyCommand ForceRenewal()
    {
        Arguments.Add("--force-renewal");
        return this;
    }

    /// <summary>
    /// Performs a test run against the staging server without saving certificates to disk.
    /// Maps to <c>--dry-run</c>.
    /// </summary>
    public CertonlyCommand DryRun()
    {
        Arguments.Add("--dry-run");
        return this;
    }

    /// <summary>
    /// Sets the authenticator plugin using a parameterless constructor.
    /// Use this for plugins that don't require configuration (e.g., <see cref="StandalonePlugin"/>).
    /// </summary>
    /// <typeparam name="TPlugin">The plugin type, which must have a parameterless constructor.</typeparam>
    public CertonlyCommand WithPlugin<TPlugin>() where TPlugin : ICertnetPlugin, new()
    {
        Plugin = new TPlugin();
        return this;
    }

    /// <summary>
    /// Sets the authenticator plugin using a pre-configured instance.
    /// Use this when the plugin requires configuration (e.g., credentials, paths).
    /// </summary>
    /// <param name="plugin">The configured plugin instance.</param>
    public CertonlyCommand WithPlugin(ICertnetPlugin plugin)
    {
        Plugin = plugin;
        return this;
    }

    /// <summary>
    /// Sets a command to run before obtaining the certificate.
    /// Maps to <c>--pre-hook</c>.
    /// </summary>
    public CertonlyCommand WithPreHook(string command)
    {
        Arguments.Add("--pre-hook");
        Arguments.Add(command);
        return this;
    }

    /// <summary>
    /// Sets a command to run after attempting to obtain the certificate.
    /// Maps to <c>--post-hook</c>.
    /// </summary>
    public CertonlyCommand WithPostHook(string command)
    {
        Arguments.Add("--post-hook");
        Arguments.Add(command);
        return this;
    }

    /// <summary>
    /// Sets a command to run after successfully obtaining the certificate.
    /// Maps to <c>--deploy-hook</c>.
    /// </summary>
    public CertonlyCommand WithDeployHook(string command)
    {
        Arguments.Add("--deploy-hook");
        Arguments.Add(command);
        return this;
    }

    /// <summary>
    /// Sets the path where the certificate PEM file will be saved.
    /// Maps to <c>--cert-path</c>.
    /// </summary>
    /// <param name="path">The file path for the certificate PEM (e.g., <c>/certs/cert.pem</c>).</param>
    public CertonlyCommand WithCertPath(string path)
    {
        Arguments.Add("--cert-path");
        Arguments.Add(path);
        return this;
    }

    /// <summary>
    /// Sets the path where the private key PEM file will be saved.
    /// Maps to <c>--key-path</c>.
    /// </summary>
    /// <param name="path">The file path for the private key PEM (e.g., <c>/certs/privkey.pem</c>).</param>
    public CertonlyCommand WithKeyPath(string path)
    {
        Arguments.Add("--key-path");
        Arguments.Add(path);
        return this;
    }

    /// <summary>
    /// Sets the path where the full certificate chain PEM file will be saved.
    /// Maps to <c>--fullchain-path</c>.
    /// </summary>
    /// <param name="path">The file path for the full chain PEM (e.g., <c>/certs/fullchain.pem</c>).</param>
    public CertonlyCommand WithFullChainPath(string path)
    {
        Arguments.Add("--fullchain-path");
        Arguments.Add(path);
        return this;
    }

    /// <summary>
    /// Sets the path where the intermediate chain PEM file (without the leaf certificate) will be saved.
    /// Maps to <c>--chain-path</c>.
    /// </summary>
    /// <param name="path">The file path for the chain PEM (e.g., <c>/certs/chain.pem</c>).</param>
    public CertonlyCommand WithChainPath(string path)
    {
        Arguments.Add("--chain-path");
        Arguments.Add(path);
        return this;
    }

    /// <summary>
    /// Convenience method that sets all four output paths (<c>--cert-path</c>, <c>--key-path</c>,
    /// <c>--fullchain-path</c>, <c>--chain-path</c>) to standard filenames within the specified directory.
    /// <para>
    /// The files written will be: <c>cert.pem</c>, <c>privkey.pem</c>, <c>fullchain.pem</c>, and <c>chain.pem</c>.
    /// </para>
    /// <para>
    /// When running via Docker, the directory is automatically volume-mounted into the container.
    /// </para>
    /// </summary>
    /// <param name="directoryPath">The host directory where certificate files should be written.</param>
    public CertonlyCommand WithOutputDirectory(string directoryPath)
    {
        var dir = directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        OutputDirectory = dir;

        Arguments.Add("--cert-path");
        Arguments.Add(Path.Combine(dir, "cert.pem"));
        Arguments.Add("--key-path");
        Arguments.Add(Path.Combine(dir, "privkey.pem"));
        Arguments.Add("--fullchain-path");
        Arguments.Add(Path.Combine(dir, "fullchain.pem"));
        Arguments.Add("--chain-path");
        Arguments.Add(Path.Combine(dir, "chain.pem"));

        return this;
    }

    /// <summary>
    /// The output directory set via <see cref="WithOutputDirectory"/>, if any.
    /// Used by <see cref="CommandBase"/> to configure Docker volume mounts.
    /// </summary>
    internal string? OutputDirectory { get; private set; }

    /// <summary>
    /// Allows making a certificate that duplicates an existing one.
    /// Maps to <c>--duplicate</c>.
    /// </summary>
    public CertonlyCommand AllowDuplicate()
    {
        Arguments.Add("--duplicate");
        return this;
    }

    /// <summary>
    /// Expands an existing certificate to include additional domains.
    /// Maps to <c>--expand</c>.
    /// </summary>
    public CertonlyCommand Expand()
    {
        Arguments.Add("--expand");
        return this;
    }

    /// <summary>
    /// Executes the <c>certbot certonly</c> command with the configured options.
    /// <para>
    /// On success, the returned <see cref="CertnetResult.Certificate"/> is populated with
    /// the certificate file paths and PEM contents (when the files exist on disk).
    /// Paths are resolved from the explicit output directory, or parsed from the tool's stdout.
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The result of the certbot execution, including certificate output.</returns>
    /// <exception cref="Exceptions.CertnetExecutionException">Thrown when certbot returns a non-zero exit code.</exception>
    /// <exception cref="Exceptions.PluginValidationException">Thrown when the plugin configuration is invalid.</exception>
    public async Task<CertnetResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await ExecuteCoreAsync(cancellationToken);

        // Enrich the result with certificate file paths and PEM contents
        result.Certificate = CertificateOutputParser.Parse(
            result.StandardOutput,
            OutputDirectory,
            _primaryDomain);

        return result;
    }
}
