using System.Collections.Generic;

namespace Socialite.NET.Abstractions;

/// <summary>
/// Configuration for an OAuth provider
/// </summary>
public class ProviderConfig
{
    /// <summary>
    /// Client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Redirect URL
    /// </summary>
    public string RedirectUrl { get; set; } = string.Empty;

    /// <summary>
    /// Requested scopes
    /// </summary>
    public IList<string> Scopes { get; set; } = new List<string>();

    /// <summary>
    /// Scope separator
    /// </summary>
    public string ScopeSeparator { get; set; } = " ";

    /// <summary>
    /// Additional parameters
    /// </summary>
    public IDictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Indicates if the provider uses stateless mode
    /// </summary>
    public bool Stateless { get; set; } = false;

    /// <summary>
    /// Indicates if the provider uses PKCE
    /// </summary>
    public bool UsesPkce { get; set; } = false;

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <exception cref="System.ArgumentException">Thrown when required properties are missing</exception>
    public virtual void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            throw new System.ArgumentException("ClientId is required", nameof(ClientId));
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            throw new System.ArgumentException("ClientSecret is required", nameof(ClientSecret));
        }

        if (string.IsNullOrWhiteSpace(RedirectUrl))
        {
            throw new System.ArgumentException("RedirectUrl is required", nameof(RedirectUrl));
        }
    }
}