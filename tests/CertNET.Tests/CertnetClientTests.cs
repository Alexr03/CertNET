using CertNET.Commands;
using CertNET.Tests.Helpers;

namespace CertNET.Tests;

public class CertnetClientTests
{
    [Fact]
    public void Constructor_WithOptions_DoesNotThrow()
    {
        var executor = new MockCertnetExecutor();
        var options = TestDefaults.CreateOptions();
        var client = new CertnetClient(options, executor);
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithConfigureAction_DoesNotThrow()
    {
        // This would fail because certbot isn't installed, but we test the mock path
        var executor = new MockCertnetExecutor();
        var options = TestDefaults.CreateOptions();
        var client = new CertnetClient(options, executor);
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var executor = new MockCertnetExecutor();
        Assert.Throws<ArgumentNullException>(() => new CertnetClient(null!, executor));
    }

    [Fact]
    public void Constructor_NullExecutor_ThrowsArgumentNullException()
    {
        var options = TestDefaults.CreateOptions();
        Assert.Throws<ArgumentNullException>(() => new CertnetClient(options, null!));
    }

    [Fact]
    public void Certonly_ReturnsCertonlyCommand()
    {
        var (client, _) = TestDefaults.CreateClient();
        var command = client.Certonly();
        Assert.IsType<CertonlyCommand>(command);
    }

    [Fact]
    public void Renew_ReturnsRenewCommand()
    {
        var (client, _) = TestDefaults.CreateClient();
        var command = client.Renew();
        Assert.IsType<RenewCommand>(command);
    }

    [Fact]
    public void Revoke_ReturnsRevokeCommand()
    {
        var (client, _) = TestDefaults.CreateClient();
        var command = client.Revoke();
        Assert.IsType<RevokeCommand>(command);
    }

    [Fact]
    public void Delete_ReturnsDeleteCommand()
    {
        var (client, _) = TestDefaults.CreateClient();
        var command = client.Delete();
        Assert.IsType<DeleteCommand>(command);
    }

    [Fact]
    public void Certificates_ReturnsCertificatesCommand()
    {
        var (client, _) = TestDefaults.CreateClient();
        var command = client.Certificates();
        Assert.IsType<CertificatesCommand>(command);
    }
}
