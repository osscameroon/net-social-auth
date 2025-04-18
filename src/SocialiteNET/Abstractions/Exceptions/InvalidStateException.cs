using System;

namespace SocialiteNET.Abstractions.Exceptions;

/// <summary>
/// Exception thrown when OAuth state is invalid
/// </summary>
public class InvalidStateException : Exception
{
    /// <summary>
    /// Creates a new instance of the InvalidStateException
    /// </summary>
    public InvalidStateException()
        : base("Invalid state parameter. The request may have been tampered with.")
    {
    }

    /// <summary>
    /// Creates a new instance of the InvalidStateException with a message
    /// </summary>
    /// <param name="message">Exception message</param>
    public InvalidStateException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the InvalidStateException with a message and inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidStateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}