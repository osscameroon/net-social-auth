using Shouldly;
using SocialiteNET.Abstractions;
using SocialiteNET.Core;

namespace SocialiteNET.UnitTests.Core;

public class UserTests
{
    [Fact]
    public void SetRaw_WithValidData_SetsUserData()
    {
        // Arrange
        User user = new();
        Dictionary<string, object?> userData = new()
        {
            { "sub", "12345" },
            { "name", "Test User" },
            { "email", "test@example.com" }
        };

        // Act
        IUser result = user.SetRaw(userData);

        // Assert
        result.ShouldBe(user);
        user.UserData.ShouldBe(userData);
    }

    [Fact]
    public void SetRaw_WithNullData_SetsEmptyDictionary()
    {
        // Arrange
        User user = new();

        // Act
        IUser result = user.SetRaw(null);

        // Assert
        result.ShouldBe(user);
        user.UserData.ShouldNotBeNull();
        user.UserData.Count.ShouldBe(0);
    }

    [Fact]
    public void Map_WithMatchingProperties_MapsDataToProperties()
    {
        // Arrange
        User user = new();
        Dictionary<string, object?> attributes = new()
        {
            { "Id", "12345" },
            { "Name", "Test User" },
            { "Email", "test@example.com" },
            { "Nickname", "tester" },
            { "Avatar", "https://example.com/avatar.jpg" },
            { "AvatarOriginal", "https://example.com/avatar-original.jpg" },
            { "ProfileUrl", "https://example.com/profile" }
        };

        // Act
        IUser result = user.Map(attributes);

        // Assert
        result.ShouldBe(user);
        user.Id.ShouldBe("12345");
        user.Name.ShouldBe("Test User");
        user.Email.ShouldBe("test@example.com");
        user.Nickname.ShouldBe("tester");
        user.Avatar.ShouldBe("https://example.com/avatar.jpg");
        user.AvatarOriginal.ShouldBe("https://example.com/avatar-original.jpg");
        user.ProfileUrl.ShouldBe("https://example.com/profile");
    }

    [Fact]
    public void Map_WithNonExistentProperties_IgnoresThoseProperties()
    {
        // Arrange
        User user = new();
        Dictionary<string, object?> attributes = new()
        {
            { "Id", "12345" },
            { "NonExistentProperty", "should be ignored" }
        };

        // Act
        IUser result = user.Map(attributes);

        // Assert
        result.ShouldBe(user);
        user.Id.ShouldBe("12345");
    }

    [Fact]
    public void Map_WithNullAttributes_ThrowsArgumentNullException()
    {
        // Arrange
        User user = new();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => user.Map(null));
    }

    [Fact]
    public void SetToken_WithValidToken_SetsToken()
    {
        // Arrange
        User user = new();
        const string token = "valid-token";

        // Act
        IUser result = user.SetToken(token);

        // Assert
        result.ShouldBe(user);
        user.Token.ShouldBe(token);
    }

    [Fact]
    public void SetToken_WithNullOrEmptyToken_ThrowsArgumentNullException()
    {
        // Arrange
        User user = new();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => user.SetToken(null));
        Should.Throw<ArgumentNullException>(() => user.SetToken(string.Empty));
    }

    [Fact]
    public void SetRefreshToken_WithValidToken_SetsRefreshToken()
    {
        // Arrange
        User user = new();
        const string refreshToken = "valid-refresh-token";

        // Act
        IUser result = user.SetRefreshToken(refreshToken);

        // Assert
        result.ShouldBe(user);
        user.RefreshToken.ShouldBe(refreshToken);
    }

    [Fact]
    public void SetRefreshToken_WithNullToken_SetsRefreshTokenToNull()
    {
        // Arrange
        User user = new()
        {
            RefreshToken = "existing-token"
        };

        // Act
        IUser result = user.SetRefreshToken(null);

        // Assert
        result.ShouldBe(user);
        user.RefreshToken.ShouldBeNull();
    }

    [Fact]
    public void SetExpiresIn_WithPositiveValue_SetsExpiresIn()
    {
        // Arrange
        User user = new();
        int expiresIn = 3600;

        // Act
        IUser result = user.SetExpiresIn(expiresIn);

        // Assert
        result.ShouldBe(user);
        user.ExpiresIn.ShouldBe(expiresIn);
    }

    [Fact]
    public void SetApprovedScopes_WithValidScopes_SetsApprovedScopes()
    {
        // Arrange
        User user = new();
        string[] scopes = ["profile", "email", "openid"];

        // Act
        IUser result = user.SetApprovedScopes(scopes);

        // Assert
        result.ShouldBe(user);
        user.ApprovedScopes.ShouldBe(scopes);
    }

    [Fact]
    public void SetApprovedScopes_WithNullScopes_SetsEmptyArray()
    {
        // Arrange
        User user = new()
        {
            ApprovedScopes = ["profile", "email"]
        };

        // Act
        IUser result = user.SetApprovedScopes(null);

        // Assert
        result.ShouldBe(user);
        user.ApprovedScopes.ShouldNotBeNull();
        user.ApprovedScopes.ShouldBeEmpty();
    }
}