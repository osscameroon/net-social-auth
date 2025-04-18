using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Socialite.NET.Abstractions;

/// <summary>
/// Interface for OAuth providers
/// </summary>
public interface IProvider
{
    /// <summary>
    /// Redirects the user to the provider's authentication page
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Redirect URL</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when context is null</exception>
    Task<string> RedirectAsync(HttpContext context);

    /// <summary>   
    /// Retrieves the user after redirection
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Authenticated user</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when context is null</exception>
    /// <exception cref="Exceptions.InvalidStateException">Thrown when the state parameter is invalid</exception>
    Task<IUser> GetUserAsync(HttpContext context);

    /// <summary>
    /// Retrieves a Social user from a known access token
    /// </summary>
    /// <param name="token">Access token</param>
    /// <returns>Authenticated user</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when token is null or empty</exception>
    Task<IUser> GetUserFromTokenAsync(string token);

    /// <summary>
    /// Refreshes the access token with a refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>New token</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when refreshToken is null or empty</exception>
    Task<IToken> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Configures the provider to not use state
    /// </summary>
    /// <returns>Provider instance</returns>
    IProvider Stateless();

    /// <summary>
    /// Configures the provider to use PKCE
    /// </summary>
    /// <returns>Provider instance</returns>
    IProvider WithPkce();

    /// <summary>
    /// Adds scopes to the request
    /// </summary>
    /// <param name="scopes">Scopes to add</param>
    /// <returns>Provider instance</returns>
    IProvider AddScopes(params string[] scopes);

    /// <summary>
    /// Replaces existing scopes with new ones
    /// </summary>
    /// <param name="scopes">New scopes</param>
    /// <returns>Provider instance</returns>
    IProvider SetScopes(params string[] scopes);

    /// <summary>
    /// Sets the redirect URL
    /// </summary>
    /// <param name="url">Redirect URL</param>
    /// <returns>Provider instance</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when url is null or empty</exception>
    IProvider SetRedirectUrl(string url);

    /// <summary>
    /// Adds custom parameters to the request
    /// </summary>
    /// <param name="parameters">Parameters to add</param>
    /// <returns>Provider instance</returns>
    IProvider With(object parameters);
}