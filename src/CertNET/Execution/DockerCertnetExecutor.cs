using System.Diagnostics;
using System.Runtime.InteropServices;
using CertNET.Exceptions;

namespace CertNET.Execution;

/// <summary>
/// Executes certbot commands inside a Docker container using official certbot images.
/// This enables certbot usage on Windows (where native certbot is discontinued) and
/// provides a consistent, isolated execution environment on all platforms.
/// <para>
/// The executor runs: <c>docker run --rm -v ... certbot/&lt;image&gt; &lt;certbot args&gt;</c>
/// </para>
/// </summary>
public class DockerCertnetExecutor : ICertnetExecutor
{
    private readonly CertnetClientOptions _options;
    private readonly string _dockerPath;

    /// <summary>
    /// The Docker image to use. This is typically set per-command based on the plugin.
    /// When <c>null</c>, defaults to <c>"certbot/certbot"</c>.
    /// </summary>
    internal string? DockerImageOverride { get; set; }

    /// <summary>
    /// Maps host file paths to their container-internal paths.
    /// Used to mount credential files into the container and rewrite certbot arguments
    /// so they reference the container path instead of the host path.
    /// </summary>
    internal Dictionary<string, string> PathMappings { get; } = new();

    /// <summary>
    /// Maps host directory paths to their container-internal directory paths for writable mounts.
    /// Unlike <see cref="PathMappings"/> (which are mounted read-only), these are mounted read-write
    /// so certbot can write output files (e.g., certificate files) into the directory.
    /// </summary>
    internal Dictionary<string, string> WritableVolumeMounts { get; } = new();

    /// <summary>
    /// Creates a new <see cref="DockerCertnetExecutor"/>.
    /// </summary>
    /// <param name="options">The client options. <see cref="CertnetClientOptions.ExecutablePath"/>
    /// can be used to specify a custom path to the <c>docker</c> binary.</param>
    public DockerCertnetExecutor(CertnetClientOptions options)
    {
        _options = options;
        _dockerPath = LocateDocker(options.ExecutablePath);
    }

    /// <inheritdoc />
    public async Task<CertnetResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var dockerImage = DockerImageOverride ?? "certbot/certbot";
        var dockerArgs = BuildDockerArguments(dockerImage, arguments);
        var argumentString = string.Join(' ', dockerArgs.Select(EscapeArgument));

        var startInfo = new ProcessStartInfo
        {
            FileName = _dockerPath,
            Arguments = argumentString,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        return await ProcessRunner.RunAsync(
            startInfo,
            _options.Timeout,
            "Docker certbot",
            _options.OutputDataReceived,
            _options.ErrorDataReceived,
            cancellationToken);
    }

    private List<string> BuildDockerArguments(string dockerImage, IReadOnlyList<string> certbotArgs)
    {
        var args = new List<string>
        {
            "run",
            "--rm"
        };

        // Mount the letsencrypt configuration directory
        var configDir = _options.ConfigDirectory ?? GetDefaultConfigDir();
        args.Add("-v");
        args.Add($"{configDir}:/etc/letsencrypt");

        // Mount the work directory
        var workDir = _options.WorkDirectory ?? GetDefaultWorkDir();
        args.Add("-v");
        args.Add($"{workDir}:/var/lib/letsencrypt");

        // Mount the logs directory
        var logsDir = _options.LogsDirectory ?? GetDefaultLogsDir();
        args.Add("-v");
        args.Add($"{logsDir}:/var/log/letsencrypt");

        // Mount any additional files (e.g., credential files) using the path mappings (read-only)
        foreach (var (hostPath, containerPath) in PathMappings)
        {
            args.Add("-v");
            args.Add($"{hostPath}:{containerPath}:ro");
        }

        // Mount writable directories (e.g., certificate output directories)
        foreach (var (hostPath, containerPath) in WritableVolumeMounts)
        {
            args.Add("-v");
            args.Add($"{hostPath}:{containerPath}");
        }

        // The Docker image
        args.Add(dockerImage);

        // Certbot arguments — strip container-managed flags and rewrite mapped paths
        var filteredArgs = RewriteArgumentsForContainer(certbotArgs);
        args.AddRange(filteredArgs);

        return args;
    }

    /// <summary>
    /// Removes --config-dir, --work-dir, and --logs-dir from the arguments
    /// (handled by volume mounts) and rewrites any host file paths that have
    /// a corresponding container path in <see cref="PathMappings"/>.
    /// </summary>
    private List<string> RewriteArgumentsForContainer(IReadOnlyList<string> args)
    {
        var filtered = new List<string>();
        var skipNext = false;

        string[] containerManagedFlags = ["--config-dir", "--work-dir", "--logs-dir"];

        for (var i = 0; i < args.Count; i++)
        {
            if (skipNext)
            {
                skipNext = false;
                continue;
            }

            if (containerManagedFlags.Contains(args[i]))
            {
                // Skip this flag and its value
                skipNext = true;
                continue;
            }

            // Rewrite host paths to container paths
            var arg = args[i];
            if (PathMappings.TryGetValue(arg, out var containerPath))
                arg = containerPath;

            filtered.Add(arg);
        }

        return filtered;
    }

    private static string GetDefaultConfigDir()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "certbot", "config");

        return "/etc/letsencrypt";
    }

    private static string GetDefaultWorkDir()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "certbot", "work");

        return "/var/lib/letsencrypt";
    }

    private static string GetDefaultLogsDir()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "certbot", "logs");

        return "/var/log/letsencrypt";
    }

    private static string LocateDocker(string? customPath)
    {
        // If the user specified a custom path, it's for Docker, not certbot
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
            return customPath;

        // Try to find docker on PATH
        var dockerName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "docker.exe" : "docker";

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
            foreach (var dir in pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries))
            {
                var fullPath = Path.Combine(dir.Trim(), dockerName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }

        // Well-known Docker Desktop locations on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var dockerDesktopPath = Path.Combine(programFiles, "Docker", "Docker", "resources", "bin", "docker.exe");
            if (File.Exists(dockerDesktopPath))
                return dockerDesktopPath;
        }

        throw new CertnetNotFoundException(
            "Docker was not found on the system PATH. " +
            "Docker is required to run certbot on Windows. " +
            "Please install Docker Desktop: https://www.docker.com/products/docker-desktop");
    }

    private static string EscapeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return "\"\"";

        if (arg.Contains(' ') || arg.Contains('"'))
            return $"\"{arg.Replace("\"", "\\\"")}\"";

        return arg;
    }

}
