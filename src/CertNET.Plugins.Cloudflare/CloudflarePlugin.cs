using CertNET.Exceptions;
using CertNET.Plugins;

namespace CertNET.Plugins.Cloudflare;

/// <summary>
/// Cloudflare DNS plugin for certbot. Automates DNS-01 challenges by creating
/// and removing TXT records using the Cloudflare API.
/// <para>
/// Supports two authentication modes:
/// <list type="bullet">
///   <item><b>API Token (recommended):</b> Set <see cref="ApiToken"/> with a token that has
///   <c>Zone:DNS:Edit</c> permissions for the target zones.</item>
///   <item><b>Global API Key (legacy):</b> Set both <see cref="ApiKey"/> and <see cref="Email"/>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Example usage:</b>
/// <code>
/// var result = await client.Certonly()
///     .ForDomains("example.com", "*.example.com")
///     .WithPlugin(new CloudflarePlugin
///     {
///         ApiToken = "your-cloudflare-api-token",
///         PropagationSeconds = 30
///     })
///     .ExecuteAsync();
/// </code>
/// </para>
/// </summary>
public class CloudflarePlugin : DnsPluginBase
{
    /// <inheritdoc />
    public override string AuthenticatorName => "dns-cloudflare";

    /// <inheritdoc />
    public override string DockerImage => "certbot/dns-cloudflare";

    /// <inheritdoc />
    public override string? CertbotPipPackage => "certbot-dns-cloudflare";

    /// <inheritdoc />
    public override string? WinAcmePluginId => "plugin.validation.dns.cloudflare";

    /// <inheritdoc />
    public override string? WinAcmeValidationName => "cloudflare";

    /// <inheritdoc />
    public override int DefaultPropagationSeconds => 10;

    /// <summary>
    /// Cloudflare API Token (recommended authentication method).
    /// The token must have <c>Zone:DNS:Edit</c> permissions for the zones
    /// you need certificates for.
    /// <para>Use this <b>or</b> the <see cref="ApiKey"/>/<see cref="Email"/> combination, not both.</para>
    /// </summary>
    public string? ApiToken { get; set; }

    /// <summary>
    /// Cloudflare Global API Key (legacy authentication method, not recommended).
    /// This key can access the entire Cloudflare API for all domains in your account.
    /// Must be used together with <see cref="Email"/>.
    /// <para>Prefer <see cref="ApiToken"/> for better security.</para>
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Cloudflare account email address. Required only when using <see cref="ApiKey"/>.
    /// </summary>
    public string? Email { get; set; }

    /// <inheritdoc />
    public override void Validate()
    {
        var hasToken = !string.IsNullOrWhiteSpace(ApiToken);
        var hasKey = !string.IsNullOrWhiteSpace(ApiKey);
        var hasEmail = !string.IsNullOrWhiteSpace(Email);

        if (!hasToken && !hasKey)
            throw new PluginValidationException(
                "CloudflarePlugin requires either ApiToken or ApiKey to be set.");

        if (hasToken && hasKey)
            throw new PluginValidationException(
                "CloudflarePlugin: provide either ApiToken or ApiKey, not both.");

        if (hasKey && !hasEmail)
            throw new PluginValidationException(
                "CloudflarePlugin: Email is required when using ApiKey authentication.");
    }

    /// <inheritdoc />
    /// <remarks>
    /// win-acme's Cloudflare validation only supports API Token authentication
    /// (not the legacy Global API Key). The token is passed directly via
    /// <c>--cloudflareapitoken</c> — no temporary credential file is needed.
    /// </remarks>
    public override IEnumerable<string> GetWinAcmeArguments()
    {
        // win-acme only supports API Token for Cloudflare
        if (!string.IsNullOrWhiteSpace(ApiToken))
        {
            yield return "--cloudflareapitoken";
            yield return ApiToken;
        }
    }

    /// <inheritdoc />
    protected override string GetCredentialFileContent()
    {
        if (!string.IsNullOrWhiteSpace(ApiToken))
            return $"dns_cloudflare_api_token = {ApiToken}";

        return $"dns_cloudflare_email = {Email}\ndns_cloudflare_api_key = {ApiKey}";
    }
}
