namespace CertNET.Plugins;

/// <summary>
/// Provides manual instructions for domain validation, or automates it via hook scripts.
/// <para>
/// Certificates created with the manual plugin do NOT support automatic renewal
/// unless combined with authentication hook scripts via <see cref="AuthHook"/>.
/// </para>
/// </summary>
public class ManualPlugin : ICertnetPlugin
{
    /// <inheritdoc />
    public string AuthenticatorName => "manual";

    /// <inheritdoc />
    public string DockerImage => "certbot/certbot";

    /// <inheritdoc />
    public string? CertbotPipPackage => null;

    /// <inheritdoc />
    public string? WinAcmePluginId => null;

    /// <inheritdoc />
    /// <remarks>
    /// win-acme has a "manual" DNS validation mode, but it requires interactive input.
    /// The <c>script</c> validation plugin is the closest equivalent for unattended use.
    /// </remarks>
    public string? WinAcmeValidationName => "manual";

    /// <summary>
    /// The preferred ACME challenge type.
    /// DNS-01 is required for wildcard certificates.
    /// </summary>
    public ChallengeType PreferredChallenge { get; set; } = ChallengeType.Http01;

    /// <summary>
    /// Path to a script that is executed to set up the domain validation challenge.
    /// Maps to <c>--manual-auth-hook</c>.
    /// Providing this enables automatic renewal for manual certificates.
    /// </summary>
    public string? AuthHook { get; set; }

    /// <summary>
    /// Path to a script that is executed to clean up after domain validation.
    /// Maps to <c>--manual-cleanup-hook</c>.
    /// </summary>
    public string? CleanupHook { get; set; }

    /// <inheritdoc />
    public IEnumerable<string> GetArguments()
    {
        yield return "--preferred-challenges";
        yield return PreferredChallenge switch
        {
            ChallengeType.Http01 => "http",
            ChallengeType.Dns01 => "dns",
            _ => "http"
        };

        if (!string.IsNullOrWhiteSpace(AuthHook))
        {
            yield return "--manual-auth-hook";
            yield return AuthHook;
        }

        if (!string.IsNullOrWhiteSpace(CleanupHook))
        {
            yield return "--manual-cleanup-hook";
            yield return CleanupHook;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetWinAcmeArguments()
    {
        // Manual mode in win-acme doesn't have direct hook equivalents;
        // script-based validation would be used instead.
        return [];
    }

    /// <inheritdoc />
    public void Validate()
    {
        // Manual plugin has no required configuration beyond what certbot itself validates.
    }
}
