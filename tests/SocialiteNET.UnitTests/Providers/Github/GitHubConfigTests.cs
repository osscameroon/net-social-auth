using Shouldly;
using SocialiteNET.Providers.Github;

namespace SocialiteNET.UnitTests.Providers.Github;

public class GitHubConfigTests
{
    [Fact]
    public void Constructor_SetsDefaultScopes()
    {
        // Arrange & Act
        GitHubConfig config = new();

        // Assert
        config.Scopes.ShouldContain("user:email");
        config.Scopes.Count.ShouldBe(1);
    }

    [Fact]
    public void Validate_WithEmptyClientId_ThrowsArgumentException()
    {
        // Arrange
        GitHubConfig config = new()
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
        GitHubConfig config = new()
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
        GitHubConfig config = new()
        {
            ClientId = "test-client-id",
            ClientSecret = "test-secret",
            RedirectUrl = ""
        };

        // Act & Assert
        ArgumentException exception = Should.Throw<ArgumentException>(() => config.Validate());
        exception.Message.ShouldBe("RedirectUrl is required (Parameter 'RedirectUrl')");
    }

    [Fact]
    public void Validate_WithValidConfig_DoesNotThrow()
    {
        // Arrange
        GitHubConfig config = new()
        {
            ClientId = "test-client-id",
            ClientSecret = "test-secret",
            RedirectUrl = "https://example.com/callback"
        };

        // Act & Assert
        Should.NotThrow(() => config.Validate());
    }
}