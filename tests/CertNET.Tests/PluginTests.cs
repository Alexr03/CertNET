using CertNET.Exceptions;
using CertNET.Plugins;
using CertNET.Tests.Helpers;

namespace CertNET.Tests;

public class PluginTests
{
    // ── Standalone ───────────────────────────────────────────────────

    [Fact]
    public void StandalonePlugin_AuthenticatorName_IsStandalone()
    {
        var plugin = new StandalonePlugin();
        Assert.Equal("standalone", plugin.AuthenticatorName);
    }

    [Fact]
    public void StandalonePlugin_DefaultArguments_AreEmpty()
    {
        var plugin = new StandalonePlugin();
        Assert.Empty(plugin.GetArguments());
    }

    [Fact]
    public void StandalonePlugin_WithPort_IncludesPortFlag()
    {
        var plugin = new StandalonePlugin { HttpPort = 8080 };
        var args = plugin.GetArguments().ToList();
        Assert.Contains("--http-01-port", args);
        Assert.Contains("8080", args);
    }

    [Fact]
    public void StandalonePlugin_WithAddress_IncludesAddressFlag()
    {
        var plugin = new StandalonePlugin { HttpAddress = "0.0.0.0" };
        var args = plugin.GetArguments().ToList();
        Assert.Contains("--http-01-address", args);
        Assert.Contains("0.0.0.0", args);
    }

    [Fact]
    public void StandalonePlugin_InvalidPort_ThrowsValidationException()
    {
        var plugin = new StandalonePlugin { HttpPort = 99999 };
        Assert.Throws<PluginValidationException>(() => plugin.Validate());
    }

    [Fact]
    public void StandalonePlugin_ValidPort_DoesNotThrow()
    {
        var plugin = new StandalonePlugin { HttpPort = 80 };
        plugin.Validate(); // Should not throw
    }

    // ── Webroot ──────────────────────────────────────────────────────

    [Fact]
    public void WebrootPlugin_AuthenticatorName_IsWebroot()
    {
        var plugin = new WebrootPlugin { WebRootPath = "/var/www/html" };
        Assert.Equal("webroot", plugin.AuthenticatorName);
    }

    [Fact]
    public void WebrootPlugin_IncludesWebRootPath()
    {
        var plugin = new WebrootPlugin { WebRootPath = "/var/www/html" };
        var args = plugin.GetArguments().ToList();
        Assert.Contains("--webroot-path", args);
        Assert.Contains("/var/www/html", args);
    }

    [Fact]
    public void WebrootPlugin_EmptyPath_ThrowsValidationException()
    {
        var plugin = new WebrootPlugin { WebRootPath = "" };
        Assert.Throws<PluginValidationException>(() => plugin.Validate());
    }

    // ── Manual ───────────────────────────────────────────────────────

    [Fact]
    public void ManualPlugin_AuthenticatorName_IsManual()
    {
        var plugin = new ManualPlugin();
        Assert.Equal("manual", plugin.AuthenticatorName);
    }

    [Fact]
    public void ManualPlugin_HttpChallenge_IncludesHttpPreference()
    {
        var plugin = new ManualPlugin { PreferredChallenge = ChallengeType.Http01 };
        var args = plugin.GetArguments().ToList();
        Assert.Contains("--preferred-challenges", args);
        Assert.Contains("http", args);
    }

    [Fact]
    public void ManualPlugin_DnsChallenge_IncludesDnsPreference()
    {
        var plugin = new ManualPlugin { PreferredChallenge = ChallengeType.Dns01 };
        var args = plugin.GetArguments().ToList();
        Assert.Contains("--preferred-challenges", args);
        Assert.Contains("dns", args);
    }

    [Fact]
    public void ManualPlugin_WithHooks_IncludesHookFlags()
    {
        var plugin = new ManualPlugin
        {
            AuthHook = "/scripts/auth.sh",
            CleanupHook = "/scripts/cleanup.sh"
        };
        var args = plugin.GetArguments().ToList();
        Assert.Contains("--manual-auth-hook", args);
        Assert.Contains("/scripts/auth.sh", args);
        Assert.Contains("--manual-cleanup-hook", args);
        Assert.Contains("/scripts/cleanup.sh", args);
    }

    // ── Nginx ────────────────────────────────────────────────────────

    [Fact]
    public void NginxPlugin_AuthenticatorName_IsNginx()
    {
        var plugin = new NginxPlugin();
        Assert.Equal("nginx", plugin.AuthenticatorName);
    }

    [Fact]
    public void NginxPlugin_WithServerRoot_IncludesServerRootFlag()
    {
        var plugin = new NginxPlugin { ServerRoot = "/etc/nginx" };
        var args = plugin.GetArguments().ToList();
        Assert.Contains("--nginx-server-root", args);
        Assert.Contains("/etc/nginx", args);
    }

    [Fact]
    public void NginxPlugin_WithCtl_IncludesCtlFlag()
    {
        var plugin = new NginxPlugin { NginxCtl = "/usr/sbin/nginx" };
        var args = plugin.GetArguments().ToList();
        Assert.Contains("--nginx-ctl", args);
        Assert.Contains("/usr/sbin/nginx", args);
    }

    // ── Apache ───────────────────────────────────────────────────────

    [Fact]
    public void ApachePlugin_AuthenticatorName_IsApache()
    {
        var plugin = new ApachePlugin();
        Assert.Equal("apache", plugin.AuthenticatorName);
    }

    [Fact]
    public void ApachePlugin_WithServerRoot_IncludesServerRootFlag()
    {
        var plugin = new ApachePlugin { ServerRoot = "/etc/apache2" };
        var args = plugin.GetArguments().ToList();
        Assert.Contains("--apache-server-root", args);
        Assert.Contains("/etc/apache2", args);
    }

    [Fact]
    public void ApachePlugin_WithCtl_IncludesCtlFlag()
    {
        var plugin = new ApachePlugin { ApacheCtl = "/usr/sbin/apache2ctl" };
        var args = plugin.GetArguments().ToList();
        Assert.Contains("--apache-ctl", args);
        Assert.Contains("/usr/sbin/apache2ctl", args);
    }

    // ── Plugin integration with command builder ──────────────────────

    [Fact]
    public async Task Command_WithStandalonePlugin_IncludesAuthenticatorFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin<StandalonePlugin>()
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--authenticator", args);
        Assert.Contains("standalone", args);
    }

    [Fact]
    public async Task Command_WithWebrootPlugin_IncludesAuthenticatorAndPath()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin(new WebrootPlugin { WebRootPath = "/var/www" })
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--authenticator", args);
        Assert.Contains("webroot", args);
        Assert.Contains("--webroot-path", args);
        Assert.Contains("/var/www", args);
    }

    [Fact]
    public async Task Command_WithConfiguredStandalone_IncludesPluginArgs()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomain("example.com")
            .WithPlugin(new StandalonePlugin
            {
                HttpPort = 8080,
                HttpAddress = "127.0.0.1"
            })
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--http-01-port", args);
        Assert.Contains("8080", args);
        Assert.Contains("--http-01-address", args);
        Assert.Contains("127.0.0.1", args);
    }
}
