using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Socialite.NET.Abstractions;
using Socialite.NET.Abstractions.Exceptions;

namespace Socialite.NET.Core;

/// <summary>
/// Base class for OAuth 2.0 providers
/// </summary>
public abstract class AbstractProvider : IProvider
{
    /// <summary>
    /// HTTP client instance
    /// </summary>
    protected readonly HttpClient HttpClient;
        
    /// <summary>
    /// Provider client ID
    /// </summary>
    protected string ClientId;
        
    /// <summary>
    /// Provider client secret
    /// </summary>
    protected string ClientSecret;
        
    /// <summary>
    /// Redirect URL
    /// </summary>
    protected string RedirectUrl;
        
    /// <summary>
    /// Custom parameters
    /// </summary>
    protected readonly Dictionary<string, string> Parameters = new Dictionary<string, string>();
        
    /// <summary>
    /// Requested scopes
    /// </summary>
    protected List<string> Scopes = new List<string>();
        
    /// <summary>
    /// Scope separator
    /// </summary>
    protected string ScopeSeparator = " ";

    /// <summary>
    /// Query encoding type
    /// </summary>
    protected int EncodingType;
        
    /// <summary>
    /// Indicates if the provider should operate statelessly
    /// </summary>
    protected bool IsStatelessMode;
        
    /// <summary>
    /// Indicates if the provider uses PKCE
    /// </summary>
    protected bool UsesPkce = false;
        
    /// <summary>
    /// Cached user
    /// </summary>
    protected IUser? CachedUser;
    
    /// <summary>
    /// Storage for the PKCE code verifier
    /// </summary>
    protected string? _codeVerifier;
        
    /// <summary>
    /// Initializes a new instance of the provider
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="clientId">Client ID</param>
    /// <param name="clientSecret">Client secret</param>
    /// <param name="redirectUrl">Redirect URL</param>
    /// <param name="options">Additional HTTP client options</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    protected AbstractProvider(
        HttpClient httpClient,
        string clientId,
        string clientSecret,
        string redirectUrl,
        Dictionary<string, object>? options = null)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ClientId = !string.IsNullOrEmpty(clientId) ? clientId : throw new ArgumentNullException(nameof(clientId));
        ClientSecret = !string.IsNullOrEmpty(clientSecret) ? clientSecret : throw new ArgumentNullException(nameof(clientSecret));
        RedirectUrl = !string.IsNullOrEmpty(redirectUrl) ? redirectUrl : throw new ArgumentNullException(nameof(redirectUrl));
            
        this.ConfigureHttpClient();
    }
        
    /// <summary>
    /// Initializes a new instance of the provider with options
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="options">Provider options</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    protected AbstractProvider(
        HttpClient httpClient,
        IOptions<ProviderConfig> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ProviderConfig config = options.Value;
        config.Validate();
            
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ClientId = config.ClientId;
        ClientSecret = config.ClientSecret;
        RedirectUrl = config.RedirectUrl;
        Scopes = new List<string>(config.Scopes);
        ScopeSeparator = config.ScopeSeparator;
        Parameters = new Dictionary<string, string>(config.Parameters);
        IsStatelessMode = config.Stateless;
        UsesPkce = config.UsesPkce;
            
        this.ConfigureHttpClient();
    }
        
    /// <summary>
    /// Configures the HTTP client
    /// </summary>
    private void ConfigureHttpClient()
    {
        // Basic HTTP client configuration
        HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
        
    /// <summary>
    /// Gets the authentication URL for the provider
    /// </summary>
    /// <param name="state">State parameter</param>
    /// <returns>Authentication URL</returns>
    protected abstract string GetAuthUrl(string? state);
        
    /// <summary>
    /// Gets the token URL for the provider
    /// </summary>
    /// <returns>Token URL</returns>
    protected abstract string GetTokenUrl();
        
    /// <summary>
    /// Gets the raw user for the given access token
    /// </summary>
    /// <param name="token">Access token</param>
    /// <returns>User data</returns>
    /// <exception cref="ArgumentNullException">Thrown when token is null</exception>
    protected abstract Task<Dictionary<string, object?>> GetUserByToken(string token);
        
    /// <summary>
    /// Maps the raw user array to a User instance
    /// </summary>
    /// <param name="user">Raw user data</param>
    /// <returns>User instance</returns>
    protected abstract IUser MapUserToObject(Dictionary<string, object?> user);
        
    /// <inheritdoc />
    public virtual async Task<string> RedirectAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? state = null;
            
        if (this.UsesState())
        {
            state = this.GetState();
            context.Session.Set("socialite:state", Encoding.UTF8.GetBytes(state));
        }

        if (!this.UsesPkce)
        {
            return await Task.FromResult(this.GetAuthUrl(state));
        }

        this._codeVerifier = this.GetCodeVerifier();
        context.Session.Set("socialite:code_verifier", Encoding.UTF8.GetBytes(this._codeVerifier));

        return await Task.FromResult(this.GetAuthUrl(state));
    }
        
    /// <summary>
    /// Builds the authentication URL from the base URL
    /// </summary>
    /// <param name="url">Base URL</param>
    /// <param name="state">State parameter</param>
    /// <returns>Authentication URL</returns>
    protected virtual string BuildAuthUrlFromBase(string url, string? state)
    {
        return QueryHelpers.AddQueryString(url, this.GetCodeFields(state)!);
    }
        
    /// <summary>
    /// Gets the GET parameters for the code request
    /// </summary>
    /// <param name="state">State parameter</param>
    /// <returns>Request parameters</returns>
    protected virtual Dictionary<string, string> GetCodeFields(string? state = null)
    {
        Dictionary<string, string> fields = new Dictionary<string, string>
        {
            ["client_id"] = this.ClientId,
            ["redirect_uri"] = this.RedirectUrl,
            ["scope"] = this.FormatScopes(this.Scopes, this.ScopeSeparator),
            ["response_type"] = "code"
        };
            
        if (this.UsesState() && !string.IsNullOrEmpty(state))
        {
            fields["state"] = state;
        }
            
        if (this.UsesPkce)
        {
            string codeChallenge = this.GetCodeChallenge();
            if (!string.IsNullOrEmpty(codeChallenge))
            {
                fields["code_challenge"] = codeChallenge;
                fields["code_challenge_method"] = this.GetCodeChallengeMethod();
            }
        }
            
        foreach (KeyValuePair<string, string> param in this.Parameters)
        {
            fields[param.Key] = param.Value;
        }
            
        return fields;
    }
        
    /// <summary>
    /// Formats the given scopes
    /// </summary>
    /// <param name="scopes">Scopes to format</param>
    /// <param name="scopeSeparator">Scope separator</param>
    /// <returns>Formatted scopes</returns>
    protected virtual string FormatScopes(IEnumerable<string> scopes, string scopeSeparator)
    {
        return string.Join(scopeSeparator, scopes ?? Array.Empty<string>());
    }
        
    /// <inheritdoc />
    public virtual async Task<IUser> GetUserAsync(HttpContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
                
        if (this.CachedUser != null)
        {
            return this.CachedUser;
        }
            
        if (this.HasInvalidState(context))
        {
            throw new InvalidStateException();
        }
            
        string? code = this.GetCode(context);
        if (string.IsNullOrEmpty(code))
        {
            throw new AuthenticationException("Authorization code not found in the request");
        }
            
        JsonElement response = await this.GetAccessTokenResponse(code, context);
            
        if (!response.TryGetProperty("access_token", out JsonElement tokenElement) || 
            tokenElement.ValueKind == JsonValueKind.Null)
        {
            throw new AuthenticationException("Access token not found in the response");
        }
            
        string? token = tokenElement.GetString();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Access token is null or empty");
        }
            
        Dictionary<string, object?> user = await this.GetUserByToken(token);
            
        return this.CreateUserFromResponse(response, user);
    }
        
    /// <summary>
    /// Creates a user instance from the given data
    /// </summary>
    /// <param name="response">Token response</param>
    /// <param name="user">User data</param>
    /// <returns>User instance</returns>
    protected virtual IUser CreateUserFromResponse(JsonElement response, Dictionary<string, object?> user)
    {
        string token = response.GetProperty("access_token").GetString() ?? string.Empty;
        string? refreshToken = this.GetOptionalProperty(response, "refresh_token");
        int expiresIn = this.GetOptionalIntProperty(response, "expires_in");
        string? scopeStr = this.GetOptionalProperty(response, "scope");
        string[] scopes = !string.IsNullOrEmpty(scopeStr) ? 
            scopeStr.Split(this.ScopeSeparator) : 
            Array.Empty<string>();
            
        this.CachedUser = this.MapUserToObject(user);
            
        return this.CachedUser
            .SetToken(token)
            .SetRefreshToken(refreshToken)
            .SetExpiresIn(expiresIn)
            .SetApprovedScopes(scopes);
    }
        
    /// <summary>
    /// Gets an optional property from a JSON element
    /// </summary>
    /// <param name="element">JSON element</param>
    /// <param name="propertyName">Property name</param>
    /// <returns>Property value or null</returns>
    protected virtual string? GetOptionalProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) && 
               property.ValueKind != JsonValueKind.Null ? 
            property.GetString() : null;
    }
        
    /// <summary>
    /// Gets an optional numeric property from a JSON element
    /// </summary>
    /// <param name="element">JSON element</param>
    /// <param name="propertyName">Property name</param>
    /// <returns>Property value or 0</returns>
    protected virtual int GetOptionalIntProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) && 
               property.ValueKind != JsonValueKind.Null && 
               property.TryGetInt32(out int value) ? 
            value : 0;
    }
        
    /// <inheritdoc />
    public virtual async Task<IUser> GetUserFromTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentNullException(nameof(token));
        }

        Dictionary<string, object?> user = await this.GetUserByToken(token);
            
        return this.MapUserToObject(user).SetToken(token);
    }
        
    /// <inheritdoc />
    public virtual async Task<IToken> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new ArgumentNullException(nameof(refreshToken));
        }

        JsonElement response = await this.GetRefreshTokenResponse(refreshToken);
            
        if (!response.TryGetProperty("access_token", out JsonElement tokenProperty) || 
            tokenProperty.ValueKind == JsonValueKind.Null)
        {
            throw new AuthenticationException("Access token not found in the response");
        }
            
        string accessToken = tokenProperty.GetString() ?? string.Empty;
        string newRefreshToken = this.GetOptionalProperty(response, "refresh_token") ?? refreshToken;
        int expiresIn = this.GetOptionalIntProperty(response, "expires_in");
        string? scopeStr = this.GetOptionalProperty(response, "scope");
        string[] scopes = !string.IsNullOrEmpty(scopeStr) ? 
            scopeStr.Split(this.ScopeSeparator) : 
            Array.Empty<string>();
            
        return new Token(accessToken, newRefreshToken, expiresIn, scopes);
    }
        
    /// <summary>
    /// Gets the refresh token response
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>Token response</returns>
    /// <exception cref="AuthenticationException">Thrown when the token refresh fails</exception>
    protected virtual async Task<JsonElement> GetRefreshTokenResponse(string refreshToken)
    {
        try
        {
            Dictionary<string, string> requestParams = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = this.ClientId,
                ["client_secret"] = this.ClientSecret
            };
            
            HttpResponseMessage response = await this.HttpClient.PostAsync(
                this.GetTokenUrl(),
                new FormUrlEncodedContent(requestParams)
            );
                
            response.EnsureSuccessStatusCode();
                
            string content = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(content).RootElement;
        }
        catch (Exception ex)
        {
            throw new AuthenticationException("Failed to refresh token", ex);
        }
    }
        
    /// <summary>
    /// Determines if the current request has an invalid state
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>True if state is invalid</returns>
    protected virtual bool HasInvalidState(HttpContext context)
    {
        if (this.IsStateless())
        {
            return false;
        }

        bool hasState = context.Session.TryGetValue("socialite:state", out byte[]? stateBytes);
        if (!hasState || stateBytes == null || stateBytes.Length == 0)
        {
            return true;
        }
        
        string state = Encoding.UTF8.GetString(stateBytes);
        string requestState = context.Request.Query["state"].ToString();
            
        return string.IsNullOrEmpty(state) || 
               string.IsNullOrEmpty(requestState) || 
               state != requestState;
    }
        
    /// <summary>
    /// Gets the access token response for the given code
    /// </summary>
    /// <param name="code">Authorization code</param>
    /// <param name="context">HTTP context</param>
    /// <returns>Token response</returns>
    /// <exception cref="AuthenticationException">Thrown when the token request fails</exception>
    protected virtual async Task<JsonElement> GetAccessTokenResponse(string code, HttpContext context)
    {
        try
        {
            Dictionary<string, string> tokenFields = this.GetTokenFields(code, context);
                
            HttpResponseMessage response = await this.HttpClient.PostAsync(
                this.GetTokenUrl(),
                new FormUrlEncodedContent(tokenFields)
            );
                
            response.EnsureSuccessStatusCode();
                
            string content = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(content).RootElement;
        }
        catch (Exception ex)
        {
            throw new AuthenticationException("Failed to get access token", ex);
        }
    }
        
    /// <summary>
    /// Gets the fields for the token request
    /// </summary>
    /// <param name="code">Authorization code</param>
    /// <param name="context">HTTP context</param>
    /// <returns>Token request fields</returns>
    protected virtual Dictionary<string, string> GetTokenFields(string code, HttpContext context)
    {
        Dictionary<string, string> fields = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = this.ClientId,
            ["client_secret"] = this.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = this.RedirectUrl
        };
            
        if (this.UsesPkce)
        {
            bool hasVerifier = context.Session.TryGetValue("socialite:code_verifier", out byte[]? codeVerifierBytes);
            
            if (hasVerifier && codeVerifierBytes != null && codeVerifierBytes.Length > 0)
            {
                string codeVerifier = Encoding.UTF8.GetString(codeVerifierBytes);
                
                if (!string.IsNullOrEmpty(codeVerifier))
                {
                    fields["code_verifier"] = codeVerifier;
                }
            }
        }
            
        foreach (KeyValuePair<string, string> param in this.Parameters)
        {
            fields[param.Key] = param.Value;
        }
            
        return fields;
    }
        
    /// <summary>
    /// Gets the authorization code from the request
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Authorization code</returns>
    protected virtual string? GetCode(HttpContext context)
    {
        return context.Request.Query["code"].ToString();
    }
        
    /// <inheritdoc />
    public virtual IProvider AddScopes(params string[] scopes)
    {
        if (scopes == null)
        {
            return this;
        }

        foreach (string scope in scopes)
        {
            if (!string.IsNullOrEmpty(scope) && !this.Scopes.Contains(scope))
            {
                this.Scopes.Add(scope);
            }
        }
            
        return this;
    }
        
    /// <inheritdoc />
    public virtual IProvider SetScopes(params string[] scopes)
    {
        this.Scopes.Clear();
            
        if (scopes != null)
        {
            foreach (string scope in scopes)
            {
                if (!string.IsNullOrEmpty(scope))
                {
                    this.Scopes.Add(scope);
                }
            }
        }
            
        return this;
    }
        
    /// <inheritdoc />
    public virtual IProvider SetRedirectUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentNullException(nameof(url));
        }

        this.RedirectUrl = url;
        return this;
    }
        
    /// <inheritdoc />
    public virtual IProvider Stateless()
    {
        this.IsStatelessMode = true;
        return this;
    }
        
    /// <inheritdoc />
    public virtual IProvider WithPkce()
    {
        this.UsesPkce = true;
        return this;
    }
        
    /// <inheritdoc />
    public virtual IProvider With(object parameters)
    {
        if (parameters == null)
            return this;
                
        // Convert anonymous object to dictionary via reflection
        System.Reflection.PropertyInfo[] props = parameters.GetType().GetProperties();
        foreach (System.Reflection.PropertyInfo prop in props)
        {
            string? value = prop.GetValue(parameters)?.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                this.Parameters[prop.Name] = value;
            }
        }
            
        return this;
    }
        
    /// <summary>
    /// Determines if the provider is operating with state
    /// </summary>
    /// <returns>True if using state</returns>
    protected virtual bool UsesState()
    {
        return !this.IsStatelessMode;
    }
        
    /// <summary>
    /// Determines if the provider is operating as stateless
    /// </summary>
    /// <returns>True if stateless</returns>
    protected virtual bool IsStateless()
    {
        return this.IsStatelessMode;
    }
        
    /// <summary>
    /// Generates a random string for state
    /// </summary>
    /// <returns>Random state string</returns>
    protected virtual string GetState()
    {
        return Guid.NewGuid().ToString("N");
    }
        
    /// <summary>
    /// Generates a random string for the PKCE code verifier
    /// </summary>
    /// <returns>Code verifier</returns>
    protected virtual string GetCodeVerifier()
    {
        byte[] bytes = new byte[64]; // 64 bytes = 512 bits
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
            
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }
        
    /// <summary>
    /// Generates the code challenge from the code verifier
    /// </summary>
    /// <returns>Code challenge</returns>
    protected virtual string GetCodeChallenge()
    {
        if (string.IsNullOrEmpty(this._codeVerifier))
            return string.Empty;
                
        using SHA256 sha256 = SHA256.Create();
        byte[] challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(this._codeVerifier));
            
        return Convert.ToBase64String(challengeBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }
        
    /// <summary>
    /// Gets the code challenge method for PKCE
    /// </summary>
    /// <returns>Challenge method</returns>
    protected virtual string GetCodeChallengeMethod()
    {
        return "S256";
    }
}