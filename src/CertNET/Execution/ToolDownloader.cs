using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using CertNET.Exceptions;

namespace CertNET.Execution;

/// <summary>
/// Downloads and installs ACME tools (win-acme, certbot) into user-local directories.
/// No administrator/root privileges are required.
/// <para>
/// This class is used when <see cref="CertnetClientOptions.AutoDownload"/> is <c>true</c>
/// and the required tool is not already installed on the system.
/// </para>
/// <para>
/// <b>Install locations:</b>
/// <list type="bullet">
///   <item><b>Windows (win-acme):</b> <c>%LOCALAPPDATA%\certnet\tools\win-acme\wacs.exe</c></item>
///   <item><b>Linux/macOS (certbot):</b> <c>~/.local/share/certnet/tools/certbot/bin/certbot</c></item>
/// </list>
/// </para>
/// </summary>
public static class ToolDownloader
{
    /// <summary>
    /// The known-good version of win-acme that will be downloaded.
    /// </summary>
    public const string WinAcmeVersion = "2.2.9.1701";

    /// <summary>
    /// The known-good version of certbot that will be pip-installed.
    /// </summary>
    public const string CertbotVersion = "3.1.0";

    /// <summary>
    /// Minimum acceptable size in bytes for a downloaded win-acme zip file (1 MB).
    /// </summary>
    internal const long MinZipSizeBytes = 1_048_576;

    /// <summary>
    /// Maximum acceptable size in bytes for a downloaded win-acme zip file (100 MB).
    /// </summary>
    internal const long MaxZipSizeBytes = 104_857_600;

    private static readonly HttpClient SharedHttpClient = new();
    private static readonly SemaphoreSlim InstallLock = new(1, 1);

    /// <summary>
    /// Returns the base directory for CertNET auto-downloaded tools.
    /// <list type="bullet">
    ///   <item><b>Windows:</b> <c>%LOCALAPPDATA%\certnet\tools\</c></item>
    ///   <item><b>Linux/macOS:</b> <c>~/.local/share/certnet/tools/</c></item>
    /// </list>
    /// </summary>
    public static string GetToolsBaseDir()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "certnet", "tools");
        }

        // Linux/macOS: XDG data home or ~/.local/share
        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrEmpty(xdgDataHome))
            xdgDataHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");

        return Path.Combine(xdgDataHome, "certnet", "tools");
    }

    /// <summary>
    /// Returns the directory where win-acme will be installed.
    /// </summary>
    public static string GetWinAcmeInstallDir()
        => Path.Combine(GetToolsBaseDir(), "win-acme");

    /// <summary>
    /// Returns the full path to the expected <c>wacs.exe</c> location.
    /// </summary>
    public static string GetWinAcmeExecutablePath()
        => Path.Combine(GetWinAcmeInstallDir(), "wacs.exe");

    /// <summary>
    /// Returns the directory where certbot's Python venv will be created.
    /// </summary>
    public static string GetCertbotInstallDir()
        => Path.Combine(GetToolsBaseDir(), "certbot");

    /// <summary>
    /// Returns the full path to the expected certbot executable in the venv.
    /// </summary>
    public static string GetCertbotExecutablePath()
    {
        var installDir = GetCertbotInstallDir();
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(installDir, "Scripts", "certbot.exe")
            : Path.Combine(installDir, "bin", "certbot");
    }

    /// <summary>
    /// Returns the download URL for the pinned win-acme version.
    /// </summary>
    public static string GetWinAcmeDownloadUrl()
        => $"https://github.com/win-acme/win-acme/releases/download/v{WinAcmeVersion}/win-acme.v{WinAcmeVersion}.x64.pluggable.zip";

    /// <summary>
    /// Ensures that win-acme is available at the auto-download location.
    /// Downloads and extracts it if missing or outdated.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The full path to <c>wacs.exe</c>.</returns>
    /// <exception cref="CertnetNotFoundException">
    /// Thrown if the download fails or the extracted archive is invalid.
    /// </exception>
    public static async Task<string> EnsureWinAcmeAsync(CancellationToken cancellationToken = default)
    {
        var installDir = GetWinAcmeInstallDir();
        var wacsPath = GetWinAcmeExecutablePath();

        // If already installed and up to date, return immediately
        if (File.Exists(wacsPath) && !IsVersionOutdated(installDir, WinAcmeVersion))
            return wacsPath;

        await InstallLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock (another thread may have installed it)
            if (File.Exists(wacsPath) && !IsVersionOutdated(installDir, WinAcmeVersion))
                return wacsPath;

            Directory.CreateDirectory(installDir);

            var downloadUrl = GetWinAcmeDownloadUrl();
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"certnet-winacme-{Guid.NewGuid():N}.zip");

            try
            {
                // Download the zip file
                using (var response = await SharedHttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();

                    await using var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await using var fileStream = File.Create(tempZipPath);
                    await httpStream.CopyToAsync(fileStream, cancellationToken);
                }

                // Validate file size
                var fileInfo = new FileInfo(tempZipPath);
                if (fileInfo.Length < MinZipSizeBytes || fileInfo.Length > MaxZipSizeBytes)
                {
                    throw new CertnetNotFoundException(
                        $"Downloaded win-acme archive has an unexpected size ({fileInfo.Length:N0} bytes). " +
                        $"Expected between {MinZipSizeBytes:N0} and {MaxZipSizeBytes:N0} bytes. " +
                        "The download may be corrupt or the URL may have changed.");
                }

                // Clean existing install directory contents (preserve the directory itself)
                if (Directory.Exists(installDir))
                {
                    foreach (var file in Directory.GetFiles(installDir))
                        File.Delete(file);
                    foreach (var dir in Directory.GetDirectories(installDir))
                        Directory.Delete(dir, recursive: true);
                }

                // Extract
                ZipFile.ExtractToDirectory(tempZipPath, installDir, overwriteFiles: true);

                if (!File.Exists(wacsPath))
                {
                    throw new CertnetNotFoundException(
                        $"win-acme was downloaded and extracted to '{installDir}', but wacs.exe was not found. " +
                        "The archive format may have changed.");
                }

                // Write version marker
                WriteVersionFile(installDir, WinAcmeVersion);
            }
            finally
            {
                // Clean up temp zip
                try { File.Delete(tempZipPath); } catch { /* best-effort */ }
            }

            return wacsPath;
        }
        catch (CertnetNotFoundException)
        {
            throw; // Re-throw our own exceptions
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new CertnetNotFoundException(
                $"Failed to download and install win-acme: {ex.Message}. " +
                "You can install it manually from https://www.win-acme.com/ " +
                "or specify the path via CertnetClientOptions.ExecutablePath.",
                ex);
        }
        finally
        {
            InstallLock.Release();
        }
    }

    /// <summary>
    /// Ensures that certbot is available at the auto-download location.
    /// Creates a Python venv and pip-installs certbot if missing or outdated.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The full path to the certbot executable.</returns>
    /// <exception cref="CertnetNotFoundException">
    /// Thrown if Python 3 is not available or pip install fails.
    /// </exception>
    public static async Task<string> EnsureCertbotAsync(CancellationToken cancellationToken = default)
    {
        var installDir = GetCertbotInstallDir();
        var certbotPath = GetCertbotExecutablePath();

        // If already installed and up to date, return immediately
        if (File.Exists(certbotPath) && !IsVersionOutdated(installDir, CertbotVersion))
            return certbotPath;

        await InstallLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (File.Exists(certbotPath) && !IsVersionOutdated(installDir, CertbotVersion))
                return certbotPath;

            // Find python3
            var python = LocatePython()
                ?? throw new CertnetNotFoundException(
                    "python3 is required to auto-install certbot but was not found on the system PATH. " +
                    "Please install Python 3 first, then retry with AutoDownload enabled.");

            Directory.CreateDirectory(Path.GetDirectoryName(installDir)!);

            // Create or upgrade the virtual environment
            await RunProcessAsync(python, $"-m venv \"{installDir}\"", cancellationToken);

            // Determine pip path inside the venv
            var pipPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(installDir, "Scripts", "pip.exe")
                : Path.Combine(installDir, "bin", "pip");

            // Install/upgrade certbot
            await RunProcessAsync(pipPath, $"install --upgrade certbot=={CertbotVersion}", cancellationToken);

            if (!File.Exists(certbotPath))
            {
                throw new CertnetNotFoundException(
                    $"certbot was pip-installed into '{installDir}', but the executable was not found at '{certbotPath}'. " +
                    "The Python venv structure may differ on this platform.");
            }

            // Write version marker
            WriteVersionFile(installDir, CertbotVersion);

            return certbotPath;
        }
        catch (CertnetNotFoundException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new CertnetNotFoundException(
                $"Failed to auto-install certbot: {ex.Message}. " +
                "You can install certbot manually (e.g., via snap, apt, or pip) " +
                "or specify the path via CertnetClientOptions.ExecutablePath.",
                ex);
        }
        finally
        {
            InstallLock.Release();
        }
    }

    /// <summary>
    /// Installs a certbot DNS plugin pip package into the auto-downloaded certbot venv.
    /// No-op if certbot was not auto-installed (i.e., the venv doesn't exist at the expected location).
    /// </summary>
    /// <param name="pipPackage">The pip package name (e.g., <c>"certbot-dns-cloudflare"</c>).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async Task EnsureCertbotDnsPluginAsync(string pipPackage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pipPackage))
            return;

        var installDir = GetCertbotInstallDir();
        var pipPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(installDir, "Scripts", "pip.exe")
            : Path.Combine(installDir, "bin", "pip");

        // Only install into our managed venv — if pip doesn't exist, certbot wasn't auto-installed
        if (!File.Exists(pipPath))
            return;

        try
        {
            await RunProcessAsync(pipPath, $"install --upgrade {pipPackage}", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new CertnetNotFoundException(
                $"Failed to install certbot DNS plugin '{pipPackage}': {ex.Message}. " +
                $"You can install it manually: {pipPath} install {pipPackage}",
                ex);
        }
    }

    /// <summary>
    /// Returns the download URL for a win-acme plugin zip from the GitHub Releases page.
    /// <para>
    /// win-acme DNS validation plugins (Cloudflare, Azure, Route53, etc.) are distributed
    /// as separate zip files that must be extracted into the same directory as <c>wacs.exe</c>.
    /// </para>
    /// </summary>
    /// <param name="pluginId">
    /// The plugin package identifier (e.g., <c>"plugin.validation.dns.cloudflare"</c>).
    /// </param>
    /// <returns>The download URL for the plugin zip.</returns>
    public static string GetWinAcmePluginDownloadUrl(string pluginId)
        => $"https://github.com/win-acme/win-acme/releases/download/v{WinAcmeVersion}/{pluginId}.v{WinAcmeVersion}.zip";

    /// <summary>
    /// Ensures that a win-acme plugin is installed alongside <c>wacs.exe</c>.
    /// Downloads and extracts the plugin zip from GitHub Releases if not already installed.
    /// <para>
    /// Win-acme DNS plugins (e.g., Cloudflare) are separate downloads that contain DLLs
    /// which must be present in the same directory as <c>wacs.exe</c> for the plugin to load.
    /// </para>
    /// </summary>
    /// <param name="pluginId">
    /// The plugin package identifier (e.g., <c>"plugin.validation.dns.cloudflare"</c>).
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="CertnetNotFoundException">
    /// Thrown if the plugin download fails.
    /// </exception>
    public static async Task EnsureWinAcmePluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        var installDir = GetWinAcmeInstallDir();
        var markerFile = GetPluginMarkerFilePath(installDir, pluginId);

        // If already installed for this version, skip
        if (File.Exists(markerFile) && !IsPluginMarkerOutdated(markerFile, WinAcmeVersion))
            return;

        await InstallLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (File.Exists(markerFile) && !IsPluginMarkerOutdated(markerFile, WinAcmeVersion))
                return;

            Directory.CreateDirectory(installDir);

            var downloadUrl = GetWinAcmePluginDownloadUrl(pluginId);
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"certnet-{pluginId}-{Guid.NewGuid():N}.zip");

            try
            {
                // Download the plugin zip
                using (var response = await SharedHttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();

                    await using var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await using var fileStream = File.Create(tempZipPath);
                    await httpStream.CopyToAsync(fileStream, cancellationToken);
                }

                // Extract plugin DLLs into the win-acme directory (alongside wacs.exe)
                ZipFile.ExtractToDirectory(tempZipPath, installDir, overwriteFiles: true);

                // Write marker file so we don't re-download next time
                File.WriteAllText(markerFile, WinAcmeVersion);
            }
            finally
            {
                // Clean up temp zip
                try { File.Delete(tempZipPath); } catch { /* best-effort */ }
            }
        }
        catch (CertnetNotFoundException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new CertnetNotFoundException(
                $"Failed to download win-acme plugin '{pluginId}': {ex.Message}. " +
                $"You can download it manually from {GetWinAcmePluginDownloadUrl(pluginId)} " +
                $"and extract it into the win-acme directory.",
                ex);
        }
        finally
        {
            InstallLock.Release();
        }
    }

    /// <summary>
    /// Forces re-download/reinstall of tools even if the current version matches.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async Task UpdateToolsAsync(CancellationToken cancellationToken = default)
    {
        // Delete version files to force re-download
        var winAcmeVersionFile = GetVersionFilePath(GetWinAcmeInstallDir());
        var certbotVersionFile = GetVersionFilePath(GetCertbotInstallDir());

        if (File.Exists(winAcmeVersionFile))
            File.Delete(winAcmeVersionFile);
        if (File.Exists(certbotVersionFile))
            File.Delete(certbotVersionFile);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            await EnsureWinAcmeAsync(cancellationToken);
        else
            await EnsureCertbotAsync(cancellationToken);
    }

    /// <summary>
    /// Checks whether the installed tool version is outdated compared to the expected version.
    /// Returns <c>true</c> if the version file is missing or contains an older version.
    /// </summary>
    /// <param name="installDir">The tool's install directory.</param>
    /// <param name="expectedVersion">The expected (latest known-good) version string.</param>
    /// <returns><c>true</c> if an update is needed; <c>false</c> if the version is current.</returns>
    internal static bool IsVersionOutdated(string installDir, string expectedVersion)
    {
        var versionFile = GetVersionFilePath(installDir);

        if (!File.Exists(versionFile))
            return true;

        var installedVersion = File.ReadAllText(versionFile).Trim();
        return !string.Equals(installedVersion, expectedVersion, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the path to the <c>version.txt</c> marker file for a tool install directory.
    /// </summary>
    internal static string GetVersionFilePath(string installDir)
        => Path.Combine(installDir, "version.txt");

    /// <summary>
    /// Gets the path to the marker file that tracks whether a win-acme plugin is installed.
    /// The marker file contains the version string of the installed plugin.
    /// </summary>
    /// <param name="installDir">The win-acme install directory.</param>
    /// <param name="pluginId">The plugin package identifier.</param>
    /// <returns>The path to the marker file (e.g., <c>{installDir}/plugin.validation.dns.cloudflare.installed</c>).</returns>
    internal static string GetPluginMarkerFilePath(string installDir, string pluginId)
        => Path.Combine(installDir, $"{pluginId}.installed");

    /// <summary>
    /// Checks whether a plugin marker file indicates an outdated version.
    /// The marker file directly contains the version string (unlike <see cref="IsVersionOutdated"/>
    /// which reads a <c>version.txt</c> inside a directory).
    /// </summary>
    internal static bool IsPluginMarkerOutdated(string markerFilePath, string expectedVersion)
    {
        if (!File.Exists(markerFilePath))
            return true;

        var installedVersion = File.ReadAllText(markerFilePath).Trim();
        return !string.Equals(installedVersion, expectedVersion, StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteVersionFile(string installDir, string version)
    {
        File.WriteAllText(GetVersionFilePath(installDir), version);
    }

    private static string? LocatePython()
    {
        // Try python3 first, then python (some systems only have python3)
        string[] candidates = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ["python3.exe", "python.exe"]
            : ["python3", "python"];

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

        foreach (var candidate in candidates)
        {
            foreach (var dir in pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries))
            {
                var fullPath = Path.Combine(dir.Trim(), candidate);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }

        return null;
    }

    private static async Task RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stderr = await stderrTask;
        _ = await stdoutTask; // consume stdout to prevent deadlocks

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Process '{fileName} {arguments}' failed with exit code {process.ExitCode}. " +
                $"stderr: {stderr}");
        }
    }
}
