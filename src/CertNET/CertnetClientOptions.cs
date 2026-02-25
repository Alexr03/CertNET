namespace CertNET;

/// <summary>
/// Global configuration options for the <see cref="CertnetClient"/>.
/// These options are applied to every certbot command executed through the client.
/// </summary>
public class CertnetClientOptions
{
    /// <summary>
    /// How certbot should be executed. Defaults to <see cref="CertNET.ExecutionMode.Auto"/>,
    /// which uses Docker on Windows and native execution on Linux/macOS.
    /// </summary>
    public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.Auto;

    /// <summary>
    /// Custom path to the certbot (or docker) executable. When <c>null</c>, the library will
    /// attempt to auto-detect the location on the system PATH.
    /// <para>
    /// In <see cref="CertNET.ExecutionMode.Native"/> mode, this is the path to the certbot binary.
    /// In <see cref="CertNET.ExecutionMode.Docker"/> mode, this is the path to the docker binary.
    /// </para>
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Configuration directory for certbot. Maps to <c>--config-dir</c>.
    /// Default: <c>/etc/letsencrypt</c> on Linux/macOS.
    /// </summary>
    public string? ConfigDirectory { get; set; }

    /// <summary>
    /// Working directory for certbot. Maps to <c>--work-dir</c>.
    /// Default: <c>/var/lib/letsencrypt</c> on Linux/macOS.
    /// </summary>
    public string? WorkDirectory { get; set; }

    /// <summary>
    /// Logs directory for certbot. Maps to <c>--logs-dir</c>.
    /// Default: <c>/var/log/letsencrypt</c> on Linux/macOS.
    /// </summary>
    public string? LogsDirectory { get; set; }

    /// <summary>
    /// Which ACME server to use. Defaults to <see cref="AcmeServer.Production"/>.
    /// Use <see cref="AcmeServer.Staging"/> for testing to avoid rate limits.
    /// </summary>
    public AcmeServer Server { get; set; } = AcmeServer.Production;

    /// <summary>
    /// Email address for ACME account registration and certificate expiry notifications.
    /// Maps to <c>--email</c>.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Whether to automatically agree to the ACME subscriber agreement.
    /// Maps to <c>--agree-tos</c>. Defaults to <c>false</c>.
    /// </summary>
    public bool AgreeToTermsOfService { get; set; }

    /// <summary>
    /// Whether to run certbot in non-interactive mode. Maps to <c>-n</c>.
    /// Defaults to <c>true</c> since this library is designed for programmatic use.
    /// </summary>
    public bool NonInteractive { get; set; } = true;

    /// <summary>
    /// When <c>true</c> and the required ACME tool (certbot or win-acme) is not found
    /// on the system, automatically download and install it to a user-local directory.
    /// <para>
    /// <b>Install locations:</b>
    /// <list type="bullet">
    ///   <item><b>Windows:</b> <c>%LOCALAPPDATA%\certnet\tools\win-acme\wacs.exe</c></item>
    ///   <item><b>Linux/macOS:</b> <c>~/.local/share/certnet/tools/certbot/bin/certbot</c></item>
    /// </list>
    /// No administrator/root privileges are required.
    /// </para>
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool AutoDownload { get; set; }

    /// <summary>
    /// Maximum time to wait for the certbot process to complete.
    /// If <c>null</c>, no timeout is applied.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Optional callback invoked for each line written to standard output by the underlying
    /// ACME tool (certbot, wacs.exe, or docker) as it executes. Use this to display real-time
    /// progress in your application.
    /// <para>
    /// <b>Example:</b>
    /// <code>
    /// options.OutputDataReceived = line => Console.WriteLine($"  {line}");
    /// </code>
    /// </para>
    /// When <c>null</c>, output is still captured and returned in <see cref="Execution.CertnetResult.StandardOutput"/>.
    /// </summary>
    public Action<string>? OutputDataReceived { get; set; }

    /// <summary>
    /// Optional callback invoked for each line written to standard error by the underlying
    /// ACME tool. Use this to display warnings and errors in real time.
    /// <para>
    /// When <c>null</c>, stderr is still captured and returned in <see cref="Execution.CertnetResult.StandardError"/>.
    /// </para>
    /// </summary>
    public Action<string>? ErrorDataReceived { get; set; }

    internal string GetServerUrl() => Server switch
    {
        AcmeServer.Production => "https://acme-v02.api.letsencrypt.org/directory",
        AcmeServer.Staging => "https://acme-staging-v02.api.letsencrypt.org/directory",
        _ => throw new ArgumentOutOfRangeException(nameof(Server), Server, "Unknown ACME server.")
    };
}
