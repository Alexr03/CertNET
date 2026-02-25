namespace CertNET.Exceptions;

/// <summary>
/// Base exception type for all CertNET errors.
/// </summary>
public class CertnetException : Exception
{
    /// <summary>
    /// Creates a new <see cref="CertnetException"/> with the specified message.
    /// </summary>
    public CertnetException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new <see cref="CertnetException"/> with the specified message and inner exception.
    /// </summary>
    public CertnetException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
