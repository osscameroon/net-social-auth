using Shouldly;
using SocialiteNET.Providers.Google;

namespace SocialiteNET.UnitTests.Providers.Google;

public class GoogleConfigTests
{
    [Fact]
    public void Constructor_SetsDefaultScopes()
    {
        // Arrange & Act
        GoogleConfig config = new();

        // Assert
        config.Scopes.ShouldContain("openid");
        config.Scopes.ShouldContain("profile");
        config.Scopes.ShouldContain("email");
        config.Scopes.Count.ShouldBe(3);
    }

    [Fact]
    public void Constructor_SetsScopeSeparator()
    {
        // Arrange & Act
        GoogleConfig config = new();

        // Assert
        config.ScopeSeparator.ShouldBe(" ");
    }

    [Fact]
    public void Validate_WithEmptyClientId_ThrowsArgumentException()
    {
        // Arrange
        GoogleConfig config = new()
        {
            ClientId = "",
            ClientSecret = "test-secret",
            RedirectUrl = "https://example.com/callback"
        };

        // Act & Assert
        ArgumentException exception = Should.Throw<ArgumentException>(() => config.Validate());
        exception.Message.ShouldBe("ClientId is required (Parameter 'ClientId')");
    }

    [Fact]
    public void Validate_WithEmptyClientSecret_ThrowsArgumentException()
    {
        // Arrange
        GoogleConfig config = new()
        {
            ClientId = "test-client-id",
            ClientSecret = "",
            RedirectUrl = "https://example.com/callback"
        };

        // Act & Assert
        ArgumentException exception = Should.Throw<ArgumentException>(() => config.Validate());
        exception.Message.ShouldBe("ClientSecret is required (Parameter 'ClientSecret')");
    }

    [Fact]
    public void Validate_WithEmptyRedirectUrl_ThrowsArgumentException()
    {
        // Arrange
        GoogleConfig config = new()
        {
            ClientId = "test-client-id",
            ClientSecret = "test-secret",
            RedirectUrl = ""
        };

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => config.Validate());
        exception.Message.ShouldBe("RedirectUrl is required (Parameter 'RedirectUrl')");
    }

    [Fact]
    public void Validate_WithValidConfig_DoesNotThrow()
    {
        // Arrange
        GoogleConfig config = new()
        {
            ClientId = "test-client-id",
            ClientSecret = "test-secret",
            RedirectUrl = "https://example.com/callback"
        };

        // Act & Assert
        Should.NotThrow(() => config.Validate());
    }
}