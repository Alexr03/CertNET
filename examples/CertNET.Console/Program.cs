using System.Runtime.InteropServices;
using CertNET;
using CertNET.Exceptions;
using CertNET.Plugins.Cloudflare;

// ── Detect platform ──────────────────────────────────────────────────

var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
if (isWindows)
{
    Console.WriteLine("Windows detected - will use win-acme (wacs.exe) for certificate management.");
    Console.WriteLine("Make sure win-acme is installed: https://www.win-acme.com/");
    Console.WriteLine();
}

// ── Gather configuration ─────────────────────────────────────────────

var domain = GetRequiredInput("Domain (e.g. example.com)");
var additionalDomains = GetOptionalInput("Additional domains (comma-separated, e.g. *.example.com,www.example.com)");
var apiToken = GetRequiredInput("Cloudflare API Token");
var email = GetRequiredInput("Email for Let's Encrypt registration");

var propagationStr = GetOptionalInput("DNS propagation wait seconds (default: 30)");
var propagationSeconds = int.TryParse(propagationStr, out var ps) ? ps : 30;

var outputDir = GetOptionalInput("Certificate output directory (leave blank for default certbot location)");

var useStaging = Confirm("Use Let's Encrypt staging server? (recommended for testing)");

// ── Build domain list ────────────────────────────────────────────────

var domains = new List<string> { domain };
if (!string.IsNullOrWhiteSpace(additionalDomains))
{
    domains.AddRange(
        additionalDomains.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

// ── Display summary ──────────────────────────────────────────────────

Console.WriteLine();
Console.WriteLine("=== Certificate Request Summary ===");
Console.WriteLine($"  Domains:     {string.Join(", ", domains)}");
Console.WriteLine($"  Server:      {(useStaging ? "Staging" : "Production")}");
Console.WriteLine($"  Email:       {email}");
Console.WriteLine($"  Propagation: {propagationSeconds}s");
Console.WriteLine($"  Output:      {(string.IsNullOrWhiteSpace(outputDir) ? "(default certbot location)" : outputDir)}");
Console.WriteLine($"  Execution:   {(isWindows ? "win-acme" : "Native certbot")}");
Console.WriteLine();

if (!Confirm("Proceed with certificate request?"))
{
    Console.WriteLine("Aborted.");
    return 1;
}

// ── Execute ──────────────────────────────────────────────────────────

var client = new CertnetClient(options =>
{
    options.Server = useStaging ? AcmeServer.Staging : AcmeServer.Production;
    options.Email = email;
    options.AgreeToTermsOfService = true;
    options.AutoDownload = true;
    options.ExecutionMode = ExecutionMode.Auto;
    // ExecutionMode.Auto will pick win-acme on Windows, Native certbot on Linux/macOS

    // Stream process output to the console in real time
    options.OutputDataReceived = line => Console.WriteLine($"  {line}");
    options.ErrorDataReceived = line =>
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Error.WriteLine($"  {line}");
        Console.ResetColor();
    };
});

try
{
    Console.WriteLine();
    Console.WriteLine(isWindows
        ? "Running win-acme with Cloudflare DNS validation..."
        : "Requesting certificate via Cloudflare DNS...");

    var command = client.Certonly()
        .ForDomains(domains.ToArray())
        .WithKeyType(KeyType.Ecdsa)
        .WithPlugin(new CloudflarePlugin
        {
            ApiToken = apiToken,
            PropagationSeconds = propagationSeconds
        });

    if (!string.IsNullOrWhiteSpace(outputDir))
    {
        // Ensure the output directory exists
        Directory.CreateDirectory(outputDir);
        command.WithOutputDirectory(outputDir);
    }

    var result = await command.ExecuteAsync();

    Console.WriteLine();
    Console.WriteLine("Certificate obtained successfully!");

    if (result.Certificate is not null)
    {
        Console.WriteLine();
        if (result.Certificate.OutputDirectory is not null)
            Console.WriteLine($"  Output directory: {result.Certificate.OutputDirectory}");
        if (result.Certificate.CertificatePath is not null)
            Console.WriteLine($"  Certificate:      {result.Certificate.CertificatePath}");
        if (result.Certificate.PrivateKeyPath is not null)
            Console.WriteLine($"  Private key:      {result.Certificate.PrivateKeyPath}");
        if (result.Certificate.FullChainPath is not null)
            Console.WriteLine($"  Full chain:       {result.Certificate.FullChainPath}");
        if (result.Certificate.ChainPath is not null)
            Console.WriteLine($"  Chain:            {result.Certificate.ChainPath}");

        if (result.Certificate.HasAllFiles)
            Console.WriteLine($"\n  All certificate files are present on disk.");
        else
            Console.WriteLine($"\n  Note: Some certificate files may not be on disk yet (e.g., staging/test run).");
    }

    return 0;
}
catch (CertnetNotFoundException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    if (isWindows)
        Console.Error.WriteLine("Please install win-acme: https://www.win-acme.com/");
    else
        Console.Error.WriteLine("Please install certbot: https://certbot.eff.org/instructions");
    return 2;
}
catch (CertnetExecutionException ex)
{
    Console.Error.WriteLine($"Error: command failed (exit code {ex.Result.ExitCode})");
    Console.Error.WriteLine();
    Console.Error.WriteLine("--- stdout ---");
    Console.Error.WriteLine(ex.Result.StandardOutput);
    Console.Error.WriteLine("--- stderr ---");
    Console.Error.WriteLine(ex.Result.StandardError);
    Console.Error.WriteLine();
    Console.Error.WriteLine($"Command: {ex.Result.CommandLine}");
    return 3;
}
catch (PluginValidationException ex)
{
    Console.Error.WriteLine($"Configuration error: {ex.Message}");
    return 4;
}

// ── Helpers ──────────────────────────────────────────────────────────

static string GetRequiredInput(string prompt)
{
    string? value;
    do
    {
        Console.Write($"{prompt}: ");
        value = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            Console.WriteLine("  This field is required.");
    } while (string.IsNullOrWhiteSpace(value));

    return value;
}

static string? GetOptionalInput(string prompt)
{
    Console.Write($"{prompt}: ");
    return Console.ReadLine()?.Trim();
}

static bool Confirm(string prompt)
{
    Console.Write($"{prompt} [y/N]: ");
    var answer = Console.ReadLine()?.Trim();
    return string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase)
        || string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase);
}
