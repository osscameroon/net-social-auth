using Shouldly;
using SocialiteNET.Core;

namespace SocialiteNET.UnitTests.Core;

public class TokenTests
{
    [Fact]
    public void Constructor_WithValidParameters_InitializesProperties()
    {
        // Arrange
        const string accessToken = "test-access-token";
        const string refreshToken = "test-refresh-token";
        const int expiresIn = 3600;
        string[] scopes = ["openid", "profile", "email"];

        // Act
        Token token = new(accessToken, refreshToken, expiresIn, scopes);

        // Assert
        token.AccessToken.ShouldBe(accessToken);
        token.RefreshToken.ShouldBe(refreshToken);
        token.ExpiresIn.ShouldBe(expiresIn);
        token.ApprovedScopes.ShouldBe(scopes);
    }

    [Fact]
    public void Constructor_WithNullAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        string accessToken = null;
        const string refreshToken = "test-refresh-token";
        const int expiresIn = 3600;
        string[] scopes = ["openid", "profile", "email"];

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new Token(accessToken, refreshToken, expiresIn, scopes));
    }

    [Fact]
    public void Constructor_WithEmptyAccessToken_ThrowsArgumentNullException()
    {
        // Arrange
        string accessToken = string.Empty;
        const string refreshToken = "test-refresh-token";
        const int expiresIn = 3600;
        string[] scopes = ["openid", "profile", "email"];

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            new Token(accessToken, refreshToken, expiresIn, scopes));
    }

    [Fact]
    public void Constructor_WithNullRefreshToken_SetsRefreshTokenToNull()
    {
        // Arrange
        const string accessToken = "test-access-token";
        string refreshToken = null;
        const int expiresIn = 3600;
        string[] scopes = ["openid", "profile", "email"];

        // Act
        Token token = new(accessToken, refreshToken, expiresIn, scopes);

        // Assert
        token.AccessToken.ShouldBe(accessToken);
        token.RefreshToken.ShouldBeNull();
        token.ExpiresIn.ShouldBe(expiresIn);
        token.ApprovedScopes.ShouldBe(scopes);
    }

    [Fact]
    public void Constructor_WithZeroExpiresIn_SetsExpiresInToZero()
    {
        // Arrange
        const string accessToken = "test-access-token";
        const string refreshToken = "test-refresh-token";
        const int expiresIn = 0;
        string[] scopes = ["openid", "profile", "email"];

        // Act
        Token token = new(accessToken, refreshToken, expiresIn, scopes);

        // Assert
        token.AccessToken.ShouldBe(accessToken);
        token.RefreshToken.ShouldBe(refreshToken);
        token.ExpiresIn.ShouldBe(0);
        token.ApprovedScopes.ShouldBe(scopes);
    }

    [Fact]
    public void Constructor_WithNegativeExpiresIn_SetsExpiresInToNegativeValue()
    {
        // Arrange
        const string accessToken = "test-access-token";
        const string refreshToken = "test-refresh-token";
        int expiresIn = -100;
        string[] scopes = { "openid", "profile", "email" };

        // Act
        Token token = new(accessToken, refreshToken, expiresIn, scopes);

        // Assert
        token.AccessToken.ShouldBe(accessToken);
        token.RefreshToken.ShouldBe(refreshToken);
        token.ExpiresIn.ShouldBe(-100);
        token.ApprovedScopes.ShouldBe(scopes);
    }

    [Fact]
    public void Constructor_WithNullScopes_SetsApprovedScopesToEmptyArray()
    {
        // Arrange
        const string accessToken = "test-access-token";
        const string refreshToken = "test-refresh-token";
        const int expiresIn = 3600;
        IEnumerable<string> scopes = null;

        // Act
        Token token = new(accessToken, refreshToken, expiresIn, scopes);

        // Assert
        token.AccessToken.ShouldBe(accessToken);
        token.RefreshToken.ShouldBe(refreshToken);
        token.ExpiresIn.ShouldBe(expiresIn);
        token.ApprovedScopes.ShouldNotBeNull();
        token.ApprovedScopes.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyScopes_SetsApprovedScopesToEmptyArray()
    {
        // Arrange
        const string accessToken = "test-access-token";
        const string refreshToken = "test-refresh-token";
        const int expiresIn = 3600;
        string[] scopes = [];

        // Act
        Token token = new(accessToken, refreshToken, expiresIn, scopes);

        // Assert
        token.AccessToken.ShouldBe(accessToken);
        token.RefreshToken.ShouldBe(refreshToken);
        token.ExpiresIn.ShouldBe(expiresIn);
        token.ApprovedScopes.ShouldBe(scopes);
    }
}