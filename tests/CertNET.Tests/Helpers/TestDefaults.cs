namespace CertNET.Tests.Helpers;

/// <summary>
/// Common test defaults and factory methods.
/// </summary>
public static class TestDefaults
{
    public static CertnetClientOptions CreateOptions(AcmeServer server = AcmeServer.Staging) => new()
    {
        Server = server,
        Email = "test@example.com",
        AgreeToTermsOfService = true,
        NonInteractive = true
    };

    public static (CertnetClient Client, MockCertnetExecutor Executor) CreateClient(
        AcmeServer server = AcmeServer.Staging)
    {
        var options = CreateOptions(server);
        var executor = new MockCertnetExecutor();
        var client = new CertnetClient(options, executor);
        return (client, executor);
    }
}
