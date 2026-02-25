using System.Runtime.InteropServices;

namespace CertNET.Execution;

/// <summary>
/// Locates the certbot executable on the current system.
/// </summary>
public static class CertnetLocator
{
    private static readonly string[] UnixPaths =
    [
        "/usr/bin/certbot",
        "/usr/local/bin/certbot",
        "/snap/bin/certbot",
        "/opt/certbot/bin/certbot",
        // Auto-download location (ToolDownloader installs certbot here)
    ];

    /// <summary>
    /// Attempts to find the certbot executable on the system.
    /// </summary>
    /// <param name="customPath">An optional user-specified path to the certbot executable.</param>
    /// <returns>The resolved path to the certbot executable.</returns>
    /// <exception cref="Exceptions.CertnetNotFoundException">Thrown when certbot cannot be found.</exception>
    public static string Locate(string? customPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            if (File.Exists(customPath))
                return customPath;

            throw new Exceptions.CertnetNotFoundException(
                $"The specified certbot executable was not found at '{customPath}'.");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return LocateOnPath("certbot.exe")
                   ?? throw new Exceptions.CertnetNotFoundException(
                       "certbot.exe was not found on the system PATH. " +
                       "Please install certbot or specify the path via CertnetClientOptions.ExecutablePath.");
        }

        // Linux / macOS: check well-known paths first, then auto-download location, then PATH
        foreach (var path in UnixPaths)
        {
            if (File.Exists(path))
                return path;
        }

        // Check the CertNET auto-download location
        var autoDownloadPath = ToolDownloader.GetCertbotExecutablePath();
        if (File.Exists(autoDownloadPath))
            return autoDownloadPath;

        return LocateOnPath("certbot")
               ?? throw new Exceptions.CertnetNotFoundException(
                   "certbot was not found at any known location or on the system PATH. " +
                   "Please install certbot or specify the path via CertnetClientOptions.ExecutablePath.");
    }

    private static string? LocateOnPath(string executable)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

        foreach (var directory in pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries))
        {
            var fullPath = Path.Combine(directory.Trim(), executable);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }
}
