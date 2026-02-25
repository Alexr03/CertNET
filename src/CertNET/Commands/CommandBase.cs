using CertNET.Exceptions;
using CertNET.Execution;
using CertNET.Plugins;

namespace CertNET.Commands;

/// <summary>
/// Base class for all certbot command builders. Provides shared argument accumulation,
/// global option application, and plugin lifecycle management.
/// </summary>
public abstract class CommandBase
{
    private readonly ICertnetExecutor _executor;
    private readonly CertnetClientOptions _options;

    /// <summary>
    /// The accumulated command-line arguments for this command.
    /// </summary>
    protected List<string> Arguments { get; } = [];

    /// <summary>
    /// The plugin to use for this command, if any.
    /// </summary>
    protected ICertnetPlugin? Plugin { get; set; }

    /// <summary>
    /// Creates a new command builder with the specified executor and options.
    /// </summary>
    protected CommandBase(ICertnetExecutor executor, CertnetClientOptions options)
    {
        _executor = executor;
        _options = options;
    }

    /// <summary>
    /// The certbot subcommand name (e.g., <c>"certonly"</c>, <c>"renew"</c>, <c>"revoke"</c>).
    /// </summary>
    protected abstract string SubCommand { get; }

    /// <summary>
    /// Builds the complete argument list and executes the command.
    /// Handles DNS plugin credential file lifecycle (create before, cleanup after),
    /// Docker executor configuration (image selection, volume mounts),
    /// win-acme executor configuration (validation plugin, direct credentials),
    /// and auto-downloading of DNS plugin pip packages when <see cref="CertnetClientOptions.AutoDownload"/> is enabled.
    /// </summary>
    protected async Task<CertnetResult> ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        // Validate plugin configuration before execution
        Plugin?.Validate();

        // Auto-install DNS plugin pip package into the certbot venv if needed.
        // Only applies to native certbot — Docker bundles plugins in images,
        // and win-acme includes plugins in its pluggable distribution.
        if (_options.AutoDownload
            && _executor is CertnetExecutor
            && Plugin?.CertbotPipPackage is not null)
        {
            await ToolDownloader.EnsureCertbotDnsPluginAsync(
                Plugin.CertbotPipPackage, cancellationToken);
        }

        // Auto-download win-acme DNS plugin DLLs if needed.
        // Win-acme DNS plugins (Cloudflare, Azure, etc.) are separate zip downloads
        // from GitHub Releases that must be extracted alongside wacs.exe.
        if (_options.AutoDownload
            && _executor is WinAcmeExecutor
            && Plugin?.WinAcmePluginId is not null)
        {
            await ToolDownloader.EnsureWinAcmePluginAsync(
                Plugin.WinAcmePluginId, cancellationToken);
        }

        // For WinAcmeExecutor, skip the credential file lifecycle entirely —
        // win-acme accepts credentials directly as CLI arguments.
        if (_executor is WinAcmeExecutor winAcmeExecutor)
        {
            ConfigureWinAcmeExecutor(winAcmeExecutor);

            var allArguments = BuildFullArgumentList();
            var result = await _executor.ExecuteAsync(allArguments, cancellationToken);

            if (!result.Success)
            {
                throw new CertnetExecutionException(
                    $"win-acme {SubCommand} failed with exit code {result.ExitCode}: {result.StandardError}",
                    result);
            }

            return result;
        }

        // For certbot-based executors (Native, Docker), use credential file lifecycle
        var dnsPlugin = Plugin as IDnsPlugin;

        try
        {
            // Write temporary credentials file for DNS plugins
            string? credentialsHostPath = null;
            if (dnsPlugin is not null)
                credentialsHostPath = dnsPlugin.WriteCredentialsFile();

            // Configure Docker executor if applicable
            ConfigureDockerExecutor(credentialsHostPath);

            var allArguments = BuildFullArgumentList();
            var result = await _executor.ExecuteAsync(allArguments, cancellationToken);

            if (!result.Success)
            {
                throw new CertnetExecutionException(
                    $"certbot {SubCommand} failed with exit code {result.ExitCode}: {result.StandardError}",
                    result);
            }

            return result;
        }
        finally
        {
            // Always clean up temporary credentials files
            dnsPlugin?.CleanupCredentialsFile();
        }
    }

    /// <summary>
    /// Configures the <see cref="WinAcmeExecutor"/> with the plugin's validation name
    /// and win-acme-specific arguments.
    /// </summary>
    private void ConfigureWinAcmeExecutor(WinAcmeExecutor winAcmeExecutor)
    {
        winAcmeExecutor.ValidationPluginName = Plugin?.WinAcmeValidationName;
        winAcmeExecutor.PluginArguments.Clear();

        if (Plugin is not null)
            winAcmeExecutor.PluginArguments.AddRange(Plugin.GetWinAcmeArguments());
    }

    /// <summary>
    /// If the executor is a <see cref="DockerCertnetExecutor"/>, configure it with
    /// the correct Docker image, credential file volume mount, and output directory mount.
    /// </summary>
    private void ConfigureDockerExecutor(string? credentialsHostPath)
    {
        if (_executor is not DockerCertnetExecutor dockerExecutor)
            return;

        // Set the Docker image based on the plugin
        dockerExecutor.DockerImageOverride = Plugin?.DockerImage;

        // Clear any previous mappings from a prior command
        dockerExecutor.PathMappings.Clear();
        dockerExecutor.WritableVolumeMounts.Clear();

        // Map the credential file from host path to container path
        if (credentialsHostPath is not null)
        {
            var containerPath = $"/tmp/certnet/{Path.GetFileName(credentialsHostPath)}";
            dockerExecutor.PathMappings[credentialsHostPath] = containerPath;
        }

        // Mount the output directory for certificate files (writable)
        if (this is CertonlyCommand { OutputDirectory: not null } certonlyCmd)
        {
            var hostDir = certonlyCmd.OutputDirectory;
            const string containerDir = "/tmp/certnet/output";

            dockerExecutor.WritableVolumeMounts[hostDir] = containerDir;

            // Map individual file paths so certbot arguments get rewritten
            string[] certFiles = ["cert.pem", "privkey.pem", "fullchain.pem", "chain.pem"];
            foreach (var file in certFiles)
            {
                dockerExecutor.PathMappings[Path.Combine(hostDir, file)] =
                    $"{containerDir}/{file}";
            }
        }
    }

    private List<string> BuildFullArgumentList()
    {
        var args = new List<string>();

        // Subcommand first
        args.Add(SubCommand);

        // Global options from CertnetClientOptions
        ApplyGlobalOptions(args);

        // Plugin authenticator flag and arguments
        if (Plugin is not null)
        {
            args.Add("--authenticator");
            args.Add(Plugin.AuthenticatorName);

            args.AddRange(Plugin.GetArguments());
        }

        // Command-specific arguments
        args.AddRange(Arguments);

        return args;
    }

    private void ApplyGlobalOptions(List<string> args)
    {
        if (_options.NonInteractive)
            args.Add("-n");

        if (_options.AgreeToTermsOfService)
            args.Add("--agree-tos");

        if (!string.IsNullOrWhiteSpace(_options.Email))
        {
            args.Add("--email");
            args.Add(_options.Email);
        }

        // Always specify the server explicitly
        args.Add("--server");
        args.Add(_options.GetServerUrl());

        if (!string.IsNullOrWhiteSpace(_options.ConfigDirectory))
        {
            args.Add("--config-dir");
            args.Add(_options.ConfigDirectory);
        }

        if (!string.IsNullOrWhiteSpace(_options.WorkDirectory))
        {
            args.Add("--work-dir");
            args.Add(_options.WorkDirectory);
        }

        if (!string.IsNullOrWhiteSpace(_options.LogsDirectory))
        {
            args.Add("--logs-dir");
            args.Add(_options.LogsDirectory);
        }
    }

    /// <summary>
    /// Returns a snapshot of the arguments that would be passed to certbot.
    /// Useful for testing and debugging.
    /// </summary>
    internal IReadOnlyList<string> BuildArgumentsForTesting()
    {
        // For DNS plugins, we need to simulate the credential file path
        if (Plugin is IDnsPlugin dnsPlugin)
        {
            dnsPlugin.WriteCredentialsFile();
            try
            {
                return BuildFullArgumentList();
            }
            finally
            {
                dnsPlugin.CleanupCredentialsFile();
            }
        }

        return BuildFullArgumentList();
    }
}
