
using SocialiteNET.Abstractions;

namespace SocialiteNET.Providers.Github;

/// <summary>
/// Configuration for GitHub provider
/// </summary>
public class GitHubConfig : ProviderConfig
{
    /// <summary>
    /// Initializes a new instance of the GitHubConfig
    /// </summary>
    public GitHubConfig()
    {
        this.Scopes.Add("user:email");
    }
}