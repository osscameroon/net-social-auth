using SocialiteNET.Abstractions;

namespace SocialiteNET.Providers.Google;

/// <summary>
/// Configuration for Google provider
/// </summary>
public class GoogleConfig : ProviderConfig
{
    /// <summary>
    /// Initializes a new instance of the GoogleConfig
    /// </summary>
    public GoogleConfig()
    {
        this.Scopes.Add("openid");
        this.Scopes.Add("profile");
        this.Scopes.Add("email");
        this.ScopeSeparator = " ";
    }
}