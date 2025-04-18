using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SocialiteNET.Abstractions;
using SocialiteNET.Abstractions.Exceptions;
using SocialiteNET.Core;

namespace SocialiteNET.Providers.Github;

/// <summary>
/// GitHub authentication provider
/// </summary>
public class GitHubProvider : AbstractProvider
{
    /// <summary>
    /// Initializes a new instance of the GitHubProvider
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="clientId">Client ID</param>
    /// <param name="clientSecret">Client secret</param>
    /// <param name="redirectUrl">Redirect URL</param>
    /// <param name="guzzleOptions">Additional HTTP client options</param>
    public GitHubProvider(
        HttpClient httpClient,
        string clientId,
        string clientSecret,
        string redirectUrl,
        Dictionary<string, object>? guzzleOptions = null)
        : base(httpClient, clientId, clientSecret, redirectUrl, guzzleOptions)
    {
        this.Scopes.Add("user:email");
    }

    /// <summary>
    /// Initializes a new instance of the GitHubProvider with config
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="options">Provider options</param>
    public GitHubProvider(
        HttpClient httpClient,
        IOptions<GitHubConfig> options)
        : base(httpClient, options)
    {
    }

    /// <inheritdoc />
    protected override string GetAuthUrl(string? state)
    {
        return this.BuildAuthUrlFromBase("https://github.com/login/oauth/authorize", state);
    }

    /// <inheritdoc />
    protected override string GetTokenUrl()
    {
        return "https://github.com/login/oauth/access_token";
    }

    /// <inheritdoc />
    protected override async Task<Dictionary<string, object?>> GetUserByToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentNullException(nameof(token));
        }

        try
        {
            HttpRequestMessage request = new(HttpMethod.Get, "https://api.github.com/user");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            request.Headers.Add("User-Agent", "Socialite.NET");
            request.Headers.Authorization = new AuthenticationHeaderValue("token", token);

            HttpResponseMessage response = await this.HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            Dictionary<string, object?> user = JsonDocument.Parse(content).RootElement.DeserializeToDict();

            // Add email if user:email scope is requested
            if (!this.Scopes.Contains("user:email"))
            {
                return user;
            }

            string? email = await this.GetEmailByToken(token);
            if (!string.IsNullOrEmpty(email))
            {
                user["email"] = email;
            }

            return user;
        }
        catch (Exception ex)
        {
            throw new AuthenticationException("Error retrieving user information from GitHub", ex);
        }
    }

    /// <summary>
    /// Gets the email for the given access token
    /// </summary>
    /// <param name="token">Access token</param>
    /// <returns>User's email or null</returns>
    private async Task<string?> GetEmailByToken(string token)
    {
        try
        {
            HttpRequestMessage request = new(HttpMethod.Get, "https://api.github.com/user/emails");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("token", token);

            HttpResponseMessage response = await this.HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            JsonElement emails = JsonDocument.Parse(content).RootElement;

            foreach (var email in emails.EnumerateArray())
            {
                bool isPrimary = false;
                bool isVerified = false;

                if (email.TryGetProperty("primary", out var primary))
                {
                    isPrimary = primary.GetBoolean();
                }

                if (email.TryGetProperty("verified", out var verified))
                {
                    isVerified = verified.GetBoolean();
                }

                if (isPrimary && isVerified && email.TryGetProperty("email", out var emailValue))
                {
                    return emailValue.GetString();
                }
            }
        }
        catch
        {
            // Ignore errors and return null
        }

        return null;
    }

    /// <inheritdoc />
    protected override IUser MapUserToObject(Dictionary<string, object?> user)
    {
        user.TryGetValue("id", out object? id);
        user.TryGetValue("login", out object? login);
        user.TryGetValue("name", out object? name);
        user.TryGetValue("email", out object? email);
        user.TryGetValue("avatar_url", out object? avatar);

        return new User()
            .SetRaw(user)
            .Map(new Dictionary<string, object?>
            {
                ["Id"] = id?.ToString() ?? string.Empty,
                ["Nickname"] = login?.ToString(),
                ["Name"] = name?.ToString(),
                ["Email"] = email?.ToString(),
                ["Avatar"] = avatar?.ToString()
            });
    }
}


