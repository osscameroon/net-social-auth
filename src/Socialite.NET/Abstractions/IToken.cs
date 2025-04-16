using System.Collections.Generic;

namespace Socialite.NET.Abstractions;

/// <summary>
/// Interface for OAuth tokens
/// </summary>
public interface IToken
{
    /// <summary>
    /// Access token
    /// </summary>
    string AccessToken { get; }
        
    /// <summary>
    /// Refresh token
    /// </summary>
    string? RefreshToken { get; }
        
    /// <summary>
    /// Token lifetime in seconds
    /// </summary>
    int ExpiresIn { get; }
        
    /// <summary>
    /// Approved scopes
    /// </summary>
    IEnumerable<string> ApprovedScopes { get; }
}