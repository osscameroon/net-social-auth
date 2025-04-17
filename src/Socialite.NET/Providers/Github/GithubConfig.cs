using Socialite.NET.Abstractions;

namespace Socialite.NET.Providers.GitHub;

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
        Scopes.Add("user:email");
    }
}