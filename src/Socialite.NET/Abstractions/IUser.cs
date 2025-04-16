using System.Collections.Generic;

namespace Socialite.NET.Abstractions;

/// <summary>
/// Interface for an OAuth provider user
/// </summary>
public interface IUser
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    string Id { get; }
        
    /// <summary>
    /// User's nickname / username
    /// </summary>
    string? Nickname { get; }
        
    /// <summary>
    /// User's full name
    /// </summary>
    string? Name { get; }
        
    /// <summary>
    /// User's email address
    /// </summary>
    string? Email { get; }
        
    /// <summary>
    /// URL to user's avatar
    /// </summary>
    string? Avatar { get; }
        
    /// <summary>
    /// URL to user's original avatar (high resolution)
    /// </summary>
    string? AvatarOriginal { get; }
        
    /// <summary>
    /// URL to user's profile
    /// </summary>
    string? ProfileUrl { get; }
        
    /// <summary>
    /// Access token
    /// </summary>
    string Token { get; }
        
    /// <summary>
    /// Refresh token
    /// </summary>
    string? RefreshToken { get; }
        
    /// <summary>
    /// Token lifetime in seconds
    /// </summary>
    int ExpiresIn { get; }
        
    /// <summary>
    /// Scopes approved by the user
    /// </summary>
    IEnumerable<string> ApprovedScopes { get; }
        
    /// <summary>
    /// Raw user data
    /// </summary>
    IDictionary<string, object?> UserData { get; }
        
    /// <summary>
    /// Sets the raw user data
    /// </summary>
    /// <param name="user">User data</param>
    /// <returns>User instance</returns>
    IUser SetRaw(IDictionary<string, object?> user);
        
    /// <summary>
    /// Maps the provided attributes to user properties
    /// </summary>
    /// <param name="attributes">Attributes to map</param>
    /// <returns>User instance</returns>
    IUser Map(IDictionary<string, object?> attributes);
        
    /// <summary>
    /// Sets the access token
    /// </summary>
    /// <param name="token">Access token</param>
    /// <returns>User instance</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when token is null or empty</exception>
    IUser SetToken(string token);
        
    /// <summary>
    /// Sets the refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>User instance</returns>
    IUser SetRefreshToken(string? refreshToken);
        
    /// <summary>
    /// Sets the token lifetime
    /// </summary>
    /// <param name="expiresIn">Duration in seconds</param>
    /// <returns>User instance</returns>
    IUser SetExpiresIn(int expiresIn);
        
    /// <summary>
    /// Sets the approved scopes
    /// </summary>
    /// <param name="scopes">List of scopes</param>
    /// <returns>User instance</returns>
    IUser SetApprovedScopes(IEnumerable<string> scopes);
}