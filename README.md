# CertNET

A fluent .NET 10 library for automated Let's Encrypt certificate management. Wraps **certbot** (Linux/macOS), **win-acme** (Windows), and **Docker certbot** behind a single, strongly-typed API.

```csharp
var client = new CertnetClient(options =>
{
    options.Server = AcmeServer.Staging;
    options.Email = "admin@example.com";
    options.AgreeToTermsOfService = true;
    options.AutoDownload = true;
});

var result = await client.Certonly()
    .ForDomains("example.com", "*.example.com")
    .WithKeyType(KeyType.Ecdsa)
    .WithPlugin(new CloudflarePlugin { ApiToken = "cf-token-xxx" })
    .WithOutputDirectory("/etc/certs")
    .ExecuteAsync();
```

---

## Features

- **Fluent builder API** for `certonly`, `renew`, `revoke`, `delete`, and `certificates`
- **Cross-platform** — auto-selects win-acme on Windows, native certbot on Linux/macOS
- **Three execution backends** — Native, Docker, and WinAcme
- **Plugin architecture** — built-in HTTP/webroot/manual plugins, DNS plugins ship as separate packages
- **Auto-download** — optionally downloads and installs certbot or win-acme if not found
- **Real-time output streaming** — `OutputDataReceived` / `ErrorDataReceived` callbacks
- **Credential management** — plugins accept credentials as properties; temp files are created and cleaned up automatically
- **Fully async** with cancellation token support

## Packages

| Package | Description |
|---|---|
| `CertNET` | Core library — client, commands, execution engines, built-in plugins |
| `CertNET.Plugins.Cloudflare` | Cloudflare DNS-01 plugin for wildcard and DNS-validated certificates |

## Requirements

- .NET 10+
- One of the following ACME clients (or set `AutoDownload = true`):
  - **Windows:** [win-acme](https://www.win-acme.com/) (`wacs.exe`)
  - **Linux/macOS:** [certbot](https://certbot.eff.org/)
  - **Any platform:** [Docker](https://www.docker.com/) with certbot images

---

## Quick Start

### Install

```bash
dotnet add package CertNET
dotnet add package CertNET.Plugins.Cloudflare  # if using Cloudflare DNS
```

### Obtain a Certificate

```csharp
using CertNET;
using CertNET.Plugins.Cloudflare;

var client = new CertnetClient(options =>
{
    options.Server = AcmeServer.Staging;   // use Production for real certs
    options.Email = "admin@example.com";
    options.AgreeToTermsOfService = true;
    options.AutoDownload = true;           // download certbot/win-acme if missing

    // Stream tool output to the console in real time
    options.OutputDataReceived = line => Console.WriteLine($"  {line}");
    options.ErrorDataReceived = line => Console.Error.WriteLine($"  {line}");
});

var result = await client.Certonly()
    .ForDomains("example.com", "*.example.com")
    .WithKeyType(KeyType.Ecdsa)
    .WithPlugin(new CloudflarePlugin
    {
        ApiToken = "your-cloudflare-api-token",
        PropagationSeconds = 30
    })
    .WithOutputDirectory("/etc/certs")
    .ExecuteAsync();

// Certificate files are available on the result
if (result.Certificate is { HasAllFiles: true })
{
    Console.WriteLine($"Cert:      {result.Certificate.CertificatePath}");
    Console.WriteLine($"Key:       {result.Certificate.PrivateKeyPath}");
    Console.WriteLine($"Full chain:{result.Certificate.FullChainPath}");

    // PEM contents are also loaded when files exist on disk
    var pem = result.Certificate.FullChain;
}
```

### Renew All Certificates

```csharp
await client.RenewAllAsync();
```

### Renew a Specific Certificate

```csharp
await client.Renew()
    .ForCertificate("example.com")
    .ForceRenewal()
    .ExecuteAsync();
```

### Revoke a Certificate

```csharp
await client.Revoke()
    .ForCertificate("example.com")
    .WithReason(RevocationReason.KeyCompromise)
    .DeleteAfterRevoke()
    .ExecuteAsync();
```

### List Certificates

```csharp
var certs = await client.Certificates().ExecuteAsync();

foreach (var cert in certs.Certificates)
{
    Console.WriteLine($"{cert.CertificateName} — expires {cert.ExpiryDate:d} — {(cert.IsValid ? "valid" : "EXPIRED")}");
}
```

---

## Configuration

All options are set via `CertnetClientOptions`:

| Property | Type | Default | Description |
|---|---|---|---|
| `ExecutionMode` | `ExecutionMode` | `Auto` | `Auto`, `Native`, `Docker`, or `WinAcme` |
| `Server` | `AcmeServer` | `Production` | `Production` or `Staging` |
| `Email` | `string?` | `null` | ACME account email |
| `AgreeToTermsOfService` | `bool` | `false` | Accept the subscriber agreement |
| `NonInteractive` | `bool` | `true` | Run without interactive prompts |
| `AutoDownload` | `bool` | `false` | Download certbot/win-acme if not found |
| `Timeout` | `TimeSpan?` | `null` | Process timeout |
| `ExecutablePath` | `string?` | `null` | Custom path to certbot, wacs.exe, or docker |
| `ConfigDirectory` | `string?` | `null` | Override `--config-dir` |
| `WorkDirectory` | `string?` | `null` | Override `--work-dir` |
| `LogsDirectory` | `string?` | `null` | Override `--logs-dir` |
| `OutputDataReceived` | `Action<string>?` | `null` | Callback for each stdout line |
| `ErrorDataReceived` | `Action<string>?` | `null` | Callback for each stderr line |

### Execution Modes

| Mode | Backend | Platform | Notes |
|---|---|---|---|
| `Auto` | — | Any | Picks `WinAcme` on Windows, `Native` on Linux/macOS |
| `Native` | certbot | Linux/macOS | Runs certbot directly |
| `WinAcme` | wacs.exe | Windows | Translates certbot-style args to win-acme CLI |
| `Docker` | docker | Any | Runs `docker run --rm certbot/<image> ...` |

---

## Plugins

### Built-in Plugins

These ship with the core `CertNET` package:

| Plugin | Authenticator | Challenge | Description |
|---|---|---|---|
| `StandalonePlugin` | `standalone` | HTTP-01 | Built-in HTTP server on port 80 |
| `WebrootPlugin` | `webroot` | HTTP-01 | Places files in an existing web root |
| `ManualPlugin` | `manual` | HTTP-01 / DNS-01 | Manual challenge completion with hooks |
| `NginxPlugin` | `nginx` | HTTP-01 | Nginx integration (Linux/macOS only) |
| `ApachePlugin` | `apache` | HTTP-01 | Apache integration (Linux/macOS only) |

```csharp
// Generic constraint — plugin must have a parameterless constructor
client.Certonly()
    .ForDomain("example.com")
    .WithPlugin<StandalonePlugin>()
    .ExecuteAsync();

// Or pass a configured instance
client.Certonly()
    .ForDomain("example.com")
    .WithPlugin(new WebrootPlugin { WebRootPath = "/var/www/html" })
    .ExecuteAsync();
```

### DNS Plugins

DNS plugins enable DNS-01 challenges, required for wildcard certificates. They ship as separate NuGet packages.

#### Cloudflare (`CertNET.Plugins.Cloudflare`)

```csharp
var plugin = new CloudflarePlugin
{
    ApiToken = "your-api-token",        // recommended (Zone:DNS:Edit scope)
    PropagationSeconds = 30
};

// Or use Global API Key (legacy, not supported by win-acme)
var plugin = new CloudflarePlugin
{
    ApiKey = "your-global-api-key",
    Email = "you@example.com"
};
```

### Writing a Custom DNS Plugin

Extend `DnsPluginBase` and implement the abstract members:

```csharp
public class Route53Plugin : DnsPluginBase
{
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }

    public override string AuthenticatorName => "dns-route53";
    public override string? CertbotPipPackage => "certbot-dns-route53";
    public override int DefaultPropagationSeconds => 30;
    public override string DockerImage => "certbot/dns-route53";

    // win-acme plugin support (optional)
    public override string? WinAcmePluginId => "plugin.validation.dns.route53";
    public override string? WinAcmeValidationName => "route53";

    protected override string GetCredentialFileContent() =>
        $"dns_route53_access_key_id = {AccessKeyId}\n" +
        $"dns_route53_secret_access_key = {SecretAccessKey}";

    public override IEnumerable<string> GetWinAcmeArguments()
    {
        if (!string.IsNullOrEmpty(AccessKeyId))
        {
            yield return "--route53accesskeyid";
            yield return AccessKeyId;
        }
        // ...
    }

    public override void Validate()
    {
        if (string.IsNullOrWhiteSpace(AccessKeyId))
            throw new PluginValidationException("AccessKeyId is required.");
        if (string.IsNullOrWhiteSpace(SecretAccessKey))
            throw new PluginValidationException("SecretAccessKey is required.");
    }
}
```

---

## Auto-Download

When `AutoDownload = true`, CertNET will download and install the required ACME tool to a user-local directory on first use. No admin/root privileges required.

| Platform | Tool | Install Location |
|---|---|---|
| Windows | win-acme | `%LOCALAPPDATA%\certnet\tools\win-acme\` |
| Linux/macOS | certbot | `~/.local/share/certnet/tools/certbot/` |

DNS plugin dependencies are also auto-installed:
- **certbot**: pip-installs the DNS plugin package into the managed venv (e.g., `certbot-dns-cloudflare`)
- **win-acme**: downloads the plugin zip from GitHub and extracts DLLs alongside `wacs.exe`

Versions are pinned and tracked via marker files. Use `ToolDownloader.UpdateToolsAsync()` to force a re-download.

---

## Real-Time Output

Hook into `OutputDataReceived` and `ErrorDataReceived` to stream ACME tool output as it happens:

```csharp
var client = new CertnetClient(options =>
{
    options.OutputDataReceived = line => Console.WriteLine(line);
    options.ErrorDataReceived = line =>
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Error.WriteLine(line);
        Console.ResetColor();
    };
});
```

Output is also accumulated in `CertnetResult.StandardOutput` and `.StandardError` after execution completes.

---

## Certificate Output

After a successful `certonly` command, the result includes a `Certificate` property with file paths and PEM contents:

```csharp
var result = await client.Certonly()
    .ForDomain("example.com")
    .WithPlugin<StandalonePlugin>()
    .WithOutputDirectory("/etc/certs")
    .ExecuteAsync();

if (result.Certificate is not null)
{
    // File paths
    Console.WriteLine(result.Certificate.OutputDirectory);   // /etc/certs
    Console.WriteLine(result.Certificate.CertificatePath);   // /etc/certs/cert.pem
    Console.WriteLine(result.Certificate.PrivateKeyPath);    // /etc/certs/privkey.pem
    Console.WriteLine(result.Certificate.FullChainPath);     // /etc/certs/fullchain.pem
    Console.WriteLine(result.Certificate.ChainPath);         // /etc/certs/chain.pem

    // PEM contents (loaded from disk when files exist)
    string? fullChainPem = result.Certificate.FullChain;
    string? privateKeyPem = result.Certificate.PrivateKey;

    // Check if all files are present
    if (result.Certificate.HasAllFiles)
        Console.WriteLine("All certificate files are on disk.");
}
```

Paths are resolved automatically:
- **Explicit output directory** (`WithOutputDirectory`) — probes for standard file names
- **certbot stdout** — parses "Certificate is saved at:" / "Key is saved at:" lines
- **win-acme stdout** — parses "Exporting .pem files to" and derives file names from the domain

For non-certonly commands (`Renew`, `Revoke`, etc.), `Certificate` is `null`.

---

## Commands Reference

### `Certonly()`

Obtains a new certificate without installing it to a web server.

```csharp
client.Certonly()
    .ForDomain("example.com")                  // required
    .ForDomains("a.com", "b.com")              // add multiple domains
    .WithCertName("my-cert")                   // friendly name
    .WithKeyType(KeyType.Ecdsa)                // RSA or ECDSA
    .WithRsaKeySize(4096)                      // RSA key size (default 2048)
    .WithEllipticCurve(EllipticCurve.Secp384r1)// curve for ECDSA
    .WithPreferredChain("ISRG Root X1")        // preferred chain
    .WithPlugin(plugin)                        // authenticator plugin
    .WithOutputDirectory("/certs")             // certificate output dir
    .WithCertPath("/certs/cert.pem")           // specific file paths
    .WithKeyPath("/certs/key.pem")
    .WithFullChainPath("/certs/fullchain.pem")
    .WithChainPath("/certs/chain.pem")
    .WithPreHook("systemctl stop nginx")       // lifecycle hooks
    .WithPostHook("systemctl start nginx")
    .WithDeployHook("systemctl reload nginx")
    .ForceRenewal()                            // force even if not expiring
    .DryRun()                                  // test without saving
    .Expand()                                  // add domains to existing cert
    .AllowDuplicate()                          // allow duplicate cert
    .ExecuteAsync(cancellationToken);
```

### `Renew()`

Renews previously obtained certificates.

```csharp
client.Renew()
    .ForCertificate("example.com")             // specific cert (optional)
    .ForceRenewal()
    .WithKeyType(KeyType.Ecdsa)
    .DryRun()
    .WithPreHook("...")
    .WithPostHook("...")
    .WithDeployHook("...")
    .Quiet()
    .NoAutoRenew()
    .ReuseKey()
    .ExecuteAsync(cancellationToken);
```

### `Revoke()`

Revokes a certificate.

```csharp
client.Revoke()
    .ForCertificate("example.com")
    .ForCertificatePath("/etc/letsencrypt/live/example.com/cert.pem")
    .WithKeyPath("/etc/letsencrypt/live/example.com/privkey.pem")
    .WithReason(RevocationReason.Superseded)
    .DeleteAfterRevoke()
    .NoDeleteAfterRevoke()
    .ExecuteAsync(cancellationToken);
```

### `Delete()`

Deletes a certificate and all related files.

```csharp
client.Delete()
    .ForCertificate("example.com")
    .ExecuteAsync(cancellationToken);
```

### `Certificates()`

Lists all managed certificates with parsed metadata.

```csharp
var result = await client.Certificates().ExecuteAsync();
foreach (var cert in result.Certificates)
{
    Console.WriteLine($"{cert.CertificateName} [{cert.KeyType}]");
    Console.WriteLine($"  Domains: {string.Join(", ", cert.Domains)}");
    Console.WriteLine($"  Expires: {cert.ExpiryDate:yyyy-MM-dd}");
    Console.WriteLine($"  Valid:   {cert.IsValid}");
    Console.WriteLine($"  Cert:    {cert.CertificatePath}");
    Console.WriteLine($"  Key:     {cert.PrivateKeyPath}");
}
```

---

## Error Handling

CertNET uses a typed exception hierarchy:

```csharp
try
{
    await client.Certonly()
        .ForDomain("example.com")
        .WithPlugin<StandalonePlugin>()
        .ExecuteAsync();
}
catch (PluginValidationException ex)
{
    // Plugin configuration error (e.g., missing API token)
    Console.Error.WriteLine($"Config error: {ex.Message}");
}
catch (CertnetNotFoundException ex)
{
    // certbot/wacs.exe/docker not found on the system
    Console.Error.WriteLine($"Tool not found: {ex.Message}");
}
catch (CertnetExecutionException ex)
{
    // The process ran but exited with a non-zero code
    Console.Error.WriteLine($"Failed (exit {ex.Result.ExitCode}): {ex.Result.StandardError}");
    Console.Error.WriteLine($"Command: {ex.Result.CommandLine}");
}
```

---

## Project Structure

```
CertNET/
  CertnetClient.cs                 # Main entry point
  CertnetClientOptions.cs          # Configuration
  Commands/
    CertonlyCommand.cs             # certonly builder
    RenewCommand.cs                # renew builder
    RevokeCommand.cs               # revoke builder
    DeleteCommand.cs               # delete builder
    CertificatesCommand.cs         # certificates builder
  Plugins/
    ICertnetPlugin.cs              # Plugin interface
    IDnsPlugin.cs                  # DNS plugin interface
    DnsPluginBase.cs               # Base class for DNS plugins
    StandalonePlugin.cs            # HTTP-01 standalone
    WebrootPlugin.cs               # HTTP-01 webroot
    ManualPlugin.cs                # Manual with hooks
    NginxPlugin.cs                 # Nginx integration
    ApachePlugin.cs                # Apache integration
  Execution/
    ProcessRunner.cs               # Shared process execution + streaming
    CertnetExecutor.cs             # Native certbot executor
    WinAcmeExecutor.cs             # win-acme executor with arg translation
    DockerCertnetExecutor.cs       # Docker executor with volume mounts
    ToolDownloader.cs              # Auto-download engine
  Exceptions/
    CertnetException.cs            # Base exception
    CertnetExecutionException.cs   # Non-zero exit code
    CertnetNotFoundException.cs    # Tool not found
    PluginValidationException.cs   # Invalid plugin config
  Models/
    CertificateInfo.cs             # Parsed certificate metadata
    CertificateOutput.cs           # Certificate files + PEM contents
    CertificatesResult.cs          # List of certificates
  Parsing/
    CertificatesParser.cs          # Parse `certbot certificates` output
    CertificateOutputParser.cs     # Extract cert paths from certonly stdout

CertNET.Plugins.Cloudflare/
  CloudflarePlugin.cs              # Cloudflare DNS-01 plugin

CertNET.Console/
  Program.cs                       # Interactive demo app

CertNET.Tests/
  ...                              # 247 xUnit tests
```

---

## Running the Demo

The included console app walks through obtaining a certificate with Cloudflare DNS validation:

```bash
cd CertNET.Console
dotnet run
```

It will prompt for your domain, Cloudflare API token, email, and preferred settings, then obtain a certificate from Let's Encrypt with real-time output streaming.

---

## Running Tests

```bash
dotnet test
```

247 tests covering commands, plugins, argument translation, auto-download, output streaming, certificate output parsing, and more.

---

## License

MIT
