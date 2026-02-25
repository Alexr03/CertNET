using CertNET.Plugins;

namespace CertNET.Tests;

public class DnsPluginBaseTests
{
    /// <summary>
    /// A minimal test implementation of DnsPluginBase for testing the base class behavior.
    /// </summary>
    private class TestDnsPlugin : DnsPluginBase
    {
        public override string AuthenticatorName => "dns-test";
        public override string? CertbotPipPackage => "certbot-dns-test";
        public override int DefaultPropagationSeconds => 30;

        public string CredentialContent { get; set; } = "test_api_key = abc123";

        protected override string GetCredentialFileContent() => CredentialContent;

        public override void Validate()
        {
            // No-op for testing
        }
    }

    [Fact]
    public void WriteCredentialsFile_CreatesFileOnDisk()
    {
        using var plugin = new TestDnsPlugin();
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
    public void WriteCredentialsFile_FileContainsExpectedContent()
    {
        using var plugin = new TestDnsPlugin { CredentialContent = "my_secret = 12345" };
        var path = plugin.WriteCredentialsFile();
        try
        {
            var content = File.ReadAllText(path);
            Assert.Equal("my_secret = 12345", content);
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    [Fact]
    public void WriteCredentialsFile_PathIsInTempDirectory()
    {
        using var plugin = new TestDnsPlugin();
        var path = plugin.WriteCredentialsFile();
        try
        {
            Assert.StartsWith(Path.GetTempPath(), path);
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    [Fact]
    public void WriteCredentialsFile_PathContainsCertnetPrefix()
    {
        using var plugin = new TestDnsPlugin();
        var path = plugin.WriteCredentialsFile();
        try
        {
            Assert.Contains("certnet-", Path.GetFileName(path));
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    [Fact]
    public void WriteCredentialsFile_PathHasIniExtension()
    {
        using var plugin = new TestDnsPlugin();
        var path = plugin.WriteCredentialsFile();
        try
        {
            Assert.EndsWith(".ini", path);
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    [Fact]
    public void CleanupCredentialsFile_RemovesFile()
    {
        using var plugin = new TestDnsPlugin();
        var path = plugin.WriteCredentialsFile();
        Assert.True(File.Exists(path));

        plugin.CleanupCredentialsFile();
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void CleanupCredentialsFile_CalledTwice_DoesNotThrow()
    {
        using var plugin = new TestDnsPlugin();
        plugin.WriteCredentialsFile();
        plugin.CleanupCredentialsFile();
        plugin.CleanupCredentialsFile(); // Second call should not throw
    }

    [Fact]
    public void CleanupCredentialsFile_WithoutWrite_DoesNotThrow()
    {
        using var plugin = new TestDnsPlugin();
        plugin.CleanupCredentialsFile(); // Should not throw
    }

    [Fact]
    public void Dispose_CleansUpFile()
    {
        string path;
        using (var plugin = new TestDnsPlugin())
        {
            path = plugin.WriteCredentialsFile();
            Assert.True(File.Exists(path));
        }

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void GetArguments_IncludesCredentialsFlag()
    {
        using var plugin = new TestDnsPlugin();
        var path = plugin.WriteCredentialsFile();
        try
        {
            var args = plugin.GetArguments().ToList();
            Assert.Contains("--dns-test-credentials", args);
            Assert.Contains(path, args);
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    [Fact]
    public void GetArguments_IncludesPropagationSeconds_Default()
    {
        using var plugin = new TestDnsPlugin();
        plugin.WriteCredentialsFile();
        try
        {
            var args = plugin.GetArguments().ToList();
            Assert.Contains("--dns-test-propagation-seconds", args);
            Assert.Contains("30", args); // DefaultPropagationSeconds
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    [Fact]
    public void GetArguments_IncludesPropagationSeconds_Custom()
    {
        using var plugin = new TestDnsPlugin { PropagationSeconds = 120 };
        plugin.WriteCredentialsFile();
        try
        {
            var args = plugin.GetArguments().ToList();
            Assert.Contains("--dns-test-propagation-seconds", args);
            Assert.Contains("120", args);
        }
        finally
        {
            plugin.CleanupCredentialsFile();
        }
    }

    [Fact]
    public void GetArguments_WithoutWrite_ExcludesCredentialsFlag()
    {
        using var plugin = new TestDnsPlugin();
        var args = plugin.GetArguments().ToList();

        // Should still include propagation but NOT credentials (no file written)
        Assert.DoesNotContain("--dns-test-credentials", args);
        Assert.Contains("--dns-test-propagation-seconds", args);
    }
}
