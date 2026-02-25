namespace CertNET.Plugins;

/// <summary>
/// Defines a certbot authenticator/installer plugin.
/// Implement this interface to create custom plugins for domain validation.
/// </summary>
public interface ICertnetPlugin
{
    /// <summary>
    /// The certbot authenticator name passed via <c>--authenticator</c> or as a flag
    /// (e.g., <c>"standalone"</c>, <c>"webroot"</c>, <c>"dns-cloudflare"</c>).
    /// </summary>
    string AuthenticatorName { get; }

    /// <summary>
    /// The Docker image to use when running certbot in Docker mode.
    /// For example, <c>"certbot/certbot"</c> for core plugins or
    /// <c>"certbot/dns-cloudflare"</c> for the Cloudflare DNS plugin.
    /// </summary>
    string DockerImage { get; }

    /// <summary>
    /// The pip package name required by this plugin when certbot is auto-installed
    /// via <see cref="CertnetClientOptions.AutoDownload"/>.
    /// <para>
    /// Core plugins (standalone, webroot, manual, etc.) return <c>null</c> since they
    /// are included with certbot. DNS plugins return their pip package name
    /// (e.g., <c>"certbot-dns-cloudflare"</c>).
    /// </para>
    /// </summary>
    string? CertbotPipPackage { get; }

    /// <summary>
    /// The win-acme plugin package identifier for auto-download.
    /// Used to construct the download URL for the plugin zip from the win-acme GitHub Releases page.
    /// <para>
    /// Core plugins (standalone, webroot, etc.) return <c>null</c> because they are built into
    /// <c>wacs.exe</c>. DNS plugins that ship as separate downloads return their package id
    /// (e.g., <c>"plugin.validation.dns.cloudflare"</c>).
    /// </para>
    /// </summary>
    string? WinAcmePluginId { get; }

    /// <summary>
    /// The win-acme validation plugin name (e.g., <c>"selfhosting"</c>, <c>"cloudflare"</c>).
    /// Return <c>null</c> if this plugin is not supported in win-acme mode
    /// (e.g., nginx/apache plugins which are Linux-only).
    /// </summary>
    string? WinAcmeValidationName { get; }

    /// <summary>
    /// Returns the additional CLI arguments this plugin contributes to the certbot command.
    /// For example, a webroot plugin would return <c>["--webroot-path", "/var/www/html"]</c>.
    /// </summary>
    IEnumerable<string> GetArguments();

    /// <summary>
    /// Returns the win-acme-specific CLI arguments for this plugin.
    /// For example, a Cloudflare plugin would return <c>["--cloudflareapitoken", "token"]</c>.
    /// <para>
    /// Unlike <see cref="GetArguments"/> (which returns certbot-style arguments), these
    /// are passed directly to <c>wacs.exe</c>.
    /// </para>
    /// </summary>
    IEnumerable<string> GetWinAcmeArguments();

    /// <summary>
    /// Validates the plugin's configuration before execution.
    /// Throws <see cref="Exceptions.PluginValidationException"/> if the configuration is invalid.
    /// </summary>
    void Validate();
}
