using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;
using SocialiteNET.Abstractions;
using SocialiteNET.Abstractions.Exceptions;
using SocialiteNET.Providers.Google;
using SocialiteNET.UnitTests.Shared;

namespace SocialiteNET.UnitTests.Providers.Google;

public class GoogleProviderTests : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly GoogleProvider provider;
    private const string ClientId = "test-client-id";
    private const string ClientSecret = "test-client-secret";
    private const string RedirectUrl = "https://example.com/callback";
    private readonly HttpMessageHandlerMock messageHandlerMock;

    public GoogleProviderTests()
    {
        this.messageHandlerMock = new HttpMessageHandlerMock();
        this.httpClient = new HttpClient(this.messageHandlerMock);
        this.provider = new GoogleProvider(this.httpClient, ClientId, ClientSecret, RedirectUrl);
    }

    public void Dispose()
    {
        this.httpClient.Dispose();
    }

    [Fact]
    public async Task RedirectAsync_WithValidContext_ReturnsAuthUrl()
    {
        // Arrange
        HttpContext? httpContext = Substitute.For<HttpContext>();
        ISession? session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);

        // Act
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldStartWith("https://accounts.google.com/o/oauth2/auth");
        redirectUrl.ShouldContain($"client_id={ClientId}");
        redirectUrl.ShouldContain($"redirect_uri={WebUtility.UrlEncode(GoogleProviderTests.RedirectUrl)}");
        redirectUrl.ShouldContain("response_type=code");
        redirectUrl.ShouldContain("scope=openid%20profile%20email");

        session.Received().Set(
            Arg.Is<string>(key => key == "socialite:state"),
            Arg.Any<byte[]>()
        );
    }

    [Fact]
    public async Task RedirectAsync_WithStatelessMode_DoesNotSetStateInSession()
    {
        // Arrange
        HttpContext? httpContext = Substitute.For<HttpContext>();
        ISession? session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);

        this.provider.Stateless();

        // Act
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldStartWith("https://accounts.google.com/o/oauth2/auth");
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
        HttpContext? httpContext = Substitute.For<HttpContext>();
        ISession? session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);

        this.provider.WithPkce();

        // Act
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldStartWith("https://accounts.google.com/o/oauth2/auth");
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
            await this.provider.GetUserAsync(null));
    }

    [Fact]
    public async Task GetUserAsync_WithInvalidState_ThrowsInvalidStateException()
    {
        // Arrange
        HttpContext? httpContext = Substitute.For<HttpContext>();
        ISession? session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);

        QueryCollection queryCollection = new(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "state", "invalid-state" }
            }
        );
        httpContext.Request.Query.Returns(queryCollection);

        session.TryGetValue("socialite:state", out Arg.Any<byte[]>()!)
            .Returns(x =>
            {
                x[1] = System.Text.Encoding.UTF8.GetBytes("valid-state");
                return true;
            });

        // Act & Assert
        await Should.ThrowAsync<InvalidStateException>(async () =>
            await this.provider.GetUserAsync(httpContext));
    }

    [Fact]
    public async Task GetUserAsync_WithoutAuthCode_ThrowsAuthenticationException()
    {
        // Arrange
        HttpContext? httpContext = Substitute.For<HttpContext>();
        ISession? session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);

        this.provider.Stateless();

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
        HttpContext? httpContext = Substitute.For<HttpContext>();
        ISession? session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);

        this.provider.Stateless();

        QueryCollection queryCollection = new(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "code", "valid-auth-code" }
            }
        );
        httpContext.Request.Query.Returns(queryCollection);

        var tokenResponse = new
        {
            access_token = "test-access-token",
            refresh_token = "test-refresh-token",
            expires_in = 3600,
            scope = "openid profile email"
        };

        var userResponse = new
        {
            sub = "12345",
            name = "Test User",
            email = "test@example.com",
            picture = "https://example.com/avatar.jpg"
        };

        this.messageHandlerMock.SetupResponse(
            HttpMethod.Post,
            "https://www.googleapis.com/oauth2/v4/token",
            JsonSerializer.Serialize(tokenResponse)
        );

        this.messageHandlerMock.SetupResponse(
            HttpMethod.Get,
            "https://www.googleapis.com/oauth2/v3/userinfo",
            JsonSerializer.Serialize(userResponse)
        );

        // Act
        IUser user = await this.provider.GetUserAsync(httpContext);

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe("12345");
        user.Name.ShouldBe("Test User");
        user.Email.ShouldBe("test@example.com");
        user.Avatar.ShouldBe("https://example.com/avatar.jpg");
        user.AvatarOriginal.ShouldBe("https://example.com/avatar.jpg");
        user.Token.ShouldBe("test-access-token");
        user.RefreshToken.ShouldBe("test-refresh-token");
        user.ExpiresIn.ShouldBe(3600);
        user.ApprovedScopes.ShouldContain("openid");
        user.ApprovedScopes.ShouldContain("profile");
        user.ApprovedScopes.ShouldContain("email");
    }

    [Fact]
    public async Task GetUserFromTokenAsync_WithNullToken_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await this.provider.GetUserFromTokenAsync(null));
    }

    [Fact]
    public async Task GetUserFromTokenAsync_WithValidToken_ReturnsUser()
    {
        // Arrange
        var userResponse = new
        {
            sub = "12345",
            name = "Test User",
            email = "test@example.com",
            picture = "https://example.com/avatar.jpg"
        };

        this.messageHandlerMock.SetupResponse(
            HttpMethod.Get,
            "https://www.googleapis.com/oauth2/v3/userinfo",
            JsonSerializer.Serialize(userResponse)
        );

        // Act
        IUser user = await this.provider.GetUserFromTokenAsync("valid-token");

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe("12345");
        user.Name.ShouldBe("Test User");
        user.Email.ShouldBe("test@example.com");
        user.Avatar.ShouldBe("https://example.com/avatar.jpg");
        user.Token.ShouldBe("valid-token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithNullToken_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await this.provider.RefreshTokenAsync(null));
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
            scope = "openid profile email"
        };

        this.messageHandlerMock.SetupResponse(
            HttpMethod.Post,
            "https://www.googleapis.com/oauth2/v4/token",
            JsonSerializer.Serialize(tokenResponse)
        );

        // Act
        IToken token = await this.provider.RefreshTokenAsync("valid-refresh-token");

        // Assert
        token.ShouldNotBeNull();
        token.AccessToken.ShouldBe("new-access-token");
        token.RefreshToken.ShouldBe("new-refresh-token");
        token.ExpiresIn.ShouldBe(3600);
        token.ApprovedScopes.ShouldContain("openid");
        token.ApprovedScopes.ShouldContain("profile");
        token.ApprovedScopes.ShouldContain("email");
    }

    [Fact]
    public async Task AddScopes_WithValidScopes_AddsToScopesList()
    {
        // Arrange 
        string[] additionalScopes = ["calendar", "drive"];

        // Act
        this.provider.AddScopes(additionalScopes);

        HttpContext? httpContext = Substitute.For<HttpContext>();
        ISession? session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldContain("scope=openid%20profile%20email%20calendar%20drive");
    }

    [Fact]
    public async Task SetScopes_WithNewScopes_ReplacesExistingScopes()
    {
        // Arrange
        string[] newScopes = { "calendar", "drive" };

        // Act
        this.provider.SetScopes(newScopes);

        HttpContext? httpContext = Substitute.For<HttpContext>();
        ISession? session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldContain("scope=calendar%20drive");
        redirectUrl.ShouldNotContain("openid");
        redirectUrl.ShouldNotContain("profile");
        redirectUrl.ShouldNotContain("email");
    }

    [Fact]
    public async Task SetRedirectUrl_WithValidUrl_UpdatesRedirectUrl()
    {
        // Arrange
        string newRedirectUrl = "https://updated-example.com/callback";

        // Act
        this.provider.SetRedirectUrl(newRedirectUrl);

        HttpContext? httpContext = Substitute.For<HttpContext>();
        ISession? session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldContain($"redirect_uri={WebUtility.UrlEncode(newRedirectUrl)}");
    }

    [Fact]
    public void SetRedirectUrl_WithNullUrl_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => this.provider.SetRedirectUrl(null));
    }

    [Fact]
    public async Task With_AddsCustomParameters()
    {
        // Arrange
        var parameters = new { access_type = "offline", prompt = "consent" };

        // Act
        this.provider.With(parameters);

        HttpContext? httpContext = Substitute.For<HttpContext>();
        ISession? session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
        string redirectUrl = await this.provider.RedirectAsync(httpContext);

        // Assert
        redirectUrl.ShouldContain("access_type=offline");
        redirectUrl.ShouldContain("prompt=consent");
    }
}