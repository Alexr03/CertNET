namespace CertNET.Exceptions;

/// <summary>
/// Thrown when a certbot plugin's configuration is invalid.
/// For example, when required credentials are missing or conflicting options are provided.
/// </summary>
public class PluginValidationException : CertnetException
{
    /// <summary>
    /// Creates a new <see cref="PluginValidationException"/> with the specified message.
    /// </summary>
    public PluginValidationException(string message) : base(message)
    {
    }
}
