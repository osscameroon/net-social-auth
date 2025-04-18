using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using SocialiteNET.Providers.Google;
using SocialiteNET.UnitTests.Shared;

namespace SocialiteNET.UnitTests.Providers.Google;

public class GoogleProviderOptionsTests
{
    private readonly HttpClient httpClient;
    private readonly HttpMessageHandlerMock messageHandlerMock;
    private readonly GoogleConfig config;
    private readonly IOptions<GoogleConfig> options;

    public GoogleProviderOptionsTests()
    {
        this.messageHandlerMock = new HttpMessageHandlerMock();
        this.httpClient = new HttpClient(this.messageHandlerMock);

        this.config = new GoogleConfig
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            RedirectUrl = "https://example.com/callback",
            Stateless = true,
            UsesPkce = true
        };
        this.config.Scopes.Add("drive");
        this.config.Parameters.Add("access_type", "offline");

        this.options = Substitute.For<IOptions<GoogleConfig>>();
        this.options.Value.Returns(this.config);
    }

    [Fact]
    public async Task Constructor_WithOptions_ConfiguresProviderCorrectly()
    {
        // Act
        GoogleProvider provider = new(this.httpClient, this.options);

        // Assert -
        HttpContext? httpContext = Substitute.For<HttpContext>();
        ISession? session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);

        string redirectUrl = await provider.RedirectAsync(httpContext);

        redirectUrl.ShouldStartWith("https://accounts.google.com/o/oauth2/auth");
        redirectUrl.ShouldContain($"client_id={this.config.ClientId}");
        redirectUrl.ShouldContain($"redirect_uri={WebUtility.UrlEncode(this.config.RedirectUrl)}");
        redirectUrl.ShouldContain("scope=openid%20profile%20email%20drive");
        redirectUrl.ShouldContain("access_type=offline");

        // Stateless mode was enabled in config
        redirectUrl.ShouldNotContain("state=");

        // PKCE was enabled in config
        redirectUrl.ShouldContain("code_challenge=");
        redirectUrl.ShouldContain("code_challenge_method=S256");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new GoogleProvider(this.httpClient, null));
    }

    [Fact]
    public void Constructor_WithInvalidConfig_ThrowsArgumentException()
    {
        // Arrange
        GoogleConfig invalidConfig = new()
        {
            ClientId = "",
            ClientSecret = "",
            RedirectUrl = ""
        };

        IOptions<GoogleConfig>? invalidOptions = Substitute.For<IOptions<GoogleConfig>>();
        invalidOptions.Value.Returns(invalidConfig);

        // Act & Assert
        var exception =
            Should.Throw<ArgumentException>(() => new GoogleProvider(this.httpClient, invalidOptions));
        exception.Message.ShouldBe("ClientId is required (Parameter 'ClientId')");
    }

    [Fact]
    public async Task FluentConfiguration_OverridesOptionsConfiguration()
    {
        // Arrange
        GoogleProvider provider = new(this.httpClient, this.options);

        // Act - Override the settings with fluent configuration
        provider
            .Stateless()
            .WithPkce()
            .SetScopes("calendar")
            .SetRedirectUrl("https://changed-example.com/callback")
            .With(new { prompt = "consent" });

        // Assert 
        HttpContext? httpContext = Substitute.For<HttpContext>();
        ISession? session = Substitute.For<ISession>();
        httpContext.Session.Returns(session);

        string redirectUrl = await provider.RedirectAsync(httpContext);

        redirectUrl.ShouldContain($"redirect_uri={WebUtility.UrlEncode("https://changed-example.com/callback")}");
        redirectUrl.ShouldContain("scope=calendar");
        redirectUrl.ShouldNotContain("openid");
    }
}