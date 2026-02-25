using CertNET.Exceptions;
using CertNET.Execution;

namespace CertNET.Tests;

public class CertnetLocatorTests
{
    [Fact]
    public void Locate_WithNonExistentCustomPath_ThrowsCertnetNotFoundException()
    {
        var exception = Assert.Throws<CertnetNotFoundException>(
            () => CertnetLocator.Locate("/non/existent/path/certbot"));

        Assert.Contains("/non/existent/path/certbot", exception.Message);
    }

    [Fact]
    public void Locate_WithExistingCustomPath_ReturnsPath()
    {
        // Create a temp file to simulate a certbot executable
        var tempFile = Path.GetTempFileName();
        try
        {
            var result = CertnetLocator.Locate(tempFile);
            Assert.Equal(tempFile, result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Locate_WithEmptyCustomPath_AttemptsAutoDetection()
    {
        // On a system without certbot, this should throw
        // On a system with certbot, this should return the path
        // We test that it doesn't throw with null (auto-detect mode)
        // but may throw CertnetNotFoundException if certbot isn't installed
        try
        {
            var path = CertnetLocator.Locate(null);
            Assert.False(string.IsNullOrWhiteSpace(path));
        }
        catch (CertnetNotFoundException)
        {
            // Expected on systems without certbot installed
        }
    }

    [Fact]
    public void Locate_WithWhitespaceCustomPath_AttemptsAutoDetection()
    {
        try
        {
            var path = CertnetLocator.Locate("   ");
            Assert.False(string.IsNullOrWhiteSpace(path));
        }
        catch (CertnetNotFoundException)
        {
            // Expected on systems without certbot installed
        }
    }
}
