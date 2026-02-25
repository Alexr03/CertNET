namespace CertNET.Plugins;

/// <summary>
/// Extended interface for DNS-based certbot plugins that require credential files.
/// <para>
/// DNS plugins accept credentials as strongly-typed properties. The library automatically
/// handles writing a temporary credentials file (required by certbot), passing it via
/// CLI arguments, and cleaning it up after execution.
/// </para>
/// </summary>
public interface IDnsPlugin : ICertnetPlugin, IDisposable
{
    /// <summary>
    /// Writes the plugin's credentials to a temporary INI file on disk
    /// and returns the file path. Called by the execution layer just before
    /// invoking certbot.
    /// </summary>
    /// <returns>The absolute path to the temporary credentials file.</returns>
    string WriteCredentialsFile();

    /// <summary>
    /// Deletes the temporary credentials file created by <see cref="WriteCredentialsFile"/>.
    /// Called automatically after certbot execution completes (in a <c>finally</c> block).
    /// </summary>
    void CleanupCredentialsFile();
}
