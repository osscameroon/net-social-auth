using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using SocialiteNET.Abstractions;
using SocialiteNET.Providers.Github;
using SocialiteNET.Providers.Google;

namespace SocialiteNET.Core;

/// <summary>
/// Main manager
/// </summary>
public class SocialiteManager : ISocialite
{
    private readonly IServiceProvider services;
    private readonly string? defaultDriver;
    private readonly Dictionary<string, Type> drivers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<IServiceProvider, IProvider>> customDriverFactories =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the SocialiteManager
    /// </summary>
    /// <param name="services">Service provider</param>
    /// <param name="defaultDriver">Default driver name</param>
    /// <exception cref="ArgumentNullException">Thrown when services is null</exception>
    public SocialiteManager(IServiceProvider services, string? defaultDriver = null)
    {
        this.services = services ?? throw new ArgumentNullException(nameof(services));
        this.defaultDriver = defaultDriver;
    }

    /// <inheritdoc />
    public IProvider GetProvider(string? driver = null)
    {
        driver ??= this.defaultDriver;

        if (string.IsNullOrEmpty(driver))
        {
            throw new InvalidOperationException("No Socialite driver was specified.");
        }

        if (this.customDriverFactories.TryGetValue(driver, out Func<IServiceProvider, IProvider>? factory))
        {
            IProvider provider = factory(this.services) ?? throw new InvalidOperationException($"Factory for driver [{driver}] returned null.");
            return provider;
        }

        if (!this.drivers.TryGetValue(driver, out Type? providerType))
        {
            throw new InvalidOperationException($"Driver [{driver}] not supported.");
        }


        try
        {
            return this.services.GetRequiredService(providerType) as IProvider
                   ?? throw new InvalidOperationException($"Could not cast provider of type {providerType.Name} to IProvider.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not resolve provider of type {providerType.Name}.", ex);
        }

    }

    /// <inheritdoc />
    public IProvider BuildProvider(string provider, ProviderConfig config)
    {
        if (string.IsNullOrEmpty(provider))
        {
            throw new ArgumentException("Provider name cannot be empty", nameof(provider));
        }

        ArgumentNullException.ThrowIfNull(config);

        config.Validate();

        using HttpClient httpClient = this.services.GetRequiredService<IHttpClientFactory>().CreateClient();

        Type providerType = Type.GetType($"SocialiteNET.Providers.{provider}.{provider}Provider, SocialiteNET.Providers.{provider}") ?? throw new InvalidOperationException($"Provider [{provider}] could not be resolved.");
        IProvider providerInstance = Activator.CreateInstance(
            providerType,
            httpClient,
            config.ClientId,
            config.ClientSecret,
            config.RedirectUrl) as IProvider ?? throw new InvalidOperationException($"Failed to create provider [{provider}].");

        providerInstance = providerInstance
            .SetScopes(config.Scopes.ToArray());

        if (config.Stateless)
        {
            providerInstance = providerInstance.Stateless();
        }

        if (config.UsesPkce)
        {
            providerInstance = providerInstance.WithPkce();
        }

        if (config.Parameters.Count > 0)
        {
            providerInstance = providerInstance.With(config.Parameters);
        }

        return providerInstance;
    }

    /// <inheritdoc />
    public IProvider BuildProvider(ProviderEnum provider, ProviderConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.Validate();

        using HttpClient httpClient = this.services.GetRequiredService<IHttpClientFactory>().CreateClient();

        switch (provider)
        {
            case ProviderEnum.Google:
                GoogleProvider googleProvider = new GoogleProvider(
                    httpClient,
                    config.ClientId,
                    config.ClientSecret,
                    config.RedirectUrl
                );

                if (config.Stateless)
                {
                    googleProvider.Stateless();
                }

                if (config.UsesPkce)
                {
                    googleProvider.WithPkce();
                }

                if (config.Parameters.Count > 0)
                {
                    googleProvider.With(config.Parameters);
                }

                return googleProvider;

            case ProviderEnum.Github:
                GitHubProvider githubProvider = new GitHubProvider(
                    httpClient,
                    config.ClientId,
                    config.ClientSecret,
                    config.RedirectUrl
                );

                if (config.Stateless)
                {
                    githubProvider.Stateless();
                }

                if (config.UsesPkce)
                {
                    githubProvider.WithPkce();
                }

                if (config.Parameters.Count > 0)
                {
                    githubProvider.With(config.Parameters);
                }

                return githubProvider;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }
    }

    /// <inheritdoc />
    public ISocialite Extend(string driver, Func<IServiceProvider, IProvider> factory)
    {
        if (string.IsNullOrEmpty(driver))
        {
            throw new ArgumentException("Driver name cannot be empty", nameof(driver));
        }

        ArgumentNullException.ThrowIfNull(factory);

        this.customDriverFactories[driver] = factory;

        return this;
    }

    /// <summary>
    /// Registers a provider driver
    /// </summary>
    /// <typeparam name="TProvider">Provider type</typeparam>
    /// <param name="name">Driver name</param>
    /// <exception cref="ArgumentException">Thrown when name is null or empty</exception>
    public void RegisterDriver<TProvider>(string name) where TProvider : IProvider
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Driver name cannot be empty", nameof(name));
        }

        this.drivers[name] = typeof(TProvider);
    }
}