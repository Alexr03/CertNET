using CertNET.Exceptions;
using CertNET.Plugins.Cloudflare;
using CertNET.Tests.Helpers;

namespace CertNET.Tests;

public class CloudflarePluginTests
{
    // ── Authentication Name ──────────────────────────────────────────

    [Fact]
    public void AuthenticatorName_IsDnsCloudflare()
    {
        var plugin = new CloudflarePlugin { ApiToken = "test-token" };
        Assert.Equal("dns-cloudflare", plugin.AuthenticatorName);
    }

    // ── Validation ───────────────────────────────────────────────────

    [Fact]
    public void Validate_NoCredentials_Throws()
    {
        var plugin = new CloudflarePlugin();
        Assert.Throws<PluginValidationException>(() => plugin.Validate());
    }

    [Fact]
    public void Validate_BothTokenAndKey_Throws()
    {
        var plugin = new CloudflarePlugin
        {
            ApiToken = "some-token",
            ApiKey = "some-key",
            Email = "test@example.com"
        };
        Assert.Throws<PluginValidationException>(() => plugin.Validate());
    }

    [Fact]
    public void Validate_ApiKeyWithoutEmail_Throws()
    {
        var plugin = new CloudflarePlugin { ApiKey = "some-key" };
        Assert.Throws<PluginValidationException>(() => plugin.Validate());
    }

    [Fact]
    public void Validate_ApiTokenOnly_DoesNotThrow()
    {
        var plugin = new CloudflarePlugin { ApiToken = "some-token" };
        plugin.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_ApiKeyWithEmail_DoesNotThrow()
    {
        var plugin = new CloudflarePlugin
        {
            ApiKey = "some-key",
            Email = "test@example.com"
        };
        plugin.Validate(); // Should not throw
    }

    // ── Credential File Content ──────────────────────────────────────

    [Fact]
    public void CredentialFile_ApiToken_WritesTokenFormat()
    {
        var plugin = new CloudflarePlugin { ApiToken = "my-api-token-123" };

        var path = plugin.WriteCredentialsFile();
        try
        {
            var content = File.ReadAllText(path);
            Assert.Contains("dns_cloudflare_api_token = my-api-token-123", content);
            Assert.DoesNotContain("dns_cloudflare_email", content);
            Assert.DoesNotContain("dns_cloudflare_api_key", content);
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    [Fact]
    public void CredentialFile_ApiKey_WritesKeyAndEmailFormat()
    {
        var plugin = new CloudflarePlugin
        {
            ApiKey = "my-global-key",
            Email = "admin@example.com"
        };

        var path = plugin.WriteCredentialsFile();
        try
        {
            var content = File.ReadAllText(path);
            Assert.Contains("dns_cloudflare_email = admin@example.com", content);
            Assert.Contains("dns_cloudflare_api_key = my-global-key", content);
            Assert.DoesNotContain("dns_cloudflare_api_token", content);
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    // ── Temp File Lifecycle ──────────────────────────────────────────

    [Fact]
    public void WriteCredentialsFile_CreatesFile()
    {
        var plugin = new CloudflarePlugin { ApiToken = "test" };
        var path = plugin.WriteCredentialsFile();
        try
        {
            Assert.True(File.Exists(path));
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    [Fact]
    public void CleanupCredentialsFile_DeletesFile()
    {
        var plugin = new CloudflarePlugin { ApiToken = "test" };
        var path = plugin.WriteCredentialsFile();
        Assert.True(File.Exists(path));

        plugin.CleanupCredentialsFile();
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Dispose_CleansUpCredentialsFile()
    {
        string path;
        using (var plugin = new CloudflarePlugin { ApiToken = "test" })
        {
            path = plugin.WriteCredentialsFile();
            Assert.True(File.Exists(path));
        }

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void CleanupCredentialsFile_WhenNoFileWritten_DoesNotThrow()
    {
        var plugin = new CloudflarePlugin { ApiToken = "test" };
        plugin.CleanupCredentialsFile(); // Should not throw
    }

    // ── Arguments ────────────────────────────────────────────────────

    [Fact]
    public void GetArguments_IncludesPropagationSeconds()
    {
        var plugin = new CloudflarePlugin
        {
            ApiToken = "test",
            PropagationSeconds = 60
        };

        plugin.WriteCredentialsFile();
        try
        {
            var args = plugin.GetArguments().ToList();
            Assert.Contains("--dns-cloudflare-propagation-seconds", args);
            Assert.Contains("60", args);
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    [Fact]
    public void GetArguments_DefaultPropagation_Is10()
    {
        var plugin = new CloudflarePlugin { ApiToken = "test" };
        Assert.Equal(10, plugin.DefaultPropagationSeconds);

        plugin.WriteCredentialsFile();
        try
        {
            var args = plugin.GetArguments().ToList();
            Assert.Contains("--dns-cloudflare-propagation-seconds", args);
            Assert.Contains("10", args);
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    [Fact]
    public void GetArguments_IncludesCredentialsPath()
    {
        var plugin = new CloudflarePlugin { ApiToken = "test" };

        var path = plugin.WriteCredentialsFile();
        try
        {
            var args = plugin.GetArguments().ToList();
            Assert.Contains("--dns-cloudflare-credentials", args);
            Assert.Contains(path, args);
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    // ── Integration with command builder ─────────────────────────────

    [Fact]
    public async Task Certonly_WithCloudflare_IncludesAuthenticatorFlag()
    {
        var (client, executor) = TestDefaults.CreateClient();

        await client.Certonly()
            .ForDomains("example.com", "*.example.com")
            .WithPlugin(new CloudflarePlugin
            {
                ApiToken = "test-token-123",
                PropagationSeconds = 30
            })
            .ExecuteAsync();

        var args = executor.CapturedArguments!;
        Assert.Contains("--authenticator", args);
        Assert.Contains("dns-cloudflare", args);
        Assert.Contains("--dns-cloudflare-propagation-seconds", args);
        Assert.Contains("30", args);
        Assert.Contains("--dns-cloudflare-credentials", args);
    }

    [Fact]
    public async Task Certonly_WithCloudflare_CleansUpCredentialFile()
    {
        var (client, executor) = TestDefaults.CreateClient();

        var plugin = new CloudflarePlugin { ApiToken = "test-token-123" };

        await client.Certonly()
            .ForDomains("example.com")
            .WithPlugin(plugin)
            .ExecuteAsync();

        // After execution, the credentials file path should be in the args
        var credPath = executor.CapturedArguments!
            .SkipWhile(a => a != "--dns-cloudflare-credentials")
            .Skip(1)
            .FirstOrDefault();

        Assert.NotNull(credPath);
        // The file should have been cleaned up after execution
        Assert.False(File.Exists(credPath));
    }
}
