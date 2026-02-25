using System.Diagnostics;
using CertNET.Exceptions;

namespace CertNET.Execution;

/// <summary>
/// Default implementation of <see cref="ICertnetExecutor"/> that invokes certbot
/// as a child process using <see cref="System.Diagnostics.Process"/>.
/// </summary>
public class CertnetExecutor : ICertnetExecutor
{
    private string? _executablePath;
    private readonly CertnetClientOptions _options;
    private readonly bool _autoDownload;

    /// <summary>
    /// Creates a new <see cref="CertnetExecutor"/> with the specified options.
    /// <para>
    /// When <see cref="CertnetClientOptions.AutoDownload"/> is <c>true</c> and certbot
    /// is not found on the system, resolution is deferred to the first
    /// <see cref="ExecuteAsync"/> call, at which point certbot will be installed automatically
    /// into a Python virtual environment.
    /// </para>
    /// </summary>
    /// <param name="options">The client options used to locate and configure certbot.</param>
    public CertnetExecutor(CertnetClientOptions options)
    {
        _options = options;
        _autoDownload = options.AutoDownload;

        try
        {
            _executablePath = CertnetLocator.Locate(options.ExecutablePath);
        }
        catch (CertnetNotFoundException) when (_autoDownload)
        {
            // Defer resolution to first ExecuteAsync() — will auto-install
            _executablePath = null;
        }
    }

    /// <summary>
    /// Creates a new <see cref="CertnetExecutor"/> with an explicit executable path and optional timeout.
    /// </summary>
    /// <param name="executablePath">The full path to the certbot executable.</param>
    /// <param name="timeout">Optional timeout for the certbot process.</param>
    public CertnetExecutor(string executablePath, TimeSpan? timeout = null)
    {
        _executablePath = executablePath;
        _options = new CertnetClientOptions { Timeout = timeout };
    }

    /// <inheritdoc />
    public async Task<CertnetResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        // Lazy resolution: auto-install certbot if not yet located
        if (_executablePath is null)
        {
            _executablePath = await ToolDownloader.EnsureCertbotAsync(cancellationToken);
        }

        var argumentString = string.Join(' ', arguments.Select(EscapeArgument));

        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            Arguments = argumentString,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        return await ProcessRunner.RunAsync(
            startInfo,
            _options.Timeout,
            "certbot",
            _options.OutputDataReceived,
            _options.ErrorDataReceived,
            cancellationToken);
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
