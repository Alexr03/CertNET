using System.Runtime.InteropServices;

namespace CertNET.Plugins;

/// <summary>
/// Base class for DNS-based certbot plugins. Handles the temporary credential file lifecycle
/// so plugin authors only need to override <see cref="GetCredentialFileContent"/> and <see cref="Validate"/>.
/// </summary>
public abstract class DnsPluginBase : IDnsPlugin
{
    private string? _tempCredentialsPath;

    /// <inheritdoc />
    public abstract string AuthenticatorName { get; }

    /// <inheritdoc />
    /// <remarks>
    /// DNS plugins override this to return their specific Docker image
    /// (e.g., <c>"certbot/dns-cloudflare"</c>). Defaults to <c>"certbot/certbot"</c>.
    /// </remarks>
    public virtual string DockerImage => "certbot/certbot";

    /// <inheritdoc />
    /// <remarks>
    /// DNS plugins must override this to return their certbot pip package name
    /// (e.g., <c>"certbot-dns-cloudflare"</c>). This is used when auto-downloading
    /// certbot to install the required DNS plugin into the Python virtual environment.
    /// </remarks>
    public abstract string? CertbotPipPackage { get; }

    /// <inheritdoc />
    /// <remarks>
    /// DNS plugins override this to return their win-acme plugin package identifier
    /// (e.g., <c>"plugin.validation.dns.cloudflare"</c>). This is used when auto-downloading
    /// win-acme to also download the separate plugin zip from GitHub Releases.
    /// Defaults to <c>null</c> (no separate plugin download needed).
    /// </remarks>
    public virtual string? WinAcmePluginId => null;

    /// <inheritdoc />
    /// <remarks>
    /// DNS plugins must override this to return their win-acme validation plugin name
    /// (e.g., <c>"cloudflare"</c>). Defaults to <c>null</c> (unsupported).
    /// </remarks>
    public virtual string? WinAcmeValidationName => null;

    /// <summary>
    /// The default number of seconds to wait for DNS propagation.
    /// Each DNS provider has a different recommended default.
    /// </summary>
    public abstract int DefaultPropagationSeconds { get; }

    /// <summary>
    /// The number of seconds to wait for DNS propagation before asking the ACME server
    /// to verify the DNS record. If <c>null</c>, the <see cref="DefaultPropagationSeconds"/> is used.
    /// </summary>
    public int? PropagationSeconds { get; set; }

    /// <summary>
    /// Override to return the INI-formatted content of the credentials file.
    /// <para>
    /// For example, for Cloudflare with an API token:
    /// <code>dns_cloudflare_api_token = abc123</code>
    /// </para>
    /// </summary>
    protected abstract string GetCredentialFileContent();

    /// <summary>
    /// Override to return any plugin-specific extra CLI arguments beyond
    /// the standard credentials and propagation flags.
    /// </summary>
    protected virtual IEnumerable<string> GetAdditionalArguments() => [];

    /// <inheritdoc />
    public abstract void Validate();

    /// <inheritdoc />
    public string WriteCredentialsFile()
    {
        var content = GetCredentialFileContent();
        _tempCredentialsPath = Path.Combine(Path.GetTempPath(), $"certnet-{Guid.NewGuid():N}.ini");
        File.WriteAllText(_tempCredentialsPath, content);

        // On Unix, restrict file permissions to owner-only (chmod 600)
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.SetUnixFileMode(_tempCredentialsPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        return _tempCredentialsPath;
    }

    /// <inheritdoc />
    public void CleanupCredentialsFile()
    {
        if (_tempCredentialsPath is not null && File.Exists(_tempCredentialsPath))
        {
            try
            {
                File.Delete(_tempCredentialsPath);
            }
            catch
            {
                // Best-effort cleanup
            }

            _tempCredentialsPath = null;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetArguments()
    {
        // The credentials path is set during WriteCredentialsFile() and used here.
        // If the credentials file hasn't been written yet, it will be written by the
        // command executor before calling GetArguments().
        if (_tempCredentialsPath is not null)
        {
            yield return $"--{AuthenticatorName}-credentials";
            yield return _tempCredentialsPath;
        }

        var propagation = PropagationSeconds ?? DefaultPropagationSeconds;
        yield return $"--{AuthenticatorName}-propagation-seconds";
        yield return propagation.ToString();

        foreach (var arg in GetAdditionalArguments())
            yield return arg;
    }

    /// <inheritdoc />
    /// <remarks>
    /// DNS plugins should override this to return their win-acme-specific arguments.
    /// For example, Cloudflare returns <c>["--cloudflareapitoken", "token"]</c>.
    /// </remarks>
    public virtual IEnumerable<string> GetWinAcmeArguments() => [];

    /// <summary>
    /// Disposes the plugin, cleaning up any temporary credential files.
    /// </summary>
    public void Dispose()
    {
        CleanupCredentialsFile();
        GC.SuppressFinalize(this);
    }
}
