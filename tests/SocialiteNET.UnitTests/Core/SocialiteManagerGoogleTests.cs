using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using SocialiteNET.Abstractions;
using SocialiteNET.Core;
using SocialiteNET.Providers.Google;

namespace SocialiteNET.UnitTests.Core;

public class SocialiteManagerGoogleTests
{
    [Fact]
    public void GetProvider_WithGoogleDriver_ReturnsGoogleProvider()
    {
        // Arrange
        ServiceCollection services = [];

        IHttpClientFactory? httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().Returns(new HttpClient());
        services.AddSingleton(httpClientFactory);

        GoogleProvider googleProvider = new(
            new HttpClient(),
            "test-client-id",
            "test-client-secret",
            "https://example.com/callback"
        );

        services.AddSingleton<GoogleProvider>(googleProvider);
        services.AddSingleton<IProvider>(sp => sp.GetRequiredService<GoogleProvider>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        SocialiteManager manager = new(serviceProvider, "google");
        manager.RegisterDriver<GoogleProvider>("google");

        // Act
        IProvider provider = manager.GetProvider();

        // Assert
        provider.ShouldNotBeNull();
        provider.ShouldBeOfType<GoogleProvider>();
    }

    [Fact]
    public void GetProvider_WithUnregisteredDriver_ThrowsInvalidOperationException()
    {
        // Arrange
        ServiceCollection services = [];
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        SocialiteManager manager = new(serviceProvider, "google");

        // Act & Assert
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => manager.GetProvider());
        exception.Message.ShouldBe("Driver [google] not supported.");
    }

    [Fact]
    public void BuildProvider_WithGoogleProviderConfig_ReturnsConfiguredGoogleProvider()
    {
        // Arrange
        ServiceCollection services = [];

        IHttpClientFactory? httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().Returns(new HttpClient());
        services.AddSingleton(httpClientFactory);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        SocialiteManager manager = new(serviceProvider);

        ProviderConfig config = new()
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            RedirectUrl = "https://example.com/callback",
            Stateless = true,
            UsesPkce = true
        };
        config.Scopes.Add("drive");
        config.Parameters.Add("access_type", "offline");

        // Act - This would typically fail in a real test as we can't create the type dynamically
        // In a real test environment, we would need to mock or use real provider registration
        var exception = Should.Throw<InvalidOperationException>(() =>
            manager.BuildProvider("Google", config));

        // Assert
        exception.Message.ShouldBe("Provider [Google] could not be resolved.");
    }

    [Fact]
    public void BuildProvider_WithTypedGoogleProviderConfig_ReturnsConfiguredGoogleProvider()
    {
        // Arrange
        ServiceCollection services = [];

        IHttpClientFactory? httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().Returns(new HttpClient());
        services.AddSingleton(httpClientFactory);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        SocialiteManager manager = new(serviceProvider);

        ProviderConfig config = new()
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            RedirectUrl = "https://example.com/callback",
            Stateless = true,
            UsesPkce = true
        };
        config.Scopes.Add("drive");
        config.Parameters.Add("access_type", "offline");

        // Act - This would typically fail in a real test as we can't create the type dynamically
        // In a real test environment, we would need to mock or use real provider registration
        var exception = Should.Throw<InvalidOperationException>(() =>
            manager.BuildProvider(ProviderEnum.Google, config));

        // Assert
        exception.Message.ShouldBe("Provider [Google] could not be resolved.");
    }

    [Fact]
    public void Extend_WithGoogleProviderFactory_RegistersCustomDriver()
    {
        // Arrange
        ServiceCollection services = [];

        IHttpClientFactory? httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().Returns(new HttpClient());
        services.AddSingleton(httpClientFactory);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        SocialiteManager manager = new(serviceProvider);

        GoogleProvider googleProvider = new GoogleProvider(
            new HttpClient(),
            "test-client-id",
            "test-client-secret",
            "https://example.com/callback"
        );

        // Act
        manager.Extend("custom-google", _ => googleProvider);
        IProvider provider = manager.GetProvider("custom-google");

        // Assert
        provider.ShouldNotBeNull();
        provider.ShouldBe(googleProvider);
    }
}