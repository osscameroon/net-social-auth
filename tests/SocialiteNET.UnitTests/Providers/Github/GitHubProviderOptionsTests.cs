using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using SocialiteNET.Providers.Github;
using SocialiteNET.UnitTests.Shared;

namespace SocialiteNET.UnitTests.Providers.Github;

public class GitHubProviderOptionsTests
{
    private readonly HttpClient httpClient;
    private readonly HttpMessageHandlerMock messageHandlerMock;
    private readonly GitHubConfig config;
    private readonly IOptions<GitHubConfig> options;

    public GitHubProviderOptionsTests()
    {
        this.messageHandlerMock = new HttpMessageHandlerMock();
        this.httpClient = new HttpClient(this.messageHandlerMock);
            
        this.config = new GitHubConfig
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            RedirectUrl = "https://example.com/callback",
            Stateless = true,
            UsesPkce = true
        };
        this.config.Scopes.Add("repo");
        this.config.Parameters.Add("allow_signup", "false");
            
        this.options = Substitute.For<IOptions<GitHubConfig>>();
        this.options.Value.Returns(this.config);
    }

    [Fact]
    public void Constructor_WithOptions_ConfiguresProviderCorrectly()
    {
        // Act
        GitHubProvider provider = new(this.httpClient, this.options);
            
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
            
        string redirectUrl = provider.RedirectAsync(httpContext).Result;
            
        redirectUrl.ShouldStartWith("https://github.com/login/oauth/authorize");
        redirectUrl.ShouldContain($"client_id={this.config.ClientId}");
        redirectUrl.ShouldContain($"redirect_uri={WebUtility.UrlEncode(this.config.RedirectUrl)}");
        redirectUrl.ShouldContain("scope=user%3Aemail%20repo");
        redirectUrl.ShouldContain("allow_signup=false");
            
        redirectUrl.ShouldNotContain("state=");
            
        redirectUrl.ShouldContain("code_challenge=");
        redirectUrl.ShouldContain("code_challenge_method=S256");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new GitHubProvider(this.httpClient, (IOptions<GitHubConfig>)null));
    }

    [Fact]
    public void Constructor_WithInvalidConfig_ThrowsArgumentException()
    {
        // Arrange
        GitHubConfig invalidConfig = new()
        {
            ClientId = "",
            ClientSecret = "",
            RedirectUrl = ""
        };
            
        IOptions<GitHubConfig> invalidOptions = Substitute.For<IOptions<GitHubConfig>>();
        invalidOptions.Value.Returns(invalidConfig);

        // Act & Assert
        ArgumentException exception = Should.Throw<ArgumentException>(() => new GitHubProvider(this.httpClient, invalidOptions));
        exception.Message.ShouldBe("ClientId is required (Parameter 'ClientId')");
    }

    [Fact]
    public async Task FluentConfiguration_OverridesOptionsConfiguration()
    {
        // Arrange
        GitHubProvider provider = new(this.httpClient, this.options);
            
        provider
            .Stateless()
            .WithPkce()
            .SetScopes("admin:org")
            .SetRedirectUrl("https://changed-example.com/callback")
            .With(new { login = "preferred-login" });
            
        // Assert 
        HttpContext httpContext = Substitute.For<HttpContext>();
        ISession session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);
            
        string redirectUrl = await provider.RedirectAsync(httpContext);
            
        redirectUrl.ShouldContain($"redirect_uri={WebUtility.UrlEncode("https://changed-example.com/callback")}");
        redirectUrl.ShouldContain("scope=admin%3Aorg");
        redirectUrl.ShouldNotContain("user%3Aemail"); 
        redirectUrl.ShouldNotContain("repo");
        redirectUrl.ShouldContain("login=preferred-login");
        redirectUrl.ShouldContain("allow_signup=false"); 
    }
}