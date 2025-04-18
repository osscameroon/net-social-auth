using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using SocialiteNET.Abstractions;
using SocialiteNET.Core;
using SocialiteNET.Providers.Github;

namespace SocialiteNET.UnitTests.Core;

public class SocialiteManagerGitHubTests
{
    [Fact]
    public void GetProvider_WithGitHubDriver_ReturnsGitHubProvider()
    {
        // Arrange
        ServiceCollection services = new();
            
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().Returns(new HttpClient());
        services.AddSingleton(httpClientFactory);
            
        GitHubProvider gitHubProvider = new(
            new HttpClient(), 
            "test-client-id", 
            "test-client-secret", 
            "https://example.com/callback"
        );
        services.AddSingleton<GitHubProvider>(gitHubProvider);
        services.AddSingleton<IProvider>(sp => sp.GetRequiredService<GitHubProvider>());
            
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        SocialiteManager manager = new SocialiteManager(serviceProvider, "github");
        manager.RegisterDriver<GitHubProvider>("github");

        // Act
        IProvider provider = manager.GetProvider();

        // Assert
        provider.ShouldNotBeNull();
        provider.ShouldBeOfType<GitHubProvider>();
        provider.ShouldBe(gitHubProvider);
    }

    [Fact]
    public void GetProvider_WithUnregisteredDriver_ThrowsInvalidOperationException()
    {
        // Arrange
        ServiceCollection services = [];
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        SocialiteManager manager = new(serviceProvider, "github");

        // Act & Assert
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => manager.GetProvider());
        exception.Message.ShouldBe("Driver [github] not supported.");
    }

    [Fact]
    public void Extend_WithGitHubProviderFactory_RegistersCustomDriver()
    {
        // Arrange
        ServiceCollection services = [];
            
        // Setup HTTP client factory
        IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient().Returns(new HttpClient());
        services.AddSingleton(httpClientFactory);
            
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        SocialiteManager manager = new(serviceProvider);
            
        GitHubProvider gitHubProvider = new(
            new HttpClient(), 
            "test-client-id", 
            "test-client-secret", 
            "https://example.com/callback"
        );

        // Act
        manager.Extend("custom-github", _ => gitHubProvider);
        IProvider provider = manager.GetProvider("custom-github");

        // Assert
        provider.ShouldNotBeNull();
        provider.ShouldBe(gitHubProvider);
    }
}