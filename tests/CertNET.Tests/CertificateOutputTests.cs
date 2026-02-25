using CertNET.Execution;
using CertNET.Models;
using CertNET.Parsing;
using CertNET.Plugins;
using CertNET.Plugins.Cloudflare;
using CertNET.Tests.Helpers;

namespace CertNET.Tests;

public class CertificateOutputTests
{
    // ── CertificateOutput model ─────────────────────────────────────

    [Fact]
    public void HasAllFiles_AllPopulated_ReturnsTrue()
    {
        var output = new CertificateOutput
        {
            Certificate = "cert-pem",
            PrivateKey = "key-pem",
            FullChain = "fullchain-pem",
            Chain = "chain-pem"
        };

        Assert.True(output.HasAllFiles);
    }

    [Fact]
    public void HasAllFiles_MissingOne_ReturnsFalse()
    {
        var output = new CertificateOutput
        {
            Certificate = "cert-pem",
            PrivateKey = "key-pem",
            FullChain = null,
            Chain = "chain-pem"
        };

        Assert.False(output.HasAllFiles);
    }

    [Fact]
    public void HasAllFiles_AllNull_ReturnsFalse()
    {
        var output = new CertificateOutput();
        Assert.False(output.HasAllFiles);
    }

    [Fact]
    public void FromPaths_ReadsExistingFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), "certnet-test-output-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        try
        {
            var certPath = Path.Combine(dir, "cert.pem");
            var keyPath = Path.Combine(dir, "privkey.pem");
            var fullChainPath = Path.Combine(dir, "fullchain.pem");
            var chainPath = Path.Combine(dir, "chain.pem");

            File.WriteAllText(certPath, "-----BEGIN CERTIFICATE-----\nAAA\n-----END CERTIFICATE-----");
            File.WriteAllText(keyPath, "-----BEGIN PRIVATE KEY-----\nBBB\n-----END PRIVATE KEY-----");
            File.WriteAllText(fullChainPath, "-----BEGIN CERTIFICATE-----\nCCC\n-----END CERTIFICATE-----");
            File.WriteAllText(chainPath, "-----BEGIN CERTIFICATE-----\nDDD\n-----END CERTIFICATE-----");

            var output = CertificateOutput.FromPaths(dir, certPath, keyPath, fullChainPath, chainPath);

            Assert.Equal(dir, output.OutputDirectory);
            Assert.Equal(certPath, output.CertificatePath);
            Assert.Equal(keyPath, output.PrivateKeyPath);
            Assert.Equal(fullChainPath, output.FullChainPath);
            Assert.Equal(chainPath, output.ChainPath);
            Assert.Contains("AAA", output.Certificate);
            Assert.Contains("BBB", output.PrivateKey);
            Assert.Contains("CCC", output.FullChain);
            Assert.Contains("DDD", output.Chain);
            Assert.True(output.HasAllFiles);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FromPaths_MissingFiles_SetsContentsToNull()
    {
        var dir = Path.Combine(Path.GetTempPath(), "certnet-test-missing-" + Guid.NewGuid().ToString("N"));

        var output = CertificateOutput.FromPaths(
            dir,
            Path.Combine(dir, "cert.pem"),
            Path.Combine(dir, "privkey.pem"),
            Path.Combine(dir, "fullchain.pem"),
            Path.Combine(dir, "chain.pem"));

        Assert.Equal(dir, output.OutputDirectory);
        Assert.NotNull(output.CertificatePath);
        Assert.Null(output.Certificate);
        Assert.Null(output.PrivateKey);
        Assert.Null(output.FullChain);
        Assert.Null(output.Chain);
        Assert.False(output.HasAllFiles);
    }

    [Fact]
    public void FromPaths_NullPaths_SetsEverythingToNull()
    {
        var output = CertificateOutput.FromPaths(null, null, null, null, null);

        Assert.Null(output.OutputDirectory);
        Assert.Null(output.CertificatePath);
        Assert.Null(output.Certificate);
        Assert.False(output.HasAllFiles);
    }

    // ── CertificateOutputParser: certbot output ─────────────────────

    [Fact]
    public void Parse_CertbotOutput_ExtractsPaths()
    {
        var stdout = """
            Saving debug log to /var/log/letsencrypt/letsencrypt.log
            Requesting a certificate for example.com

            Successfully received certificate.
            Certificate is saved at: /etc/letsencrypt/live/example.com/fullchain.pem
            Key is saved at:         /etc/letsencrypt/live/example.com/privkey.pem
            This certificate expires on 2026-05-26.
            These files will be updated when the certificate renews.
            """;

        var result = CertificateOutputParser.Parse(stdout, outputDirectory: null, primaryDomain: "example.com");

        Assert.NotNull(result);
        // Use EndsWith/Contains to handle platform path separator differences
        Assert.Contains("example.com", result.OutputDirectory!);
        Assert.EndsWith("fullchain.pem", result.FullChainPath!);
        Assert.EndsWith("privkey.pem", result.PrivateKeyPath!);
        Assert.EndsWith("cert.pem", result.CertificatePath!);
        Assert.EndsWith("chain.pem", result.ChainPath!);
        Assert.Contains("example.com", result.CertificatePath!);
    }

    [Fact]
    public void Parse_CertbotOutput_OnlyCertPath_StillWorks()
    {
        var stdout = "Certificate is saved at: /etc/letsencrypt/live/test.com/fullchain.pem";

        var result = CertificateOutputParser.Parse(stdout, null, "test.com");

        Assert.NotNull(result);
        Assert.Equal("/etc/letsencrypt/live/test.com/fullchain.pem", result.FullChainPath);
        Assert.Null(result.PrivateKeyPath);
    }

    // ── CertificateOutputParser: win-acme output ────────────────────

    [Fact]
    public void Parse_WinAcmeOutput_ExtractsDirectoryAndDerivesPaths()
    {
        var stdout = """
             A simple Windows ACMEv2 client (WACS)
             Running in mode: Unattended
             Exporting .pem files to C:\Certificates\
             Renewal for example.com was successfully created
            """;

        var result = CertificateOutputParser.Parse(stdout, outputDirectory: null, primaryDomain: "example.com");

        Assert.NotNull(result);
        Assert.Contains("Certificates", result.OutputDirectory!);
        Assert.EndsWith("-crt.pem", result.CertificatePath!);
        Assert.EndsWith("-key.pem", result.PrivateKeyPath!);
        Assert.EndsWith("-chain.pem", result.FullChainPath!);
        Assert.EndsWith("-chain-only.pem", result.ChainPath!);
        Assert.Contains("example.com", result.CertificatePath!);
    }

    [Fact]
    public void Parse_WinAcmeOutput_WildcardDomain_ReplacesStarWithUnderscore()
    {
        var stdout = "Exporting .pem files to C:\\certs\\";

        var result = CertificateOutputParser.Parse(stdout, null, "*.example.com");

        Assert.NotNull(result);
        Assert.Contains("_.example.com-crt.pem", result.CertificatePath!);
        Assert.Contains("_.example.com-key.pem", result.PrivateKeyPath!);
    }

    // ── CertificateOutputParser: explicit output directory ──────────

    [Fact]
    public void Parse_WithOutputDirectory_UsesStandardNames()
    {
        var dir = "/my/certs";

        var result = CertificateOutputParser.Parse("any stdout", outputDirectory: dir, primaryDomain: "x.com");

        Assert.NotNull(result);
        Assert.Equal(dir, result.OutputDirectory);
        Assert.Equal(Path.Combine(dir, "cert.pem"), result.CertificatePath);
        Assert.Equal(Path.Combine(dir, "privkey.pem"), result.PrivateKeyPath);
        Assert.Equal(Path.Combine(dir, "fullchain.pem"), result.FullChainPath);
        Assert.Equal(Path.Combine(dir, "chain.pem"), result.ChainPath);
    }

    [Fact]
    public void Parse_NoMatchingOutput_ReturnsNull()
    {
        var result = CertificateOutputParser.Parse("some unrelated output", null, null);

        Assert.Null(result);
    }

    // ── CertnetResult.Certificate ───────────────────────────────────

    [Fact]
    public void CertnetResult_Certificate_DefaultsToNull()
    {
        var result = new CertnetResult
        {
            ExitCode = 0,
            StandardOutput = "",
            StandardError = "",
            CommandLine = "test"
        };

        Assert.Null(result.Certificate);
    }

    [Fact]
    public void CertnetResult_Certificate_CanBeSet()
    {
        var output = new CertificateOutput
        {
            OutputDirectory = "/certs",
            CertificatePath = "/certs/cert.pem"
        };

        var result = new CertnetResult
        {
            ExitCode = 0,
            StandardOutput = "",
            StandardError = "",
            CommandLine = "test",
            Certificate = output
        };

        Assert.NotNull(result.Certificate);
        Assert.Equal("/certs", result.Certificate.OutputDirectory);
    }

    // ── Integration: CertonlyCommand populates Certificate ──────────

    [Fact]
    public async Task CertonlyCommand_ExecuteAsync_PopulatesCertificateFromCertbotOutput()
    {
        var mockExecutor = new MockCertnetExecutor
        {
            ResultToReturn = new CertnetResult
            {
                ExitCode = 0,
                StandardOutput = """
                    Successfully received certificate.
                    Certificate is saved at: /etc/letsencrypt/live/example.com/fullchain.pem
                    Key is saved at:         /etc/letsencrypt/live/example.com/privkey.pem
                    """,
                StandardError = "",
                CommandLine = "certbot certonly ..."
            }
        };

        var options = TestDefaults.CreateOptions();
        var client = new CertnetClient(options, mockExecutor);

        var result = await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        Assert.NotNull(result.Certificate);
        Assert.EndsWith("fullchain.pem", result.Certificate.FullChainPath!);
        Assert.EndsWith("privkey.pem", result.Certificate.PrivateKeyPath!);
        Assert.EndsWith("cert.pem", result.Certificate.CertificatePath!);
        Assert.EndsWith("chain.pem", result.Certificate.ChainPath!);
        Assert.Contains("example.com", result.Certificate.OutputDirectory!);
    }

    [Fact]
    public async Task CertonlyCommand_ExecuteAsync_PopulatesCertificateFromOutputDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "certnet-certonly-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        try
        {
            // Create fake cert files
            File.WriteAllText(Path.Combine(dir, "cert.pem"), "CERT");
            File.WriteAllText(Path.Combine(dir, "privkey.pem"), "KEY");
            File.WriteAllText(Path.Combine(dir, "fullchain.pem"), "FULLCHAIN");
            File.WriteAllText(Path.Combine(dir, "chain.pem"), "CHAIN");

            var mockExecutor = new MockCertnetExecutor();
            var options = TestDefaults.CreateOptions();
            var client = new CertnetClient(options, mockExecutor);

            var result = await client.Certonly()
                .ForDomain("example.com")
                .WithPlugin<StandalonePlugin>()
                .WithOutputDirectory(dir)
                .ExecuteAsync();

            Assert.NotNull(result.Certificate);
            Assert.Equal(dir, result.Certificate.OutputDirectory);
            Assert.Equal("CERT", result.Certificate.Certificate);
            Assert.Equal("KEY", result.Certificate.PrivateKey);
            Assert.Equal("FULLCHAIN", result.Certificate.FullChain);
            Assert.Equal("CHAIN", result.Certificate.Chain);
            Assert.True(result.Certificate.HasAllFiles);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task CertonlyCommand_ExecuteAsync_NoOutputPatterns_CertificateIsNull()
    {
        var mockExecutor = new MockCertnetExecutor
        {
            ResultToReturn = new CertnetResult
            {
                ExitCode = 0,
                StandardOutput = "The dry run was successful.",
                StandardError = "",
                CommandLine = "certbot certonly --dry-run ..."
            }
        };

        var options = TestDefaults.CreateOptions();
        var client = new CertnetClient(options, mockExecutor);

        var result = await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .DryRun()
            .ExecuteAsync();

        Assert.Null(result.Certificate);
    }

    [Fact]
    public async Task CertonlyCommand_ExecuteAsync_WinAcmeOutput_PopulatesCertificate()
    {
        var mockExecutor = new MockCertnetExecutor
        {
            ResultToReturn = new CertnetResult
            {
                ExitCode = 0,
                StandardOutput = """
                    Running in mode: Unattended
                    Exporting .pem files to C:\certs\
                    Renewal for example.com was successfully created
                    """,
                StandardError = "",
                CommandLine = "wacs.exe --source manual ..."
            }
        };

        var options = TestDefaults.CreateOptions();
        var client = new CertnetClient(options, mockExecutor);

        var result = await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        Assert.NotNull(result.Certificate);
        Assert.Contains("certs", result.Certificate.OutputDirectory!);
        Assert.Contains("example.com-crt.pem", result.Certificate.CertificatePath!);
        Assert.Contains("example.com-key.pem", result.Certificate.PrivateKeyPath!);
    }
}
