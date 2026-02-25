using System.Diagnostics;
using System.Text;
using CertNET.Exceptions;

namespace CertNET.Execution;

/// <summary>
/// Shared helper that runs a process with line-by-line output streaming.
/// All three executors (<see cref="CertnetExecutor"/>, <see cref="WinAcmeExecutor"/>,
/// <see cref="DockerCertnetExecutor"/>) delegate to this class so that the
/// <see cref="CertnetClientOptions.OutputDataReceived"/> and
/// <see cref="CertnetClientOptions.ErrorDataReceived"/> callbacks are invoked
/// consistently.
/// </summary>
internal static class ProcessRunner
{
    /// <summary>
    /// Runs a process, streaming stdout/stderr line-by-line to the provided callbacks
    /// while accumulating the full output for the returned <see cref="CertnetResult"/>.
    /// </summary>
    /// <param name="startInfo">The configured <see cref="ProcessStartInfo"/>. Must have
    /// <c>RedirectStandardOutput</c> and <c>RedirectStandardError</c> set to <c>true</c>.</param>
    /// <param name="timeout">Optional process timeout.</param>
    /// <param name="toolDisplayName">Display name for error messages (e.g., "certbot", "win-acme", "Docker certbot").</param>
    /// <param name="onOutputLine">Callback for each stdout line, or <c>null</c>.</param>
    /// <param name="onErrorLine">Callback for each stderr line, or <c>null</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="afterStart">Optional action to run immediately after the process starts
    /// (e.g., closing stdin for win-acme).</param>
    /// <returns>The captured result including full stdout/stderr text.</returns>
    internal static async Task<CertnetResult> RunAsync(
        ProcessStartInfo startInfo,
        TimeSpan? timeout,
        string toolDisplayName,
        Action<string>? onOutputLine,
        Action<string>? onErrorLine,
        CancellationToken cancellationToken,
        Action<Process>? afterStart = null)
    {
        var commandLine = $"{startInfo.FileName} {startInfo.Arguments}";

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Allow callers to perform post-start actions (e.g., close stdin for win-acme)
        afterStart?.Invoke(process);

        // Read stdout and stderr line-by-line concurrently
        var stdoutTask = ReadStreamAsync(process.StandardOutput, onOutputLine, cancellationToken);
        var stderrTask = ReadStreamAsync(process.StandardError, onErrorLine, cancellationToken);

        if (timeout.HasValue)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout.Value);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKillProcess(process);
                throw new CertnetExecutionException(
                    $"{toolDisplayName} process timed out after {timeout.Value.TotalSeconds} seconds.",
                    new CertnetResult
                    {
                        ExitCode = -1,
                        StandardOutput = "",
                        StandardError = $"Process timed out after {timeout.Value.TotalSeconds} seconds.",
                        CommandLine = commandLine
                    });
            }
        }
        else
        {
            await process.WaitForExitAsync(cancellationToken);
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new CertnetResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdout,
            StandardError = stderr,
            CommandLine = commandLine
        };
    }

    /// <summary>
    /// Reads a stream line-by-line, invoking the callback for each non-null line
    /// and accumulating the full text in a <see cref="StringBuilder"/>.
    /// </summary>
    private static async Task<string> ReadStreamAsync(
        StreamReader reader,
        Action<string>? onLine,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                break;

            onLine?.Invoke(line);

            if (sb.Length > 0)
                sb.AppendLine();
            sb.Append(line);
        }

        return sb.ToString();
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}
