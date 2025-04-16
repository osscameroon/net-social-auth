using Socialite.NET.Abstractions;

namespace Socialite.NET.Providers.Google;

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
        Scopes.Add("openid");
        Scopes.Add("profile");
        Scopes.Add("email");
        ScopeSeparator = " ";
    }
}