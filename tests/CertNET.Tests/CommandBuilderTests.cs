using CertNET.Plugins;
using CertNET.Tests.Helpers;

namespace CertNET.Tests;

public class CommandBuilderTests
{
    // ── Certonly ──────────────────────────────────────────────────────

    [Fact]
    public async Task Certonly_BasicDomain_ProducesCorrectArguments()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("certonly", args);
        Assert.Contains("-d", args);
        Assert.Contains("example.com", args);
        Assert.Contains("--authenticator", args);
        Assert.Contains("standalone", args);
    }

    [Fact]
    public async Task Certonly_MultipleDomains_AllIncluded()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomains("example.com", "www.example.com", "api.example.com")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("example.com", args);
        Assert.Contains("www.example.com", args);
        Assert.Contains("api.example.com", args);

        // Should have 3 domain flags
        Assert.Equal(3, args.Count(a => a == "-d"));
    }

    [Fact]
    public async Task Certonly_WithCertName_IncludesCertName()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithCertName("my-cert")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--cert-name", args);
        Assert.Contains("my-cert", args);
    }

    [Fact]
    public async Task Certonly_WithRsaKey_IncludesKeyTypeAndSize()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithKeyType(KeyType.Rsa)
            .WithRsaKeySize(4096)
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--key-type", args);
        Assert.Contains("rsa", args);
        Assert.Contains("--rsa-key-size", args);
        Assert.Contains("4096", args);
    }

    [Fact]
    public async Task Certonly_WithEcdsaKey_IncludesKeyTypeAndCurve()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithKeyType(KeyType.Ecdsa)
            .WithEllipticCurve(EllipticCurve.Secp384r1)
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--key-type", args);
        Assert.Contains("ecdsa", args);
        Assert.Contains("--elliptic-curve", args);
        Assert.Contains("secp384r1", args);
    }

    [Fact]
    public async Task Certonly_DryRun_IncludesDryRunFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .DryRun()
            .ExecuteAsync();

        Assert.Contains("--dry-run", executor.CapturedArguments!);
    }

    [Fact]
    public async Task Certonly_ForceRenewal_IncludesForceRenewalFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .ForceRenewal()
            .ExecuteAsync();

        Assert.Contains("--force-renewal", executor.CapturedArguments!);
    }

    [Fact]
    public async Task Certonly_WithHooks_IncludesHookFlags()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .WithPreHook("echo pre")
            .WithPostHook("echo post")
            .WithDeployHook("echo deploy")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--pre-hook", args);
        Assert.Contains("echo pre", args);
        Assert.Contains("--post-hook", args);
        Assert.Contains("echo post", args);
        Assert.Contains("--deploy-hook", args);
        Assert.Contains("echo deploy", args);
    }

    [Fact]
    public async Task Certonly_Expand_IncludesExpandFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .Expand()
            .ExecuteAsync();

        Assert.Contains("--expand", executor.CapturedArguments!);
    }

    [Fact]
    public async Task Certonly_PreferredChain_IncludesPreferredChainFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .WithPreferredChain("ISRG Root X1")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--preferred-chain", args);
        Assert.Contains("ISRG Root X1", args);
    }

    // ── Certonly Output Paths ────────────────────────────────────────

    [Fact]
    public async Task Certonly_WithCertPath_IncludesCertPathFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .WithCertPath("/certs/cert.pem")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--cert-path", args);
        Assert.Contains("/certs/cert.pem", args);
    }

    [Fact]
    public async Task Certonly_WithKeyPath_IncludesKeyPathFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .WithKeyPath("/certs/privkey.pem")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--key-path", args);
        Assert.Contains("/certs/privkey.pem", args);
    }

    [Fact]
    public async Task Certonly_WithFullChainPath_IncludesFullChainPathFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .WithFullChainPath("/certs/fullchain.pem")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--fullchain-path", args);
        Assert.Contains("/certs/fullchain.pem", args);
    }

    [Fact]
    public async Task Certonly_WithChainPath_IncludesChainPathFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .WithChainPath("/certs/chain.pem")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--chain-path", args);
        Assert.Contains("/certs/chain.pem", args);
    }

    [Fact]
    public async Task Certonly_WithOutputDirectory_IncludesAllFourPaths()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .WithOutputDirectory("/my/certs")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--cert-path", args);
        Assert.Contains("--key-path", args);
        Assert.Contains("--fullchain-path", args);
        Assert.Contains("--chain-path", args);

        // Verify the paths point into the directory
        var certIdx = args.ToList().IndexOf("--cert-path");
        var keyIdx = args.ToList().IndexOf("--key-path");
        var fullchainIdx = args.ToList().IndexOf("--fullchain-path");
        var chainIdx = args.ToList().IndexOf("--chain-path");

        Assert.EndsWith("cert.pem", args[certIdx + 1]);
        Assert.EndsWith("privkey.pem", args[keyIdx + 1]);
        Assert.EndsWith("fullchain.pem", args[fullchainIdx + 1]);
        Assert.EndsWith("chain.pem", args[chainIdx + 1]);
    }

    [Fact]
    public async Task Certonly_WithAllIndividualPaths_IncludesAllFlags()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .WithCertPath("/a/cert.pem")
            .WithKeyPath("/b/privkey.pem")
            .WithFullChainPath("/c/fullchain.pem")
            .WithChainPath("/d/chain.pem")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("/a/cert.pem", args);
        Assert.Contains("/b/privkey.pem", args);
        Assert.Contains("/c/fullchain.pem", args);
        Assert.Contains("/d/chain.pem", args);
    }

    // ── Global Options ───────────────────────────────────────────────

    [Fact]
    public async Task GlobalOptions_StagingServer_IncludesStagingUrl()
    {
        var (client, executor) = TestDefaults.CreateClient(AcmeServer.Staging);

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--server", args);
        Assert.Contains("https://acme-staging-v02.api.letsencrypt.org/directory", args);
    }

    [Fact]
    public async Task GlobalOptions_ProductionServer_IncludesProductionUrl()
    {
        var (client, executor) = TestDefaults.CreateClient(AcmeServer.Production);

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--server", args);
        Assert.Contains("https://acme-v02.api.letsencrypt.org/directory", args);
    }

    [Fact]
    public async Task GlobalOptions_NonInteractive_IncludesNFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        Assert.Contains("-n", executor.CapturedArguments!);
    }

    [Fact]
    public async Task GlobalOptions_AgreeTos_IncludesAgreeTosFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        Assert.Contains("--agree-tos", executor.CapturedArguments!);
    }

    [Fact]
    public async Task GlobalOptions_Email_IncludesEmailFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--email", args);
        Assert.Contains("test@example.com", args);
    }

    [Fact]
    public async Task GlobalOptions_ConfigDir_IncludesConfigDirFlag()
    {
        var executor = new MockCertnetExecutor();
        var options = TestDefaults.CreateOptions();
        options.ConfigDirectory = "/custom/config";
        var client = new CertnetClient(options, executor);

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--config-dir", args);
        Assert.Contains("/custom/config", args);
    }

    // ── Renew ────────────────────────────────────────────────────────

    [Fact]
    public async Task Renew_Basic_ProducesCorrectArguments()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Renew().ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("renew", args);
    }

    [Fact]
    public async Task Renew_ForCertificate_IncludesCertName()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Renew()
            .ForCertificate("example.com")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--cert-name", args);
        Assert.Contains("example.com", args);
    }

    [Fact]
    public async Task Renew_ForceRenewal_IncludesForceRenewalFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Renew()
            .ForceRenewal()
            .ExecuteAsync();

        Assert.Contains("--force-renewal", executor.CapturedArguments!);
    }

    [Fact]
    public async Task Renew_DryRun_IncludesDryRunFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Renew()
            .DryRun()
            .ExecuteAsync();

        Assert.Contains("--dry-run", executor.CapturedArguments!);
    }

    [Fact]
    public async Task Renew_WithHooks_IncludesHookFlags()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Renew()
            .WithPreHook("service nginx stop")
            .WithPostHook("service nginx start")
            .WithDeployHook("echo deployed")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--pre-hook", args);
        Assert.Contains("--post-hook", args);
        Assert.Contains("--deploy-hook", args);
    }

    [Fact]
    public async Task Renew_Quiet_IncludesQuietFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Renew()
            .Quiet()
            .ExecuteAsync();

        Assert.Contains("--quiet", executor.CapturedArguments!);
    }

    [Fact]
    public async Task Renew_ReuseKey_IncludesReuseKeyFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Renew()
            .ReuseKey()
            .ExecuteAsync();

        Assert.Contains("--reuse-key", executor.CapturedArguments!);
    }

    // ── Revoke ───────────────────────────────────────────────────────

    [Fact]
    public async Task Revoke_ByCertName_ProducesCorrectArguments()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Revoke()
            .ForCertificate("example.com")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("revoke", args);
        Assert.Contains("--cert-name", args);
        Assert.Contains("example.com", args);
    }

    [Fact]
    public async Task Revoke_ByCertPath_ProducesCorrectArguments()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Revoke()
            .ForCertificatePath("/etc/letsencrypt/live/example.com/cert.pem")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--cert-path", args);
        Assert.Contains("/etc/letsencrypt/live/example.com/cert.pem", args);
    }

    [Fact]
    public async Task Revoke_WithReason_IncludesReasonFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Revoke()
            .ForCertificate("example.com")
            .WithReason(RevocationReason.KeyCompromise)
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--reason", args);
        Assert.Contains("keycompromise", args);
    }

    [Theory]
    [InlineData(RevocationReason.Unspecified, "unspecified")]
    [InlineData(RevocationReason.KeyCompromise, "keycompromise")]
    [InlineData(RevocationReason.AffiliationChanged, "affiliationchanged")]
    [InlineData(RevocationReason.Superseded, "superseded")]
    [InlineData(RevocationReason.CessationOfOperation, "cessationofoperation")]
    public async Task Revoke_AllReasons_MapCorrectly(RevocationReason reason, string expected)
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Revoke()
            .ForCertificate("example.com")
            .WithReason(reason)
            .ExecuteAsync();

        Assert.Contains(expected, executor.CapturedArguments!);
    }

    [Fact]
    public async Task Revoke_DeleteAfterRevoke_IncludesFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Revoke()
            .ForCertificate("example.com")
            .DeleteAfterRevoke()
            .ExecuteAsync();

        Assert.Contains("--delete-after-revoke", executor.CapturedArguments!);
    }

    [Fact]
    public async Task Revoke_WithKeyPath_IncludesKeyPathFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Revoke()
            .ForCertificatePath("/etc/letsencrypt/live/example.com/cert.pem")
            .WithKeyPath("/etc/letsencrypt/live/example.com/privkey.pem")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--key-path", args);
        Assert.Contains("/etc/letsencrypt/live/example.com/privkey.pem", args);
    }

    // ── Delete ───────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ByCertName_ProducesCorrectArguments()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Delete()
            .ForCertificate("example.com")
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("delete", args);
        Assert.Contains("--cert-name", args);
        Assert.Contains("example.com", args);
    }

    // ── Certificates ─────────────────────────────────────────────────

    [Fact]
    public async Task Certificates_ProducesCorrectArguments()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certificates().ExecuteAsync();

        Assert.Contains("certificates", executor.CapturedArguments!);
    }

    // ── RenewAll ─────────────────────────────────────────────────────

    [Fact]
    public async Task RenewAll_ProducesRenewSubcommand()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.RenewAllAsync();

        Assert.Contains("renew", executor.CapturedArguments!);
    }
}
