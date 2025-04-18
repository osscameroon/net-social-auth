using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Socialite.NET.Abstractions;
using Socialite.NET.Abstractions.Exceptions;
using Socialite.NET.Core;

namespace Socialite.NET.Providers.Google;

/// <summary>
/// Google authentication provider
/// </summary>
public class GoogleProvider : AbstractProvider
{
    /// <summary>
    /// Initializes a new instance of the GoogleProvider
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="clientId">Client ID</param>
    /// <param name="clientSecret">Client secret</param>
    /// <param name="redirectUrl">Redirect URL</param>
    /// <param name="options">Additional HTTP client options</param>
    public GoogleProvider(
        HttpClient httpClient,
        string clientId,
        string clientSecret,
        string redirectUrl,
        Dictionary<string, object>? options = null)
        : base(httpClient, clientId, clientSecret, redirectUrl, options)
    {
        ScopeSeparator = " ";
        Scopes.AddRange(["openid", "profile", "email"]);
    }

    /// <summary>
    /// Initializes a new instance of the GoogleProvider with config
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="options">Provider options</param>
    public GoogleProvider(
        HttpClient httpClient,
        IOptions<GoogleConfig> options)
        : base(httpClient, options)
    {
    }

    /// <inheritdoc />
    protected override string GetAuthUrl(string? state)
    {
        return BuildAuthUrlFromBase("https://accounts.google.com/o/oauth2/auth", state);
    }

    /// <inheritdoc />
    protected override string GetTokenUrl()
    {
        return "https://www.googleapis.com/oauth2/v4/token";
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
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(content).RootElement.DeserializeToDict();
        }
        catch (Exception ex)
        {
            throw new AuthenticationException("Error retrieving user information from Google", ex);
        }
    }

    /// <inheritdoc />
    protected override IUser MapUserToObject(Dictionary<string, object?> user)
    {
        user.TryGetValue("sub", out object? sub);
        user.TryGetValue("nickname", out object? nickname);
        user.TryGetValue("name", out object? name);
        user.TryGetValue("email", out object? email);
        user.TryGetValue("picture", out object? avatar);

        return new User()
            .SetRaw(user)
            .Map(new Dictionary<string, object?>
            {
                ["Id"] = sub?.ToString() ?? string.Empty,
                ["Nickname"] = nickname?.ToString(),
                ["Name"] = name?.ToString(),
                ["Email"] = email?.ToString(),
                ["Avatar"] = avatar?.ToString(),
                ["AvatarOriginal"] = avatar?.ToString()
            });
    }
}