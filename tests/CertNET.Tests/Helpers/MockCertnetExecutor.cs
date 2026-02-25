using CertNET.Execution;

namespace CertNET.Tests.Helpers;

/// <summary>
/// A mock executor that captures the arguments passed to certbot
/// instead of actually running the certbot process.
/// </summary>
public class MockCertnetExecutor : ICertnetExecutor
{
    /// <summary>
    /// The arguments captured from the last execution.
    /// </summary>
    public IReadOnlyList<string>? CapturedArguments { get; private set; }

    /// <summary>
    /// All argument lists captured across multiple executions.
    /// </summary>
    public List<IReadOnlyList<string>> AllCapturedArguments { get; } = [];

    /// <summary>
    /// The result to return from ExecuteAsync. Defaults to a successful result.
    /// </summary>
    public CertnetResult ResultToReturn { get; set; } = new()
    {
        ExitCode = 0,
        StandardOutput = "",
        StandardError = "",
        CommandLine = "certbot (mock)"
    };

    public Task<CertnetResult> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        CapturedArguments = arguments;
        AllCapturedArguments.Add(arguments);
        return Task.FromResult(ResultToReturn);
    }
}
