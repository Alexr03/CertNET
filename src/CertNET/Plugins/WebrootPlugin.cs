using CertNET.Exceptions;

namespace CertNET.Plugins;

/// <summary>
/// Obtains a certificate by placing challenge files in the web root directory
/// of an already running web server. The server must be configured to serve
/// files from <c>/.well-known/acme-challenge/</c>.
/// </summary>
public class WebrootPlugin : ICertnetPlugin
{
    /// <inheritdoc />
    public string AuthenticatorName => "webroot";

    /// <inheritdoc />
    public string DockerImage => "certbot/certbot";

    /// <inheritdoc />
    public string? CertbotPipPackage => null;

    /// <inheritdoc />
    public string? WinAcmePluginId => null;

    /// <inheritdoc />
    public string? WinAcmeValidationName => "filesystem";

    /// <summary>
    /// The top-level directory (web root) containing the files served by your web server.
    /// Maps to <c>--webroot-path</c> / <c>-w</c>.
    /// </summary>
    public required string WebRootPath { get; set; }

    /// <inheritdoc />
    public IEnumerable<string> GetArguments()
    {
        yield return "--webroot-path";
        yield return WebRootPath;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetWinAcmeArguments()
    {
        yield return "--webroot";
        yield return WebRootPath;
    }

    /// <inheritdoc />
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(WebRootPath))
            throw new PluginValidationException("WebRootPath must be specified for the webroot plugin.");
    }
}
