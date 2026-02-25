using CertNET.Execution;
using CertNET.Plugins;
using CertNET.Plugins.Cloudflare;
using CertNET.Tests.Helpers;

namespace CertNET.Tests;

public class WinAcmeExecutorTests
{
    // ── WinAcmeValidationName on plugins ─────────────────────────────

    [Fact]
    public void StandalonePlugin_WinAcmeValidationName_IsSelfHosting()
    {
        var plugin = new StandalonePlugin();
        Assert.Equal("selfhosting", plugin.WinAcmeValidationName);
    }

    [Fact]
    public void WebrootPlugin_WinAcmeValidationName_IsFilesystem()
    {
        var plugin = new WebrootPlugin { WebRootPath = "/var/www" };
        Assert.Equal("filesystem", plugin.WinAcmeValidationName);
    }

    [Fact]
    public void ManualPlugin_WinAcmeValidationName_IsManual()
    {
        var plugin = new ManualPlugin();
        Assert.Equal("manual", plugin.WinAcmeValidationName);
    }

    [Fact]
    public void NginxPlugin_WinAcmeValidationName_IsNull()
    {
        var plugin = new NginxPlugin();
        Assert.Null(plugin.WinAcmeValidationName);
    }

    [Fact]
    public void ApachePlugin_WinAcmeValidationName_IsNull()
    {
        var plugin = new ApachePlugin();
        Assert.Null(plugin.WinAcmeValidationName);
    }

    [Fact]
    public void CloudflarePlugin_WinAcmeValidationName_IsCloudflare()
    {
        var plugin = new CloudflarePlugin { ApiToken = "test" };
        Assert.Equal("cloudflare", plugin.WinAcmeValidationName);
    }

    // ── CloudflarePlugin WinAcme arguments ──────────────────────────

    [Fact]
    public void CloudflarePlugin_GetWinAcmeArguments_IncludesApiToken()
    {
        var plugin = new CloudflarePlugin { ApiToken = "my-cf-token" };
        var args = plugin.GetWinAcmeArguments().ToList();
        Assert.Contains("--cloudflareapitoken", args);
        Assert.Contains("my-cf-token", args);
    }

    [Fact]
    public void CloudflarePlugin_GetWinAcmeArguments_NoToken_ReturnsEmpty()
    {
        // When using ApiKey auth (not supported by win-acme), no args returned
        var plugin = new CloudflarePlugin { ApiKey = "key", Email = "test@example.com" };
        var args = plugin.GetWinAcmeArguments().ToList();
        Assert.Empty(args);
    }

    // ── StandalonePlugin WinAcme arguments ──────────────────────────

    [Fact]
    public void StandalonePlugin_GetWinAcmeArguments_WithPort_IncludesValidationPort()
    {
        var plugin = new StandalonePlugin { HttpPort = 8080 };
        var args = plugin.GetWinAcmeArguments().ToList();
        Assert.Contains("--validationport", args);
        Assert.Contains("8080", args);
    }

    [Fact]
    public void StandalonePlugin_GetWinAcmeArguments_NoPort_IsEmpty()
    {
        var plugin = new StandalonePlugin();
        Assert.Empty(plugin.GetWinAcmeArguments());
    }

    // ── WebrootPlugin WinAcme arguments ─────────────────────────────

    [Fact]
    public void WebrootPlugin_GetWinAcmeArguments_IncludesWebroot()
    {
        var plugin = new WebrootPlugin { WebRootPath = "/var/www/html" };
        var args = plugin.GetWinAcmeArguments().ToList();
        Assert.Contains("--webroot", args);
        Assert.Contains("/var/www/html", args);
    }

    // ── ExecutionMode.WinAcme ───────────────────────────────────────

    [Fact]
    public void ExecutionMode_WinAcme_CanBeSet()
    {
        var options = new CertnetClientOptions { ExecutionMode = ExecutionMode.WinAcme };
        Assert.Equal(ExecutionMode.WinAcme, options.ExecutionMode);
    }

    // ── Argument translation ────────────────────────────────────────

    [Fact]
    public void TranslateCertonly_BasicDomain_ProducesSourceManualAndHost()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "certonly", "-n", "--agree-tos", "--email", "admin@test.com",
            "--server", "https://acme-staging-v02.api.letsencrypt.org/directory",
            "--authenticator", "standalone",
            "-d", "example.com"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--source", result);
        Assert.Contains("manual", result);
        Assert.Contains("--host", result);
        Assert.Contains("example.com", result);
        Assert.Contains("--emailaddress", result);
        Assert.Contains("admin@test.com", result);
        Assert.Contains("--accepttos", result);
        Assert.Contains("--baseuri", result); // staging server via --baseuri (not --test)
        Assert.Contains("https://acme-staging-v02.api.letsencrypt.org/directory", result);
        Assert.DoesNotContain("--test", result); // --test is never used (causes Console.ReadKey crash)
        Assert.Contains("--closeonfinish", result);
    }

    [Fact]
    public void TranslateCertonly_MultipleDomains_JoinedWithComma()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "certonly", "-d", "example.com", "-d", "*.example.com",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        var hostIdx = result.IndexOf("--host");
        Assert.NotEqual(-1, hostIdx);
        Assert.Equal("example.com,*.example.com", result[hostIdx + 1]);
    }

    [Fact]
    public void TranslateCertonly_WithCertName_IncludesFriendlyName()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "certonly", "--cert-name", "my-cert",
            "-d", "example.com",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--friendlyname", result);
        Assert.Contains("my-cert", result);
    }

    [Fact]
    public void TranslateCertonly_EcdsaKey_TranslatesToCsrEc()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "certonly", "--key-type", "ecdsa",
            "-d", "example.com",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--csr", result);
        Assert.Contains("ec", result);
    }

    [Fact]
    public void TranslateCertonly_RsaKey_TranslatesToCsrRsa()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "certonly", "--key-type", "rsa",
            "-d", "example.com",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--csr", result);
        Assert.Contains("rsa", result);
    }

    [Fact]
    public void TranslateCertonly_WithOutputPaths_TranslatesToPemFiles()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "certonly",
            "--cert-path", "/certs/cert.pem",
            "--key-path", "/certs/privkey.pem",
            "-d", "example.com",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--store", result);
        Assert.Contains("pemfiles", result);
        Assert.Contains("--pemfilespath", result);
    }

    [Fact]
    public void TranslateCertonly_ProductionServer_IncludesBaseUri()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "certonly", "-d", "example.com",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--baseuri", result);
        Assert.Contains("https://acme-v02.api.letsencrypt.org/directory", result);
        Assert.DoesNotContain("--test", result);
    }

    [Fact]
    public void TranslateCertonly_ForceRenewal_IncludesForce()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "certonly", "--force-renewal",
            "-d", "example.com",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--force", result);
    }

    [Fact]
    public void TranslateCertonly_DryRun_UsesBaseUriNotTest()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "certonly", "--dry-run",
            "-d", "example.com",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        // --dry-run passes through the server URL via --baseuri instead of --test,
        // because --test triggers Console.ReadKey() prompts that crash with redirected stdin
        Assert.Contains("--baseuri", result);
        Assert.DoesNotContain("--test", result);
    }

    [Fact]
    public void TranslateCertonly_WithValidationPlugin_IncludesValidation()
    {
        var executor = CreateTestExecutor();
        executor.ValidationPluginName = "cloudflare";
        executor.PluginArguments.AddRange(["--cloudflareapitoken", "my-token"]);

        var certbotArgs = new List<string>
        {
            "certonly", "-d", "example.com",
            "--authenticator", "dns-cloudflare",
            "--server", "https://acme-staging-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--validation", result);
        Assert.Contains("cloudflare", result);
        Assert.Contains("--cloudflareapitoken", result);
        Assert.Contains("my-token", result);
    }

    // ── Renew translation ───────────────────────────────────────────

    [Fact]
    public void TranslateRenew_Basic_IncludesRenewFlag()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "renew", "-n",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--renew", result);
        Assert.Contains("--closeonfinish", result);
    }

    [Fact]
    public void TranslateRenew_ForCertificate_IncludesFriendlyName()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "renew", "--cert-name", "example.com",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--renew", result);
        Assert.Contains("--friendlyname", result);
        Assert.Contains("example.com", result);
    }

    // ── Revoke translation ──────────────────────────────────────────

    [Fact]
    public void TranslateRevoke_IncludesRevokeFlag()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "revoke", "--cert-name", "example.com", "-n",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--revoke", result);
        Assert.Contains("--friendlyname", result);
        Assert.Contains("example.com", result);
    }

    // ── Delete translation ──────────────────────────────────────────

    [Fact]
    public void TranslateDelete_IncludesCancelFlag()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "delete", "--cert-name", "example.com", "-n"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--cancel", result);
        Assert.Contains("--friendlyname", result);
        Assert.Contains("example.com", result);
    }

    // ── Certificates translation ────────────────────────────────────

    [Fact]
    public void TranslateCertificates_IncludesListFlag()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "certificates", "-n"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--list", result);
        Assert.Contains("--closeonfinish", result);
    }

    // ── Non-interactive safety flags ───────────────────────────────

    [Fact]
    public void TranslateCertonly_AlwaysIncludesNotaskscheduler()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "certonly", "-d", "example.com",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--notaskscheduler", result);
    }

    [Fact]
    public void TranslateCertonly_AlwaysIncludesCloseonfinish_EvenWithoutNonInteractive()
    {
        var executor = CreateTestExecutor();
        // Note: no -n flag — closeonfinish should still be present
        var certbotArgs = new List<string>
        {
            "certonly", "-d", "example.com",
            "--server", "https://acme-staging-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--closeonfinish", result);
        Assert.Contains("--notaskscheduler", result);
    }

    [Fact]
    public void TranslateRenew_AlwaysIncludesCloseonfinish_EvenWithoutNonInteractive()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "renew",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--renew", result);
        Assert.Contains("--closeonfinish", result);
    }

    [Fact]
    public void TranslateRevoke_AlwaysIncludesCloseonfinish_EvenWithoutNonInteractive()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "revoke", "--cert-name", "example.com",
            "--server", "https://acme-v02.api.letsencrypt.org/directory"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--revoke", result);
        Assert.Contains("--closeonfinish", result);
    }

    [Fact]
    public void TranslateDelete_AlwaysIncludesCloseonfinish_EvenWithoutNonInteractive()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "delete", "--cert-name", "example.com"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--cancel", result);
        Assert.Contains("--closeonfinish", result);
    }

    [Fact]
    public void TranslateCertificates_AlwaysIncludesCloseonfinish_EvenWithoutNonInteractive()
    {
        var executor = CreateTestExecutor();
        var certbotArgs = new List<string>
        {
            "certificates"
        };

        var result = executor.TranslateCertbotToWinAcme(certbotArgs);

        Assert.Contains("--list", result);
        Assert.Contains("--closeonfinish", result);
    }

    // ── Integration: client with mock executor ──────────────────────

    [Fact]
    public async Task Client_WithWinAcmeMode_MockExecutor_Works()
    {
        var options = new CertnetClientOptions
        {
            ExecutionMode = ExecutionMode.WinAcme,
            Server = AcmeServer.Staging,
            Email = "test@example.com",
            AgreeToTermsOfService = true
        };
        var executor = new MockCertnetExecutor();
        var client = new CertnetClient(options, executor);

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        // The mock executor still receives certbot-style args
        // (translation happens inside the real WinAcmeExecutor, not the mock)
        Assert.Contains("certonly", executor.CapturedArguments!);
    }

    [Fact]
    public async Task Client_WithCloudflarePlugin_WinAcmeMode_MockExecutor_IncludesAllFlags()
    {
        var options = new CertnetClientOptions
        {
            ExecutionMode = ExecutionMode.WinAcme,
            Server = AcmeServer.Staging,
            Email = "test@example.com",
            AgreeToTermsOfService = true
        };
        var executor = new MockCertnetExecutor();
        var client = new CertnetClient(options, executor);

        await client.Certonly()
            .ForDomains("example.com", "*.example.com")
            .WithPlugin(new CloudflarePlugin
            {
                ApiToken = "my-token",
                PropagationSeconds = 60
            })
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("certonly", args);
        Assert.Contains("--authenticator", args);
        Assert.Contains("dns-cloudflare", args);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates a WinAcmeExecutor that won't try to locate wacs.exe,
    /// for testing the argument translation logic only.
    /// We use reflection to bypass the constructor's locator.
    /// </summary>
    private static WinAcmeExecutor CreateTestExecutor()
    {
        // We need a real WinAcmeExecutor instance to test TranslateCertbotToWinAcme,
        // but the constructor tries to locate wacs.exe which won't exist in CI.
        // Create a temp dummy file to satisfy the locator.
        var tempDir = Path.Combine(Path.GetTempPath(), "certnet-test-winacme");
        Directory.CreateDirectory(tempDir);
        var wacsPath = Path.Combine(tempDir, "wacs.exe");
        if (!File.Exists(wacsPath))
            File.WriteAllText(wacsPath, "dummy");

        var options = new CertnetClientOptions
        {
            ExecutablePath = wacsPath
        };

        return new WinAcmeExecutor(options);
    }
}
