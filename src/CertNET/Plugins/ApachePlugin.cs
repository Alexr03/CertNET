namespace CertNET.Plugins;

/// <summary>
/// Automates obtaining and installing a certificate with Apache.
/// Certbot will automatically modify the Apache configuration to use the certificate.
/// </summary>
public class ApachePlugin : ICertnetPlugin
{
    /// <inheritdoc />
    public string AuthenticatorName => "apache";

    /// <inheritdoc />
    public string DockerImage => "certbot/certbot";

    /// <inheritdoc />
    public string? CertbotPipPackage => null;

    /// <inheritdoc />
    public string? WinAcmePluginId => null;

    /// <inheritdoc />
    /// <remarks>
    /// Apache is not available on Windows. This plugin is not supported in win-acme mode.
    /// </remarks>
    public string? WinAcmeValidationName => null;

    /// <summary>
    /// The Apache server root directory. Maps to <c>--apache-server-root</c>.
    /// Defaults to <c>/etc/apache2</c>.
    /// </summary>
    public string? ServerRoot { get; set; }

    /// <summary>
    /// Path to the Apache control binary. Maps to <c>--apache-ctl</c>.
    /// Defaults to <c>apache2ctl</c>.
    /// </summary>
    public string? ApacheCtl { get; set; }

    /// <inheritdoc />
    public IEnumerable<string> GetArguments()
    {
        if (!string.IsNullOrWhiteSpace(ServerRoot))
        {
            yield return "--apache-server-root";
            yield return ServerRoot;
        }

        if (!string.IsNullOrWhiteSpace(ApacheCtl))
        {
            yield return "--apache-ctl";
            yield return ApacheCtl;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetWinAcmeArguments() => [];

    /// <inheritdoc />
    public void Validate()
    {
        // Apache plugin has no required configuration beyond what certbot itself validates.
    }
}
