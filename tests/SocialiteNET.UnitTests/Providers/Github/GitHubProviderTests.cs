using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;
using SocialiteNET.Abstractions;
using SocialiteNET.Abstractions.Exceptions;
using SocialiteNET.Providers.Github;
using SocialiteNET.UnitTests.Shared;

namespace SocialiteNET.UnitTests.Providers.Github;

public class GitHubProviderTests : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly GitHubProvider provider;
    private const string ClientId = "test-client-id";
    private const string ClientSecret = "test-client-secret";
    private const string RedirectUrl = "https://example.com/callback";
    private readonly HttpMessageHandlerMock messageHandlerMock;

    public GitHubProviderTests()
    {
        this.messageHandlerMock = new HttpMessageHandlerMock();
        this.httpClient = new HttpClient(this.messageHandlerMock);
        this.provider = new GitHubProvider(this.httpClient, ClientId, ClientSecret, RedirectUrl);
    }

    public void Dispose()
    {
        this.httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task RedirectAsync_WithValidContext_ReturnsAuthUrl()
    {
        // Arrange
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);

        // Act
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldStartWith("https://github.com/login/oauth/authorize");
        redirectUrl.ShouldContain($"client_id={ClientId}");
        redirectUrl.ShouldContain($"redirect_uri={WebUtility.UrlEncode(RedirectUrl)}");
        redirectUrl.ShouldContain("response_type=code");
        redirectUrl.ShouldContain("scope=user%3Aemail");
            
        session.Received().Set(
            Arg.Is<string>(key => key == "socialite:state"),
            Arg.Any<byte[]>()
        );
    }

    [Fact]
    public async Task RedirectAsync_WithStatelessMode_DoesNotSetStateInSession()
    {
        // Arrange
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
            
        this.provider.Stateless();

        // Act
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldStartWith("https://github.com/login/oauth/authorize");
        redirectUrl.ShouldContain($"client_id={ClientId}");
        redirectUrl.ShouldNotContain("state=");
            
        session.DidNotReceive().Set(
            Arg.Is<string>(key => key == "socialite:state"),
            Arg.Any<byte[]>()
        );
    }

    [Fact]
    public async Task RedirectAsync_WithPKCE_SetsCodeChallengeParameters()
    {
        // Arrange
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
            
        this.provider.WithPkce();

        // Act
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldStartWith("https://github.com/login/oauth/authorize");
        redirectUrl.ShouldContain("code_challenge=");
        redirectUrl.ShouldContain("code_challenge_method=S256");
            
        session.Received().Set(
            Arg.Is<string>(key => key == "socialite:code_verifier"),
            Arg.Any<byte[]>()
        );
    }

    [Fact]
    public async Task GetUserAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () => 
            await this.provider.GetUserAsync(null!));
    }

    [Fact]
    public async Task GetUserAsync_WithoutAuthCode_ThrowsAuthenticationException()
    {
        // Arrange
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
            
        // Setup stateless mode to bypass state check
        this.provider.Stateless();
            
        // Setup request query without code parameter
        QueryCollection queryCollection = new();
        httpContext.Request.Query.Returns(queryCollection);

        // Act & Assert
        AuthenticationException exception = await Should.ThrowAsync<AuthenticationException>(async () => 
            await this.provider.GetUserAsync(httpContext));
        exception.Message.ShouldBe("Authorization code not found in the request");
    }

    [Fact]
    public async Task GetUserAsync_WithValidCodeAndResponse_ReturnsUser()
    {
        // Arrange
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
            
        // Setup stateless mode to bypass state check
        this.provider.Stateless();
            
        // Setup request query with code parameter
        QueryCollection queryCollection = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "code", "valid-auth-code" }
            }
        );
        httpContext.Request.Query.Returns(queryCollection);
            
        // Setup token response
        var tokenResponse = new
        {
            access_token = "test-access-token",
            refresh_token = "test-refresh-token",
            expires_in = 3600,
            scope = "user:email"
        };
            
        var userResponse = new
        {
            id = 12345,
            login = "testuser",
            name = "Test User",
            email = (string?)null,
            avatar_url = "https://example.com/avatar.jpg"
        };
            
        var emailsResponse = new[]
        {
            new
            {
                email = "testuser@example.com",
                primary = true,
                verified = true
            },
            new
            {
                email = "secondary@example.com",
                primary = false,
                verified = true
            }
        };
            
        // Configure HTTP responses
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Post, 
            "https://github.com/login/oauth/access_token",
            JsonSerializer.Serialize(tokenResponse)
        );
            
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Get, 
            "https://api.github.com/user",
            JsonSerializer.Serialize(userResponse)
        );
            
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Get, 
            "https://api.github.com/user/emails",
            JsonSerializer.Serialize(emailsResponse)
        );

        // Act
        IUser user = await this.provider.GetUserAsync(httpContext);

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe("12345");
        user.Nickname.ShouldBe("testuser");
        user.Name.ShouldBe("Test User");
        user.Email.ShouldBe("testuser@example.com");
        user.Avatar.ShouldBe("https://example.com/avatar.jpg");
        user.Token.ShouldBe("test-access-token");
        user.RefreshToken.ShouldBe("test-refresh-token");
        user.ExpiresIn.ShouldBe(3600);
    }

    [Fact]
    public async Task GetUserAsync_WithoutUserEmailScope_DoesNotFetchEmail()
    {
        // Arrange
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
            
        // Remove user:email scope
        this.provider.SetScopes("repo");
            
        // Setup stateless mode to bypass state check
        this.provider.Stateless();
            
        // Setup request query with code parameter
        QueryCollection queryCollection = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "code", "valid-auth-code" }
            }
        );
        httpContext.Request.Query.Returns(queryCollection);
            
        // Setup token response
        var tokenResponse = new
        {
            access_token = "test-access-token",
            refresh_token = "test-refresh-token",
            expires_in = 3600,
            scope = "repo"
        };
            
        // Setup user response
        var userResponse = new
        {
            id = 12345,
            login = "testuser",
            name = "Test User",
            email = (string?)null, // No email in profile
            avatar_url = "https://example.com/avatar.jpg"
        };
            
        // Configure HTTP responses
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Post, 
            "https://github.com/login/oauth/access_token",
            JsonSerializer.Serialize(tokenResponse)
        );
            
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Get, 
            "https://api.github.com/user",
            JsonSerializer.Serialize(userResponse)
        );

        // Act
        IUser user = await this.provider.GetUserAsync(httpContext);

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe("12345");
        user.Nickname.ShouldBe("testuser");
        user.Name.ShouldBe("Test User");
        user.Email.ShouldBeNull(); // Email should be null since we don't have the user:email scope
    }

    [Fact]
    public async Task GetUserFromTokenAsync_WithNullToken_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () => 
            await this.provider.GetUserFromTokenAsync(null!));
    }

    [Fact]
    public async Task GetUserFromTokenAsync_WithValidToken_ReturnsUser()
    {
        // Arrange
        var userResponse = new
        {
            id = 12345,
            login = "testuser",
            name = "Test User",
            email = (string?)null, // Email will be fetched separately
            avatar_url = "https://example.com/avatar.jpg"
        };
            
        var emailsResponse = new[]
        {
            new
            {
                email = "testuser@example.com",
                primary = true,
                verified = true
            }
        };
            
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Get, 
            "https://api.github.com/user",
            JsonSerializer.Serialize(userResponse)
        );
            
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Get, 
            "https://api.github.com/user/emails",
            JsonSerializer.Serialize(emailsResponse)
        );

        // Act
        IUser user = await this.provider.GetUserFromTokenAsync("valid-token");

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe("12345");
        user.Nickname.ShouldBe("testuser");
        user.Name.ShouldBe("Test User");
        user.Email.ShouldBe("testuser@example.com");
        user.Avatar.ShouldBe("https://example.com/avatar.jpg");
        user.Token.ShouldBe("valid-token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithNullToken_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () => 
            await this.provider.RefreshTokenAsync(null!));
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewToken()
    {
        // Arrange
        var tokenResponse = new
        {
            access_token = "new-access-token",
            refresh_token = "new-refresh-token",
            expires_in = 3600,
            scope = "user:email"
        };
            
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Post, 
            "https://github.com/login/oauth/access_token",
            JsonSerializer.Serialize(tokenResponse)
        );

        // Act
        IToken token = await this.provider.RefreshTokenAsync("valid-refresh-token");

        // Assert
        token.ShouldNotBeNull();
        token.AccessToken.ShouldBe("new-access-token");
        token.RefreshToken.ShouldBe("new-refresh-token");
        token.ExpiresIn.ShouldBe(3600);
        token.ApprovedScopes.ShouldContain("user:email");
    }

    [Fact]
    public async Task AddScopes_WithValidScopes_AddsToScopesList()
    {
        // Arrange
        string[] additionalScopes = ["repo", "user"];

        // Act
        this.provider.AddScopes(additionalScopes);
            
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldContain("scope=user%3Aemail%20repo%20user");
    }

    [Fact]
    public async Task SetScopes_WithNewScopes_ReplacesExistingScopes()
    {
        // Arrange
        string[] newScopes = ["repo", "user"];

        // Act
        this.provider.SetScopes(newScopes);
            
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldContain("scope=repo%20user");
        redirectUrl.ShouldNotContain("user%3Aemail");
    }

    [Fact]
    public async Task SetRedirectUrl_WithValidUrl_UpdatesRedirectUrl()
    {
        // Arrange
        const string newRedirectUrl = "https://updated-example.com/callback";

        // Act
        this.provider.SetRedirectUrl(newRedirectUrl);
            
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldContain($"redirect_uri={WebUtility.UrlEncode(newRedirectUrl)}");
    }

    [Fact]
    public async Task With_AddsCustomParameters()
    {
        // Arrange
        object parameters = new { allow_signup = "false", login = "testuser" };

        // Act
        this.provider.With(parameters);
            
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldContain("allow_signup=false");
        redirectUrl.ShouldContain("login=testuser");
    }
}