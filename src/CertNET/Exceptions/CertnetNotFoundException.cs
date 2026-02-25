namespace CertNET.Exceptions;

/// <summary>
/// Thrown when the certbot executable cannot be found on the system.
/// </summary>
public class CertnetNotFoundException : CertnetException
{
    /// <summary>
    /// Creates a new <see cref="CertnetNotFoundException"/> with the specified message.
    /// </summary>
    public CertnetNotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new <see cref="CertnetNotFoundException"/> with the specified message and inner exception.
    /// </summary>
    public CertnetNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
