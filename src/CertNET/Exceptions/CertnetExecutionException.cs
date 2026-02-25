using CertNET.Execution;

namespace CertNET.Exceptions;

/// <summary>
/// Thrown when a certbot command fails (returns a non-zero exit code).
/// </summary>
public class CertnetExecutionException : CertnetException
{
    /// <summary>
    /// The full result of the failed certbot execution, including stdout, stderr, and exit code.
    /// </summary>
    public CertnetResult Result { get; }

    /// <summary>
    /// Creates a new <see cref="CertnetExecutionException"/> with the specified message and result.
    /// </summary>
    public CertnetExecutionException(string message, CertnetResult result)
        : base(message)
    {
        Result = result;
    }
}
