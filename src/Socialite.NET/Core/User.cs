using System;
using System.Collections.Generic;
using Socialite.NET.Abstractions;

namespace Socialite.NET.Core;

/// <summary>
/// Default implementation of OAuth user
/// </summary>
public class User : IUser
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
        
    /// <inheritdoc />
    public string? Nickname { get; set; }
        
    /// <inheritdoc />
    public string? Name { get; set; }
        
    /// <inheritdoc />
    public string? Email { get; set; }
        
    /// <inheritdoc />
    public string? Avatar { get; set; }
        
    /// <inheritdoc />
    public string? AvatarOriginal { get; set; }
        
    /// <inheritdoc />
    public string? ProfileUrl { get; set; }
        
    /// <inheritdoc />
    public string Token { get; set; } = string.Empty;
        
    /// <inheritdoc />
    public string? RefreshToken { get; set; }
        
    /// <inheritdoc />
    public int ExpiresIn { get; set; }
        
    /// <inheritdoc />
    public IEnumerable<string> ApprovedScopes { get; set; } = Array.Empty<string>();
        
    /// <inheritdoc />
    public IDictionary<string, object?> UserData { get; set; } = new Dictionary<string, object?>();
        
    /// <inheritdoc />
    public virtual IUser SetRaw(IDictionary<string, object?> user)
    {
        UserData = user ?? new Dictionary<string, object?>();
        return this;
    }
        
    /// <inheritdoc />
    public virtual IUser Map(IDictionary<string, object?> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        foreach (var (key, value) in attributes)
        {
            var property = GetType().GetProperty(key);
            if (property != null && property.CanWrite)
            {
                property.SetValue(this, value);
            }
        }
            
        return this;
    }
        
    /// <inheritdoc />
    public IUser SetToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentNullException(nameof(token));
        }

        Token = token;
        return this;
    }
        
    /// <inheritdoc />
    public IUser SetRefreshToken(string? refreshToken)
    {
        RefreshToken = refreshToken;
        return this;
    }
        
    /// <inheritdoc />
    public IUser SetExpiresIn(int expiresIn)
    {
        ExpiresIn = expiresIn;
        return this;
    }
        
    /// <inheritdoc />
    public IUser SetApprovedScopes(IEnumerable<string> scopes)
    {
        ApprovedScopes = scopes ?? Array.Empty<string>();
        return this;
    }
}