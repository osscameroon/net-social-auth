using System;
using System.Collections.Generic;
using System.Linq;
using Socialite.NET.Abstractions;

namespace Socialite.NET.Core;

/// <summary>
/// Default implementation of OAuth tokens
/// </summary>
public class Token : IToken
{
    /// <inheritdoc />
    public string AccessToken { get; }
        
    /// <inheritdoc />
    public string? RefreshToken { get; }
        
    /// <inheritdoc />
    public int ExpiresIn { get; }
        
    /// <inheritdoc />
    public IEnumerable<string> ApprovedScopes { get; }
        
    /// <summary>
    /// Creates a new instance of Token
    /// </summary>
    /// <param name="accessToken">Access token</param>
    /// <param name="refreshToken">Refresh token</param>
    /// <param name="expiresIn">Expiration time in seconds</param>
    /// <param name="approvedScopes">Approved scopes</param>
    /// <exception cref="ArgumentNullException">Thrown when accessToken is null</exception>
    public Token(
        string accessToken, 
        string? refreshToken, 
        int expiresIn, 
        IEnumerable<string>? approvedScopes)
    {
        if (string.IsNullOrEmpty(accessToken))
            throw new ArgumentNullException(nameof(accessToken));
                
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresIn = expiresIn;
        ApprovedScopes = approvedScopes?.ToArray() ?? [];
    }
}