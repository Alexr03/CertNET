using CertNET.Exceptions;

namespace CertNET.Plugins;

/// <summary>
/// Uses certbot's built-in standalone web server to obtain a certificate.
/// The standalone server binds to port 80 to respond to HTTP-01 challenges.
/// <para>
/// This is useful on systems with no running web server, or when direct integration
/// with the local web server is not desired.
/// </para>
/// </summary>
public class StandalonePlugin : ICertnetPlugin
{
    /// <inheritdoc />
    public string AuthenticatorName => "standalone";

    /// <inheritdoc />
    public string DockerImage => "certbot/certbot";

    /// <inheritdoc />
    public string? CertbotPipPackage => null;

    /// <inheritdoc />
    public string? WinAcmePluginId => null;

    /// <inheritdoc />
    public string? WinAcmeValidationName => "selfhosting";

    /// <summary>
    /// Port to use for the HTTP-01 challenge. Defaults to 80.
    /// Maps to <c>--http-01-port</c>.
    /// </summary>
    public int? HttpPort { get; set; }

    /// <summary>
    /// Address to bind to for the HTTP-01 challenge.
    /// Maps to <c>--http-01-address</c>.
    /// </summary>
    public string? HttpAddress { get; set; }

    /// <inheritdoc />
    public IEnumerable<string> GetArguments()
    {
        if (HttpPort.HasValue)
        {
            yield return "--http-01-port";
            yield return HttpPort.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(HttpAddress))
        {
            yield return "--http-01-address";
            yield return HttpAddress;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetWinAcmeArguments()
    {
        if (HttpPort.HasValue)
        {
            yield return "--validationport";
            yield return HttpPort.Value.ToString();
        }
    }

    /// <inheritdoc />
    public void Validate()
    {
        if (HttpPort is < 1 or > 65535)
            throw new PluginValidationException($"HttpPort must be between 1 and 65535, got {HttpPort}.");
    }
}
