using System.Diagnostics;
using System.Runtime.InteropServices;
using CertNET.Execution;

namespace CertNET.Tests;

public class ProcessRunnerTests
{
    // ── OutputDataReceived / ErrorDataReceived on options ────────────

    [Fact]
    public void Options_OutputDataReceived_DefaultsToNull()
    {
        var options = new CertnetClientOptions();
        Assert.Null(options.OutputDataReceived);
    }

    [Fact]
    public void Options_ErrorDataReceived_DefaultsToNull()
    {
        var options = new CertnetClientOptions();
        Assert.Null(options.ErrorDataReceived);
    }

    [Fact]
    public void Options_OutputDataReceived_CanBeSet()
    {
        var captured = new List<string>();
        var options = new CertnetClientOptions
        {
            OutputDataReceived = line => captured.Add(line)
        };
        options.OutputDataReceived("test line");
        Assert.Single(captured);
        Assert.Equal("test line", captured[0]);
    }

    [Fact]
    public void Options_ErrorDataReceived_CanBeSet()
    {
        var captured = new List<string>();
        var options = new CertnetClientOptions
        {
            ErrorDataReceived = line => captured.Add(line)
        };
        options.ErrorDataReceived("error line");
        Assert.Single(captured);
        Assert.Equal("error line", captured[0]);
    }

    // ── ProcessRunner streams output line-by-line ───────────────────

    [Fact]
    public async Task RunAsync_StreamsStdoutLinesToCallback()
    {
        var capturedLines = new List<string>();

        // Use a simple echo command that produces known output
        var (fileName, arguments) = GetEchoCommand("line1\nline2\nline3");

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var result = await ProcessRunner.RunAsync(
            startInfo,
            timeout: TimeSpan.FromSeconds(10),
            toolDisplayName: "test",
            onOutputLine: line => capturedLines.Add(line),
            onErrorLine: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("line1", capturedLines);
        Assert.Contains("line2", capturedLines);
        Assert.Contains("line3", capturedLines);
        // Full output is also accumulated in the result
        Assert.Contains("line1", result.StandardOutput);
        Assert.Contains("line3", result.StandardOutput);
    }

    [Fact]
    public async Task RunAsync_StdoutAccumulatedEvenWithoutCallback()
    {
        var (fileName, arguments) = GetEchoCommand("hello world");

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var result = await ProcessRunner.RunAsync(
            startInfo,
            timeout: TimeSpan.FromSeconds(10),
            toolDisplayName: "test",
            onOutputLine: null,
            onErrorLine: null,
            cancellationToken: CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello world", result.StandardOutput);
    }

    [Fact]
    public async Task RunAsync_StreamsStderrLinesToCallback()
    {
        var capturedErrors = new List<string>();

        // Write to stderr
        var (fileName, arguments) = GetStderrCommand("error output");

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var result = await ProcessRunner.RunAsync(
            startInfo,
            timeout: TimeSpan.FromSeconds(10),
            toolDisplayName: "test",
            onOutputLine: null,
            onErrorLine: line => capturedErrors.Add(line),
            cancellationToken: CancellationToken.None);

        Assert.Contains("error output", capturedErrors);
        Assert.Contains("error output", result.StandardError);
    }

    [Fact]
    public async Task RunAsync_AfterStartCallback_IsInvoked()
    {
        var afterStartCalled = false;
        var (fileName, arguments) = GetEchoCommand("test");

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        await ProcessRunner.RunAsync(
            startInfo,
            timeout: TimeSpan.FromSeconds(10),
            toolDisplayName: "test",
            onOutputLine: null,
            onErrorLine: null,
            cancellationToken: CancellationToken.None,
            afterStart: _ => afterStartCalled = true);

        Assert.True(afterStartCalled);
    }

    [Fact]
    public async Task RunAsync_CommandLine_IncludesFileNameAndArgs()
    {
        var (fileName, arguments) = GetEchoCommand("test");

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var result = await ProcessRunner.RunAsync(
            startInfo,
            timeout: null,
            toolDisplayName: "test",
            onOutputLine: null,
            onErrorLine: null,
            cancellationToken: CancellationToken.None);

        Assert.Contains(fileName, result.CommandLine);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a platform-appropriate command that echoes text to stdout.
    /// </summary>
    private static (string FileName, string Arguments) GetEchoCommand(string text)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Use PowerShell for multiline support
            var escaped = text.Replace("\"", "`\"");
            return ("powershell", $"-NoProfile -Command \"Write-Host '{escaped}'\"");
        }

        return ("echo", $"\"{text}\"");
    }

    /// <summary>
    /// Returns a platform-appropriate command that writes text to stderr.
    /// </summary>
    private static (string FileName, string Arguments) GetStderrCommand(string text)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ("powershell", $"-NoProfile -Command \"[Console]::Error.WriteLine('{text}')\"");

        return ("bash", $"-c \"echo '{text}' >&2\"");
    }
}
