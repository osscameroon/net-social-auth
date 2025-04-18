
# SocialiteNET Documentation

Welcome to the SocialiteNET documentation. This wiki contains comprehensive information about the library, its features, and how to use it effectively in your ASP.NET Core applications.

## Table of Contents

1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Configuration](#configuration)
4. [Basic Usage](#basic-usage)
5. [Advanced Usage](#advanced-usage)
6. [Providers](#providers)
7. [Customization](#customization)
8. [Examples](#examples)
9. [Troubleshooting](#troubleshooting)
10. [Contributing](#contributing)

## Introduction

SocialiteNET is an OAuth authentication wrapper for ASP.NET Core, inspired by Laravel Socialite. It provides a clean, fluent API to handle the complex OAuth authentication process with minimal boilerplate code.

### Why SocialiteNET?

OAuth authentication can be complex, with many moving parts:
- Authorization request generation
- State parameter validation
- Token retrieval and storage
- User profile retrieval
- Scope management
- Token refreshing

SocialiteNET handles all these tasks with a clean, expressive API, allowing you to implement social login with minimal effort.

### Key Features

- **Fluent API**: Chain methods to build requests exactly as needed
- **Security Best Practices**: Built-in support for state validation and PKCE
- **Extensible Architecture**: Easily add custom providers
- **Multiple Provider Support**: Ready-to-use implementations for popular OAuth providers
- **Stateless Mode**: Support for both traditional web apps and APIs
- **Robust Error Handling**: Clear exceptions with meaningful messages

## Installation

### Prerequisites

- .NET 8.0 or higher
- ASP.NET Core project

### Package Installation

Install the SocialiteNET package using the .NET CLI:

```bash
dotnet add package SocialiteNET
```

Or via the Package Manager Console:

```powershell
Install-Package SocialiteNET
```

For provider-specific packages:

```bash
dotnet add package SocialiteNET.Providers.Google
# Additional providers as they become available
```

## Configuration

### Service Registration

First, register Socialite services in your `Program.cs` or `Startup.cs` file:

```csharp
using SocialiteNET;

var builder = WebApplication.CreateBuilder(args);

// Add Socialite services
builder.Services.AddSocialite(options =>
{
    // Optional: Set a default provider
    options.DefaultDriver = "google";
});

// Register required providers
builder.Services.AddSocialite()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.RedirectUrl = builder.Configuration["Authentication:Google:RedirectUrl"];
        
        // Optional settings
        // options.Stateless = true;
        // options.UsesPkce = true;
        // options.Scopes.Add("calendar");
    });

var app = builder.Build();

// Add Socialite middleware (adds session and authentication)
app.UseSocialite();

// Other middleware configurations...
```

### Configuration in appsettings.json

Store your OAuth credentials in your configuration:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "RedirectUrl": "https://your-app.com/auth/google/callback"
    }
  }
}
```

### Provider Configuration Options

Each provider can be configured with the following options:

| Option | Description | Default |
|--------|-------------|---------|
| ClientId | OAuth client ID | *Required* |
| ClientSecret | OAuth client secret | *Required* |
| RedirectUrl | Callback URL after authentication | *Required* |
| Scopes | List of OAuth scopes to request | Provider-specific defaults |
| ScopeSeparator | Character used to separate scopes | " " (space) |
| Stateless | Whether to operate in stateless mode | false |
| UsesPkce | Whether to use PKCE | false |
| Parameters | Additional OAuth parameters | {} |

## Basic Usage

The typical OAuth flow involves two routes:

1. A route to redirect the user to the OAuth provider
2. A callback route to handle the OAuth response

### Example Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using SocialiteNET.Abstractions;

public class AuthController : Controller
{
    private readonly ISocialite _socialite;

    public AuthController(ISocialite socialite)
    {
        _socialite = socialite;
    }

    [HttpGet("auth/google")]
    public async Task<IActionResult> RedirectToGoogle()
    {
        // Get the provider and generate the redirect URL
        string redirectUrl = await _socialite.GetProvider("google")
            .RedirectAsync(HttpContext);

        // Redirect the user to the OAuth provider
        return Redirect(redirectUrl);
    }

    [HttpGet("auth/google/callback")]
    public async Task<IActionResult> HandleGoogleCallback()
    {
        // Get the authenticated user from the OAuth provider
        IUser user = await _socialite.GetProvider("google")
            .GetUserAsync(HttpContext);

        // Process user information
        // 1. Check if the user exists in your database
        // 2. Create or update the user record
        // 3. Sign in the user

        // Available user properties
        string id = user.Id;                  // Unique provider user ID
        string? name = user.Name;             // Full name
        string? email = user.Email;           // Email address
        string? nickname = user.Nickname;     // Username or nickname
        string? avatar = user.Avatar;         // Profile picture URL
        string token = user.Token;            // OAuth access token
        string? refreshToken = user.RefreshToken; // OAuth refresh token (if available)
        int expiresIn = user.ExpiresIn;       // Token expiration in seconds

        // Sign in the user with your auth system
        // ...

        return RedirectToAction("Dashboard", "Home");
    }
}
```

## Advanced Usage

### Stateless Authentication

For APIs or applications without sessions, use stateless mode to skip state parameter validation:

```csharp
// Configure at registration time
builder.Services.AddSocialite()
    .AddGoogle(options =>
    {
        // ...
        options.Stateless = true;
    });

// Or at runtime
string redirectUrl = await _socialite.GetProvider("google")
    .Stateless()
    .RedirectAsync(HttpContext);
```

### PKCE Support

For public clients (like SPAs), enable PKCE (Proof Key for Code Exchange) for enhanced security:

```csharp
// Configure at registration time
builder.Services.AddSocialite()
    .AddGoogle(options =>
    {
        // ...
        options.UsesPkce = true;
    });

// Or at runtime
string redirectUrl = await _socialite.GetProvider("google")
    .WithPkce()
    .RedirectAsync(HttpContext);
```

### Customizing Scopes

Control exactly what permissions your application requests:

```csharp
// Add specific scopes
string redirectUrl = await _socialite.GetProvider("google")
    .AddScopes("calendar", "drive.readonly")
    .RedirectAsync(HttpContext);

// Replace all scopes
string redirectUrl = await _socialite.GetProvider("google")
    .SetScopes("email", "profile", "calendar")
    .RedirectAsync(HttpContext);
```

### Custom Parameters

Add provider-specific parameters to the OAuth request:

```csharp
// For Google: request a specific prompt behavior and access type
string redirectUrl = await _socialite.GetProvider("google")
    .With(new { 
        prompt = "select_account", 
        access_type = "offline",
        hd = "example.com"
    })
    .RedirectAsync(HttpContext);
```

### User From Token

If you already have a valid access token, you can retrieve user information directly:

```csharp
IUser user = await _socialite.GetProvider("google")
    .GetUserFromTokenAsync(accessToken);
```

### Refreshing Tokens

For providers that support refresh tokens, you can obtain a new access token when it expires:

```csharp
IToken newToken = await _socialite.GetProvider("google")
    .RefreshTokenAsync(refreshToken);

string newAccessToken = newToken.AccessToken;
string? newRefreshToken = newToken.RefreshToken;
int expiresIn = newToken.ExpiresIn;
```

### Dynamic Provider Configuration

Create providers with custom configurations at runtime:

```csharp
// Create a custom provider configuration
var config = new ProviderConfig
{
    ClientId = "dynamic-client-id",
    ClientSecret = "dynamic-client-secret",
    RedirectUrl = "https://your-app.com/auth/callback",
    Stateless = true,
    UsesPkce = true
};

// Add some scopes
config.Scopes.Add("email");
config.Scopes.Add("profile");

// Build the provider
IProvider provider = _socialite.BuildProvider("google", config);

// Use the provider as normal
string redirectUrl = await provider.RedirectAsync(HttpContext);
```

## Providers

### Available Providers

Currently, SocialiteNET supports:

- **Google**
    - Default scopes: `openid`, `profile`, `email`
    - Documentation: [Google OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)

### Upcoming Providers

We plan to add support for the following providers:

- GitHub
- Facebook
- Twitter/X
- LinkedIn
- Microsoft
- Slack
- And more...

### Provider-Specific Notes

#### Google

- Default scopes include `openid`, `profile`, and `email`
- Map of user fields:
    - `Id` ← `sub`
    - `Name` ← `name`
    - `Email` ← `email`
    - `Avatar` ← `picture`

## Customization

### Creating Custom Providers

You can create custom OAuth providers by extending the `AbstractProvider` class:

```csharp
public class CustomProvider : AbstractProvider
{
    public CustomProvider(
        HttpClient httpClient,
        string clientId,
        string clientSecret,
        string redirectUrl)
        : base(httpClient, clientId, clientSecret, redirectUrl)
    {
        // Set provider-specific defaults
        ScopeSeparator = " ";
        Scopes.AddRange(["profile", "email"]);
    }

    protected override string GetAuthUrl(string? state)
    {
        return BuildAuthUrlFromBase("https://custom-provider.com/oauth/authorize", state);
    }

    protected override string GetTokenUrl()
    {
        return "https://custom-provider.com/oauth/token";
    }

    protected override async Task<Dictionary<string, object?>> GetUserByToken(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://custom-provider.com/api/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content).RootElement.DeserializeToDict();
    }

    protected override IUser MapUserToObject(Dictionary<string, object?> user)
    {
        return new User()
            .SetRaw(user)
            .Map(new Dictionary<string, object?>
            {
                ["Id"] = user.TryGetValue("id", out var id) ? id?.ToString() : string.Empty,
                ["Name"] = user.TryGetValue("name", out var name) ? name?.ToString() : null,
                ["Email"] = user.TryGetValue("email", out var email) ? email?.ToString() : null,
                ["Avatar"] = user.TryGetValue("avatar_url", out var avatar) ? avatar?.ToString() : null
            });
    }
}
```

### Registering Custom Providers

Register your custom provider with Socialite:

```csharp
// Register the provider with dependency injection
builder.Services.AddHttpClient<CustomProvider>();
builder.Services.AddTransient<CustomProvider>();

// Register with Socialite
builder.Services.AddSocialite()
    .Extend("custom", serviceProvider => 
    {
        var httpClient = serviceProvider.GetRequiredService<HttpClient>();
        return new CustomProvider(
            httpClient, 
            configuration["Authentication:Custom:ClientId"],
            configuration["Authentication:Custom:ClientSecret"],
            configuration["Authentication:Custom:RedirectUrl"]
        );
    });

// Use your custom provider
var redirectUrl = await _socialite.GetProvider("custom")
    
