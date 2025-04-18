using System;

namespace SocialiteNET.Abstractions.Exceptions;

/// <summary>
/// Exception thrown when authentication fails
/// </summary>
public class AuthenticationException : Exception
{
    /// <summary>
    /// Creates a new instance of the AuthenticationException
    /// </summary>
    public AuthenticationException()
        : base("Authentication failed")
    {
    }

    /// <summary>
    /// Creates a new instance of the AuthenticationException with a message
    /// </summary>
    /// <param name="message">Exception message</param>
    public AuthenticationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the AuthenticationException with a message and inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="innerException">Inner exception</param>
    public AuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}