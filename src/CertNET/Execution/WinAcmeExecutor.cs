using System.Diagnostics;
using System.Runtime.InteropServices;
using CertNET.Exceptions;

namespace CertNET.Execution;

/// <summary>
/// Executes ACME certificate operations using win-acme (<c>wacs.exe</c>) on Windows.
/// <para>
/// This executor receives certbot-style arguments from the fluent API and translates them
/// into the equivalent win-acme CLI arguments. This allows the same user-facing API to work
/// with either certbot (Linux/macOS) or win-acme (Windows).
/// </para>
/// <para>
/// The plugin must provide its win-acme-specific arguments via
/// <see cref="Plugins.ICertnetPlugin.GetWinAcmeArguments"/>.
/// </para>
/// </summary>
public class WinAcmeExecutor : ICertnetExecutor
{
    private readonly CertnetClientOptions _options;
    private readonly bool _autoDownload;
    private string? _wacsPath;

    /// <summary>
    /// The win-acme validation plugin name, set per-command based on the plugin.
    /// </summary>
    internal string? ValidationPluginName { get; set; }

    /// <summary>
    /// Additional win-acme-specific arguments from the plugin (e.g., <c>--cloudflareapitoken</c>).
    /// </summary>
    internal List<string> PluginArguments { get; } = [];

    /// <summary>
    /// Creates a new <see cref="WinAcmeExecutor"/>.
    /// <para>
    /// When <see cref="CertnetClientOptions.AutoDownload"/> is <c>true</c> and <c>wacs.exe</c>
    /// is not found on the system, resolution is deferred to the first
    /// <see cref="ExecuteAsync"/> call, at which point win-acme will be downloaded automatically.
    /// </para>
    /// </summary>
    /// <param name="options">The client options.</param>
    public WinAcmeExecutor(CertnetClientOptions options)
    {
        _options = options;
        _autoDownload = options.AutoDownload;

        try
        {
            _wacsPath = LocateWacs(options.ExecutablePath);
        }
        catch (CertnetNotFoundException) when (_autoDownload)
        {
            // Defer resolution to first ExecuteAsync() — will auto-download
            _wacsPath = null;
        }
    }

    /// <inheritdoc />
    public async Task<CertnetResult> ExecuteAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        // Lazy resolution: auto-download win-acme if not yet located
        if (_wacsPath is null)
        {
            _wacsPath = await ToolDownloader.EnsureWinAcmeAsync(cancellationToken);
        }

        var wacsArgs = TranslateCertbotToWinAcme(arguments);
        var argumentString = string.Join(' ', wacsArgs.Select(EscapeArgument));

        var startInfo = new ProcessStartInfo
        {
            FileName = _wacsPath,
            Arguments = argumentString,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        return await ProcessRunner.RunAsync(
            startInfo,
            _options.Timeout,
            "win-acme",
            _options.OutputDataReceived,
            _options.ErrorDataReceived,
            cancellationToken,
            afterStart: process =>
            {
                // Close stdin immediately so wacs.exe never blocks waiting for input.
                // We avoid the --test flag (which triggers Console.ReadKey() prompts)
                // and use --baseuri with the staging URL directly.
                process.StandardInput.Close();
            });
    }

    /// <summary>
    /// Translates certbot-style arguments into win-acme arguments.
    /// </summary>
    internal List<string> TranslateCertbotToWinAcme(IReadOnlyList<string> certbotArgs)
    {
        var args = new List<string>();
        var domains = new List<string>();
        string? subCommand = null;
        string? certName = null;
        string? email = null;
        string? keyType = null;
        string? pemOutputDir = null;
        bool agreeToS = false;
        bool nonInteractive = false;
        bool staging = false;
        bool forceRenewal = false;
        bool dryRun = false;
        bool deleteAfterRevoke = false;
        string? serverUrl = null;

        // Parse the certbot-style arguments
        for (var i = 0; i < certbotArgs.Count; i++)
        {
            var arg = certbotArgs[i];

            switch (arg)
            {
                case "certonly":
                case "renew":
                case "revoke":
                case "delete":
                case "certificates":
                    subCommand = arg;
                    break;

                case "-d":
                    if (i + 1 < certbotArgs.Count)
                        domains.Add(certbotArgs[++i]);
                    break;

                case "--cert-name":
                case "--friendlyname":
                    if (i + 1 < certbotArgs.Count)
                        certName = certbotArgs[++i];
                    break;

                case "--email":
                    if (i + 1 < certbotArgs.Count)
                        email = certbotArgs[++i];
                    break;

                case "--agree-tos":
                    agreeToS = true;
                    break;

                case "-n":
                    nonInteractive = true;
                    break;

                case "--key-type":
                    if (i + 1 < certbotArgs.Count)
                        keyType = certbotArgs[++i];
                    break;

                case "--force-renewal":
                    forceRenewal = true;
                    break;

                case "--dry-run":
                    dryRun = true;
                    break;

                case "--delete-after-revoke":
                    deleteAfterRevoke = true;
                    break;

                case "--server":
                    if (i + 1 < certbotArgs.Count)
                    {
                        serverUrl = certbotArgs[++i];
                        staging = serverUrl.Contains("staging", StringComparison.OrdinalIgnoreCase);
                    }
                    break;

                case "--cert-path":
                case "--key-path":
                case "--fullchain-path":
                case "--chain-path":
                    // Extract the directory from any of these paths for --pemfilespath
                    if (i + 1 < certbotArgs.Count)
                    {
                        var filePath = certbotArgs[++i];
                        var dir = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(dir))
                            pemOutputDir = dir;
                    }
                    break;

                // Skip certbot-specific flags that have no win-acme equivalent
                case "--authenticator":
                case "--rsa-key-size":
                case "--elliptic-curve":
                case "--preferred-chain":
                case "--config-dir":
                case "--work-dir":
                case "--logs-dir":
                case "--expand":
                case "--duplicate":
                case "--pre-hook":
                case "--post-hook":
                case "--deploy-hook":
                case "--reuse-key":
                case "--quiet":
                case "--http-01-port":
                case "--http-01-address":
                    // Skip the flag and its value (if it takes one)
                    if (i + 1 < certbotArgs.Count && !certbotArgs[i + 1].StartsWith("--") && !certbotArgs[i + 1].StartsWith("-"))
                        i++;
                    break;

                default:
                    // Skip unknown certbot-specific flags (e.g., --dns-cloudflare-credentials, --dns-cloudflare-propagation-seconds)
                    if (arg.StartsWith("--dns-") || arg.StartsWith("--manual-"))
                    {
                        // Skip the value too
                        if (i + 1 < certbotArgs.Count && !certbotArgs[i + 1].StartsWith("--"))
                            i++;
                    }
                    break;
            }
        }

        // Build win-acme arguments based on the parsed certbot command
        switch (subCommand)
        {
            case "certonly":
                BuildCertonlyArgs(args, domains, certName, email, keyType, pemOutputDir,
                    agreeToS, nonInteractive, staging, forceRenewal, dryRun, serverUrl);
                break;

            case "renew":
                BuildRenewArgs(args, certName, forceRenewal, nonInteractive, staging, dryRun, serverUrl);
                break;

            case "revoke":
                BuildRevokeArgs(args, certName, deleteAfterRevoke, nonInteractive, serverUrl);
                break;

            case "delete":
                BuildDeleteArgs(args, certName, nonInteractive);
                break;

            case "certificates":
                args.Add("--list");
                args.Add("--closeonfinish");
                break;
        }

        return args;
    }

    private void BuildCertonlyArgs(
        List<string> args,
        List<string> domains,
        string? certName,
        string? email,
        string? keyType,
        string? pemOutputDir,
        bool agreeToS,
        bool nonInteractive,
        bool staging,
        bool forceRenewal,
        bool dryRun,
        string? serverUrl)
    {
        // Source: manual (we provide the domains explicitly)
        args.Add("--source");
        args.Add("manual");

        // Domains
        if (domains.Count > 0)
        {
            args.Add("--host");
            args.Add(string.Join(",", domains));
        }

        // Friendly name
        if (!string.IsNullOrWhiteSpace(certName))
        {
            args.Add("--friendlyname");
            args.Add(certName);
        }

        // Validation plugin from the ICertnetPlugin
        if (!string.IsNullOrWhiteSpace(ValidationPluginName))
        {
            args.Add("--validation");
            args.Add(ValidationPluginName);
        }

        // Plugin-specific arguments (e.g., --cloudflareapitoken)
        args.AddRange(PluginArguments);

        // CSR / key type
        if (!string.IsNullOrWhiteSpace(keyType))
        {
            args.Add("--csr");
            args.Add(keyType switch
            {
                "ecdsa" => "ec",
                "rsa" => "rsa",
                _ => "ec"
            });
        }

        // Store: PEM files if an output directory was specified
        if (!string.IsNullOrWhiteSpace(pemOutputDir))
        {
            args.Add("--store");
            args.Add("pemfiles");
            args.Add("--pemfilespath");
            args.Add(pemOutputDir);
        }

        // Account
        if (!string.IsNullOrWhiteSpace(email))
        {
            args.Add("--emailaddress");
            args.Add(email);
        }

        if (agreeToS)
            args.Add("--accepttos");

        // Server — always use --baseuri with the explicit URL instead of --test.
        // The --test flag enables extra interactive prompts ("[--test] Store and install?",
        // "[--test] Quit?") that use Console.ReadKey() internally, which throws
        // InvalidOperationException when stdin is redirected. Using --baseuri with the
        // staging URL achieves the same ACME endpoint without the interactive prompts.
        if (!string.IsNullOrWhiteSpace(serverUrl))
        {
            args.Add("--baseuri");
            args.Add(serverUrl);
        }

        if (forceRenewal)
            args.Add("--force");

        // Prevent the "create scheduled task?" prompt
        args.Add("--notaskscheduler");

        // Ensure wacs exits after completing the operation
        args.Add("--closeonfinish");
    }

    private static void BuildRenewArgs(
        List<string> args,
        string? certName,
        bool forceRenewal,
        bool nonInteractive,
        bool staging,
        bool dryRun,
        string? serverUrl)
    {
        args.Add("--renew");

        if (!string.IsNullOrWhiteSpace(certName))
        {
            args.Add("--friendlyname");
            args.Add(certName);
        }

        if (forceRenewal)
            args.Add("--force");

        // Always use --baseuri instead of --test (see BuildCertonlyArgs comment)
        if (!string.IsNullOrWhiteSpace(serverUrl))
        {
            args.Add("--baseuri");
            args.Add(serverUrl);
        }

        args.Add("--closeonfinish");
    }

    private static void BuildRevokeArgs(
        List<string> args,
        string? certName,
        bool deleteAfterRevoke,
        bool nonInteractive,
        string? serverUrl)
    {
        args.Add("--revoke");

        if (!string.IsNullOrWhiteSpace(certName))
        {
            args.Add("--friendlyname");
            args.Add(certName);
        }

        if (deleteAfterRevoke)
        {
            args.Add("--cancel");
            if (!string.IsNullOrWhiteSpace(certName))
            {
                args.Add("--friendlyname");
                args.Add(certName);
            }
        }

        if (!string.IsNullOrWhiteSpace(serverUrl) && !serverUrl.Contains("staging", StringComparison.OrdinalIgnoreCase))
        {
            args.Add("--baseuri");
            args.Add(serverUrl);
        }

        args.Add("--closeonfinish");
    }

    private static void BuildDeleteArgs(
        List<string> args,
        string? certName,
        bool nonInteractive)
    {
        args.Add("--cancel");

        if (!string.IsNullOrWhiteSpace(certName))
        {
            args.Add("--friendlyname");
            args.Add(certName);
        }

        args.Add("--closeonfinish");
    }

    private static string LocateWacs(string? customPath)
    {
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
            return customPath;

        // Try to find wacs.exe on PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
            foreach (var dir in pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries))
            {
                var fullPath = Path.Combine(dir.Trim(), "wacs.exe");
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }

        // Check well-known win-acme install locations on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string[] knownPaths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "win-acme", "wacs.exe"),
                @"C:\win-acme\wacs.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "win-acme", "wacs.exe"),
                // CertNET auto-download location
                ToolDownloader.GetWinAcmeExecutablePath(),
            ];

            foreach (var path in knownPaths)
            {
                if (File.Exists(path))
                    return path;
            }
        }

        throw new CertnetNotFoundException(
            "win-acme (wacs.exe) was not found on the system PATH or in common install locations. " +
            "Please install win-acme: https://www.win-acme.com/, specify the path via CertnetClientOptions.ExecutablePath, " +
            "or set CertnetClientOptions.AutoDownload = true to download it automatically.");
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
