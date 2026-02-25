using CertNET.Parsing;

namespace CertNET.Tests;

public class CertificatesParserTests
{
    private const string SingleCertOutput = """
        Found the following certificates:
          Certificate Name: example.com
            Serial Number: 2ce74f4e2b822211dd7648ea26c8927bdef6
            Key Type: ECDSA
            Domains: example.com www.example.com
            Expiry Date: 2025-11-16 20:27:27+00:00 (VALID: 30 days)
            Certificate Path: /etc/letsencrypt/live/example.com/fullchain.pem
            Private Key Path: /etc/letsencrypt/live/example.com/privkey.pem
        """;

    private const string MultipleCertOutput = """
        Found the following certificates:
          Certificate Name: example.com
            Serial Number: 2ce74f4e2b822211dd7648ea26c8927bdef6
            Key Type: ECDSA
            Domains: example.com www.example.com
            Expiry Date: 2025-11-16 20:27:27+00:00 (VALID: 30 days)
            Certificate Path: /etc/letsencrypt/live/example.com/fullchain.pem
            Private Key Path: /etc/letsencrypt/live/example.com/privkey.pem
          Certificate Name: api.example.com
            Serial Number: 3af85c1d2a933322ee8759fb37d9a038cef7
            Key Type: RSA
            Domains: api.example.com
            Expiry Date: 2025-12-01 10:00:00+00:00 (VALID: 45 days)
            Certificate Path: /etc/letsencrypt/live/api.example.com/fullchain.pem
            Private Key Path: /etc/letsencrypt/live/api.example.com/privkey.pem
        """;

    private const string TestCertOutput = """
        Found the following certificates:
          Certificate Name: test.example.com
            Serial Number: 1ab23c4d5e6f7890
            Key Type: ECDSA
            Domains: test.example.com
            Expiry Date: 2025-11-16 20:27:27+00:00 (INVALID: TEST_CERT)
            Certificate Path: /etc/letsencrypt/live/test.example.com/fullchain.pem
            Private Key Path: /etc/letsencrypt/live/test.example.com/privkey.pem
        """;

    private const string NoCertsOutput = """
        No certificates found.
        """;

    [Fact]
    public void Parse_SingleCertificate_ReturnsOneCert()
    {
        var result = CertificatesParser.Parse(SingleCertOutput);
        Assert.Single(result);
    }

    [Fact]
    public void Parse_SingleCertificate_CorrectName()
    {
        var result = CertificatesParser.Parse(SingleCertOutput);
        Assert.Equal("example.com", result[0].CertificateName);
    }

    [Fact]
    public void Parse_SingleCertificate_CorrectDomains()
    {
        var result = CertificatesParser.Parse(SingleCertOutput);
        Assert.Equal(["example.com", "www.example.com"], result[0].Domains);
    }

    [Fact]
    public void Parse_SingleCertificate_CorrectKeyType()
    {
        var result = CertificatesParser.Parse(SingleCertOutput);
        Assert.Equal(KeyType.Ecdsa, result[0].KeyType);
    }

    [Fact]
    public void Parse_SingleCertificate_CorrectPaths()
    {
        var result = CertificatesParser.Parse(SingleCertOutput);
        Assert.Equal("/etc/letsencrypt/live/example.com/fullchain.pem", result[0].CertificatePath);
        Assert.Equal("/etc/letsencrypt/live/example.com/privkey.pem", result[0].PrivateKeyPath);
    }

    [Fact]
    public void Parse_SingleCertificate_CorrectSerialNumber()
    {
        var result = CertificatesParser.Parse(SingleCertOutput);
        Assert.Equal("2ce74f4e2b822211dd7648ea26c8927bdef6", result[0].SerialNumber);
    }

    [Fact]
    public void Parse_SingleCertificate_IsValid()
    {
        var result = CertificatesParser.Parse(SingleCertOutput);
        Assert.True(result[0].IsValid);
    }

    [Fact]
    public void Parse_SingleCertificate_CorrectExpiryDate()
    {
        var result = CertificatesParser.Parse(SingleCertOutput);
        var expected = new DateTimeOffset(2025, 11, 16, 20, 27, 27, TimeSpan.Zero);
        Assert.Equal(expected, result[0].ExpiryDate);
    }

    [Fact]
    public void Parse_MultipleCertificates_ReturnsBoth()
    {
        var result = CertificatesParser.Parse(MultipleCertOutput);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Parse_MultipleCertificates_CorrectNames()
    {
        var result = CertificatesParser.Parse(MultipleCertOutput);
        Assert.Equal("example.com", result[0].CertificateName);
        Assert.Equal("api.example.com", result[1].CertificateName);
    }

    [Fact]
    public void Parse_MultipleCertificates_DifferentKeyTypes()
    {
        var result = CertificatesParser.Parse(MultipleCertOutput);
        Assert.Equal(KeyType.Ecdsa, result[0].KeyType);
        Assert.Equal(KeyType.Rsa, result[1].KeyType);
    }

    [Fact]
    public void Parse_TestCertificate_IsInvalid()
    {
        var result = CertificatesParser.Parse(TestCertOutput);
        Assert.Single(result);
        Assert.False(result[0].IsValid);
    }

    [Fact]
    public void Parse_NoCertificates_ReturnsEmpty()
    {
        var result = CertificatesParser.Parse(NoCertsOutput);
        Assert.Empty(result);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        var result = CertificatesParser.Parse("");
        Assert.Empty(result);
    }
}
