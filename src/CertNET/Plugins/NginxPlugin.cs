namespace CertNET.Plugins;

/// <summary>
/// Automates obtaining and installing a certificate with Nginx.
/// Certbot will automatically modify the Nginx configuration to use the certificate.
/// </summary>
public class NginxPlugin : ICertnetPlugin
{
    /// <inheritdoc />
    public string AuthenticatorName => "nginx";

    /// <inheritdoc />
    public string DockerImage => "certbot/certbot";

    /// <inheritdoc />
    public string? CertbotPipPackage => null;

    /// <inheritdoc />
    public string? WinAcmePluginId => null;

    /// <inheritdoc />
    /// <remarks>
    /// Nginx is not available on Windows. This plugin is not supported in win-acme mode.
    /// </remarks>
    public string? WinAcmeValidationName => null;

    /// <summary>
    /// The Nginx server root directory. Maps to <c>--nginx-server-root</c>.
    /// Defaults to <c>/etc/nginx</c> or <c>/usr/local/etc/nginx</c>.
    /// </summary>
    public string? ServerRoot { get; set; }

    /// <summary>
    /// Path to the Nginx control binary. Maps to <c>--nginx-ctl</c>.
    /// Defaults to <c>nginx</c>.
    /// </summary>
    public string? NginxCtl { get; set; }

    /// <inheritdoc />
    public IEnumerable<string> GetArguments()
    {
        if (!string.IsNullOrWhiteSpace(ServerRoot))
        {
            yield return "--nginx-server-root";
            yield return ServerRoot;
        }

        if (!string.IsNullOrWhiteSpace(NginxCtl))
        {
            yield return "--nginx-ctl";
            yield return NginxCtl;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetWinAcmeArguments() => [];

    /// <inheritdoc />
    public void Validate()
    {
        // Nginx plugin has no required configuration beyond what certbot itself validates.
    }
}
