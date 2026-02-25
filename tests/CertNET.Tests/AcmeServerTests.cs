namespace CertNET.Tests;

public class AcmeServerTests
{
    [Fact]
    public void Production_ReturnsCorrectUrl()
    {
        var options = new CertnetClientOptions { Server = AcmeServer.Production };
        Assert.Equal("https://acme-v02.api.letsencrypt.org/directory", options.GetServerUrl());
    }

    [Fact]
    public void Staging_ReturnsCorrectUrl()
    {
        var options = new CertnetClientOptions { Server = AcmeServer.Staging };
        Assert.Equal("https://acme-staging-v02.api.letsencrypt.org/directory", options.GetServerUrl());
    }

    [Fact]
    public void DefaultServer_IsProduction()
    {
        var options = new CertnetClientOptions();
        Assert.Equal(AcmeServer.Production, options.Server);
    }

    [Fact]
    public void DefaultNonInteractive_IsTrue()
    {
        var options = new CertnetClientOptions();
        Assert.True(options.NonInteractive);
    }
}
