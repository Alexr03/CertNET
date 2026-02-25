namespace CertNET;

/// <summary>
/// Specifies how certbot should be executed.
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Automatically selects the best execution mode for the current platform.
    /// On Windows, defaults to <see cref="WinAcme"/>. On Linux/macOS, defaults to <see cref="Native"/>.
    /// </summary>
    Auto,

    /// <summary>
    /// Runs certbot directly as a native process. Requires certbot to be installed on the system.
    /// </summary>
    Native,

    /// <summary>
    /// Runs certbot inside a Docker container using official certbot images.
    /// Requires Docker to be installed and running. Works on all platforms including Windows.
    /// </summary>
    Docker,

    /// <summary>
    /// Runs win-acme (<c>wacs.exe</c>) on Windows. This is the recommended mode for Windows
    /// systems without Docker. Requires win-acme to be installed.
    /// <para>
    /// The library automatically translates the certbot-style fluent API into
    /// win-acme CLI arguments, so the user-facing API remains identical.
    /// </para>
    /// </summary>
    WinAcme
}
