using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;
using SocialiteNET.Abstractions;
using SocialiteNET.Abstractions.Exceptions;
using SocialiteNET.Providers.Github;
using SocialiteNET.UnitTests.Shared;

namespace SocialiteNET.UnitTests.Providers.Github;

public class GitHubProviderSecurityTests : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly GitHubProvider provider;
    private const string ClientId = "test-client-id";
    private const string ClientSecret = "test-client-secret";
    private const string RedirectUrl = "https://example.com/callback";
    private readonly HttpMessageHandlerMock messageHandlerMock;

    public GitHubProviderSecurityTests()
    {
        this.messageHandlerMock = new HttpMessageHandlerMock();
        this.httpClient = new HttpClient(this.messageHandlerMock);
        this.provider = new GitHubProvider(this.httpClient, ClientId, ClientSecret, RedirectUrl);
    }

    public void Dispose()
    {
        this.httpClient.Dispose();
    }

    [Fact]
    public async Task RedirectAsync_WithPKCE_GeneratesCodeChallengeAndVerifier()
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
    public async Task GetUserAsync_WithPKCE_UsesCodeVerifierForTokenRequest()
    {
        // Arrange
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
            
        // Enable PKCE mode
        this.provider.WithPkce();
            
        string codeVerifier = "test-code-verifier";
        session.TryGetValue("socialite:code_verifier", out Arg.Any<byte[]>()!)
            .Returns(x => {
                x[1] = Encoding.UTF8.GetBytes(codeVerifier);
                return true;
            });
            
        this.provider.Stateless();
            
        // Setup request with auth code
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
            }
        };
            
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
        await this.provider.GetUserAsync(httpContext);

        // Assert
        session.Received().TryGetValue("socialite:code_verifier", out Arg.Any<byte[]>()!);
    }

    [Fact]
    public async Task GetUserAsync_WithFailedTokenRequest_ThrowsAuthenticationException()
    {
        // Arrange
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
            
        //
        this.provider.Stateless();
            
        // Setup request with auth code
        QueryCollection queryCollection = new(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "code", "valid-auth-code" }
            }
        );
        httpContext.Request.Query.Returns(queryCollection);
            
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Post, 
            "https://github.com/login/oauth/access_token",
            "{ \"error\": \"bad_verification_code\", \"error_description\": \"The code passed is incorrect or expired\" }",
            HttpStatusCode.BadRequest
        );

        // Act & Assert
        AuthenticationException exception = await Should.ThrowAsync<AuthenticationException>(async () => 
            await this.provider.GetUserAsync(httpContext));
            
        exception.Message.ShouldBe("Failed to get access token");
    }

    [Fact]
    public async Task GetUserAsync_WithMissingAccessToken_ThrowsAuthenticationException()
    {
        // Arrange
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
            
        // Setup stateless mode to bypass state check
        this.provider.Stateless();
            
        QueryCollection queryCollection = new(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "code", "valid-auth-code" }
            }
        );
        httpContext.Request.Query.Returns(queryCollection);
            
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Post, 
            "https://github.com/login/oauth/access_token",
            "{ \"token_type\": \"bearer\", \"scope\": \"user:email\" }"
        );

        // Act & Assert
        AuthenticationException exception = await Should.ThrowAsync<AuthenticationException>(async () => 
            await this.provider.GetUserAsync(httpContext));
            
        exception.Message.ShouldBe("Access token not found in the response");
    }

    [Fact]
    public async Task GetUserAsync_WithNullAccessToken_ThrowsAuthenticationException()
    {
        // Arrange
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
            
        this.provider.Stateless();
            
        QueryCollection queryCollection = new(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "code", "valid-auth-code" }
            }
        );
        httpContext.Request.Query.Returns(queryCollection);
            
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Post, 
            "https://github.com/login/oauth/access_token",
            "{ \"access_token\": null, \"token_type\": \"bearer\", \"scope\": \"user:email\" }"
        );

        // Act & Assert
        AuthenticationException exception = await Should.ThrowAsync<AuthenticationException>(async () => 
            await this.provider.GetUserAsync(httpContext));
            
        exception.Message.ShouldBe("Access token not found in the response");
    }

    [Fact]
    public async Task GetUserByToken_WithInvalidToken_ThrowsAuthenticationException()
    {
        // Arrange
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Get, 
            "https://api.github.com/user",
            "{ \"message\": \"Bad credentials\", \"documentation_url\": \"https://developer.github.com/v3\" }",
            HttpStatusCode.Unauthorized
        );

        // Act & Assert
        AuthenticationException exception = await Should.ThrowAsync<AuthenticationException>(async () => 
            await this.provider.GetUserFromTokenAsync("invalid-token"));
            
        exception.Message.ShouldContain("Error retrieving user information from GitHub");
    }

    [Fact]
    public async Task GetUserByToken_WithFailedEmailsRequest_StillReturnsUser()
    {
        // Arrange
        var userResponse = new
        {
            id = 12345,
            login = "testuser",
            name = "Test User",
            email = (string?)null,
            avatar_url = "https://example.com/avatar.jpg"
        };
            
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Get, 
            "https://api.github.com/user",
            JsonSerializer.Serialize(userResponse)
        );
            
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Get, 
            "https://api.github.com/user/emails",
            "{ \"message\": \"Not Found\", \"documentation_url\": \"https://developer.github.com/v3\" }",
            HttpStatusCode.NotFound
        );

        // Act
        IUser user = await this.provider.GetUserFromTokenAsync("valid-token");

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe("12345");
        user.Nickname.ShouldBe("testuser");
        user.Name.ShouldBe("Test User");
        user.Email.ShouldBeNull(); //
        user.Avatar.ShouldBe("https://example.com/avatar.jpg");
    }

    [Fact]
    public async Task GetUserByToken_WithNoVerifiedEmails_EmailShouldBeNull()
    {
        // Arrange
        var userResponse = new
        {
            id = 12345,
            login = "testuser",
            name = "Test User",
            email = (string?)null,
            avatar_url = "https://example.com/avatar.jpg"
        };
            
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Get, 
            "https://api.github.com/user",
            JsonSerializer.Serialize(userResponse)
        );
            
        var emailsResponse = new[]
        {
            new
            {
                email = "testuser@example.com",
                primary = false,
                verified = true
            },
            new
            {
                email = "primary@example.com",
                primary = true,
                verified = false
            }
        };
            
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
        user.Email.ShouldBeNull();
    }

    [Fact]
    public async Task RefreshToken_WithFailedRequest_ThrowsAuthenticationException()
    {
        // Arrange
        this.messageHandlerMock.SetupResponse(
            HttpMethod.Post, 
            "https://github.com/login/oauth/access_token",
            "{ \"error\": \"invalid_request\", \"error_description\": \"Invalid refresh token\" }",
            HttpStatusCode.BadRequest
        );

        // Act & Assert
        AuthenticationException exception = await Should.ThrowAsync<AuthenticationException>(async () => 
            await this.provider.RefreshTokenAsync("invalid-refresh-token"));
            
        exception.Message.ShouldBe("Failed to refresh token");
    }
}