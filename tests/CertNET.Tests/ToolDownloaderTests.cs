using CertNET.Execution;

namespace CertNET.Tests;

public class ToolDownloaderTests
{
    // ── Path resolution ──────────────────────────────────────────────

    [Fact]
    public void GetToolsBaseDir_ReturnsNonEmptyPath()
    {
        var baseDir = ToolDownloader.GetToolsBaseDir();
        Assert.False(string.IsNullOrWhiteSpace(baseDir));
        Assert.Contains("certnet", baseDir, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tools", baseDir, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetWinAcmeInstallDir_IsSubdirectoryOfToolsBase()
    {
        var baseDir = ToolDownloader.GetToolsBaseDir();
        var winAcmeDir = ToolDownloader.GetWinAcmeInstallDir();
        Assert.StartsWith(baseDir, winAcmeDir);
        Assert.Contains("win-acme", winAcmeDir, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetWinAcmeExecutablePath_EndsWithWacsExe()
    {
        var path = ToolDownloader.GetWinAcmeExecutablePath();
        Assert.EndsWith("wacs.exe", path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetCertbotInstallDir_IsSubdirectoryOfToolsBase()
    {
        var baseDir = ToolDownloader.GetToolsBaseDir();
        var certbotDir = ToolDownloader.GetCertbotInstallDir();
        Assert.StartsWith(baseDir, certbotDir);
        Assert.Contains("certbot", certbotDir, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetCertbotExecutablePath_ContainsCertbot()
    {
        var path = ToolDownloader.GetCertbotExecutablePath();
        Assert.Contains("certbot", path, StringComparison.OrdinalIgnoreCase);
    }

    // ── Download URL ─────────────────────────────────────────────────

    [Fact]
    public void GetWinAcmeDownloadUrl_ContainsPinnedVersion()
    {
        var url = ToolDownloader.GetWinAcmeDownloadUrl();
        Assert.Contains(ToolDownloader.WinAcmeVersion, url);
        Assert.StartsWith("https://", url);
        Assert.EndsWith(".zip", url);
    }

    [Fact]
    public void GetWinAcmeDownloadUrl_PointsToGitHub()
    {
        var url = ToolDownloader.GetWinAcmeDownloadUrl();
        Assert.Contains("github.com/win-acme/win-acme", url);
    }

    [Fact]
    public void GetWinAcmeDownloadUrl_IsPluggableVariant()
    {
        var url = ToolDownloader.GetWinAcmeDownloadUrl();
        Assert.Contains("pluggable", url);
    }

    // ── Version management ───────────────────────────────────────────

    [Fact]
    public void IsVersionOutdated_ReturnsTrueWhenVersionFileMissing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"certnet-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            Assert.True(ToolDownloader.IsVersionOutdated(tempDir, "1.0.0"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void IsVersionOutdated_ReturnsFalseWhenVersionMatches()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"certnet-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(ToolDownloader.GetVersionFilePath(tempDir), "2.2.9.1701");
            Assert.False(ToolDownloader.IsVersionOutdated(tempDir, "2.2.9.1701"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void IsVersionOutdated_ReturnsTrueWhenVersionDiffers()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"certnet-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(ToolDownloader.GetVersionFilePath(tempDir), "2.2.8.1500");
            Assert.True(ToolDownloader.IsVersionOutdated(tempDir, "2.2.9.1701"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void IsVersionOutdated_IsCaseInsensitive()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"certnet-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(ToolDownloader.GetVersionFilePath(tempDir), "V1.0.0");
            Assert.False(ToolDownloader.IsVersionOutdated(tempDir, "v1.0.0"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void IsVersionOutdated_HandlesWhitespace()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"certnet-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(ToolDownloader.GetVersionFilePath(tempDir), "  1.0.0  \n");
            Assert.False(ToolDownloader.IsVersionOutdated(tempDir, "1.0.0"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetVersionFilePath_ReturnsPathInInstallDir()
    {
        var dir = @"C:\some\path";
        var versionPath = ToolDownloader.GetVersionFilePath(dir);
        Assert.StartsWith(dir, versionPath);
        Assert.EndsWith("version.txt", versionPath);
    }

    // ── Plugin download URL ────────────────────────────────────────

    [Fact]
    public void GetWinAcmePluginDownloadUrl_ContainsPluginIdAndVersion()
    {
        var url = ToolDownloader.GetWinAcmePluginDownloadUrl("plugin.validation.dns.cloudflare");
        Assert.Contains("plugin.validation.dns.cloudflare", url);
        Assert.Contains(ToolDownloader.WinAcmeVersion, url);
        Assert.StartsWith("https://", url);
        Assert.EndsWith(".zip", url);
    }

    [Fact]
    public void GetWinAcmePluginDownloadUrl_PointsToGitHub()
    {
        var url = ToolDownloader.GetWinAcmePluginDownloadUrl("plugin.validation.dns.cloudflare");
        Assert.Contains("github.com/win-acme/win-acme", url);
    }

    [Fact]
    public void GetWinAcmePluginDownloadUrl_DoesNotContainPluggable()
    {
        // Plugin zips are separate from the main pluggable zip
        var url = ToolDownloader.GetWinAcmePluginDownloadUrl("plugin.validation.dns.cloudflare");
        Assert.DoesNotContain("pluggable", url);
    }

    // ── Plugin marker files ──────────────────────────────────────────

    [Fact]
    public void GetPluginMarkerFilePath_ContainsPluginId()
    {
        var dir = @"C:\some\path";
        var markerPath = ToolDownloader.GetPluginMarkerFilePath(dir, "plugin.validation.dns.cloudflare");
        Assert.StartsWith(dir, markerPath);
        Assert.Contains("plugin.validation.dns.cloudflare", markerPath);
        Assert.EndsWith(".installed", markerPath);
    }

    [Fact]
    public void IsPluginMarkerOutdated_ReturnsTrueWhenMissing()
    {
        var fakePath = Path.Combine(Path.GetTempPath(), $"certnet-test-{Guid.NewGuid():N}.installed");
        Assert.True(ToolDownloader.IsPluginMarkerOutdated(fakePath, "1.0.0"));
    }

    [Fact]
    public void IsPluginMarkerOutdated_ReturnsFalseWhenCurrent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"certnet-test-{Guid.NewGuid():N}.installed");
        try
        {
            File.WriteAllText(tempFile, "2.2.9.1701");
            Assert.False(ToolDownloader.IsPluginMarkerOutdated(tempFile, "2.2.9.1701"));
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    [Fact]
    public void IsPluginMarkerOutdated_ReturnsTrueWhenOlder()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"certnet-test-{Guid.NewGuid():N}.installed");
        try
        {
            File.WriteAllText(tempFile, "2.2.8.1500");
            Assert.True(ToolDownloader.IsPluginMarkerOutdated(tempFile, "2.2.9.1701"));
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Constants ────────────────────────────────────────────────────

    [Fact]
    public void WinAcmeVersion_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(ToolDownloader.WinAcmeVersion));
    }

    [Fact]
    public void CertbotVersion_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(ToolDownloader.CertbotVersion));
    }

    [Fact]
    public void MinZipSizeBytes_IsOneMB()
    {
        Assert.Equal(1_048_576, ToolDownloader.MinZipSizeBytes);
    }

    [Fact]
    public void MaxZipSizeBytes_IsOneHundredMB()
    {
        Assert.Equal(104_857_600, ToolDownloader.MaxZipSizeBytes);
    }
}
