using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Socialite.NET.Abstractions;
using Socialite.NET.Providers.Google;

namespace Socialite.NET.Core;

/// <summary>
/// Main manager
/// </summary>
public class SocialiteManager : ISocialite
{
    private readonly IServiceProvider _services;
    private readonly string? _defaultDriver;
    private readonly Dictionary<string, Type> _drivers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<IServiceProvider, IProvider>> _customDriverFactories =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the SocialiteManager
    /// </summary>
    /// <param name="services">Service provider</param>
    /// <param name="defaultDriver">Default driver name</param>
    /// <exception cref="ArgumentNullException">Thrown when services is null</exception>
    public SocialiteManager(IServiceProvider services, string? defaultDriver = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _defaultDriver = defaultDriver;
    }

    /// <inheritdoc />
    public IProvider GetProvider(string? driver = null)
    {
        IProvider pr = (IProvider)_services.GetRequiredService(typeof(IProvider));
        driver ??= _defaultDriver;

        if (string.IsNullOrEmpty(driver))
        {
            throw new InvalidOperationException("No Socialite driver was specified.");
        }

        // Check custom drivers first
        if (_customDriverFactories.TryGetValue(driver, out Func<IServiceProvider, IProvider>? factory))
        {
            IProvider? provider = factory(_services) ?? throw new InvalidOperationException($"Factory for driver [{driver}] returned null.");
            return provider;
        }

        // Then check registered drivers
        if (!_drivers.TryGetValue(driver, out Type? providerType))
        {
            throw new InvalidOperationException($"Driver [{driver}] not supported.");
        }

        //var pr = (IProvider) _services.GetRequiredService(typeof(IProvider));

        try
        {
            return _services.GetRequiredService(providerType) as IProvider
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

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        config.Validate();

        using HttpClient httpClient = _services.GetRequiredService<IHttpClientFactory>().CreateClient();

        // Try to resolve provider type
        Type? providerType = Type.GetType($"Socialite.NET.Providers.{provider}.{provider}Provider, Socialite.NET.Providers.{provider}") ?? throw new InvalidOperationException($"Provider [{provider}] could not be resolved.");
        IProvider? providerInstance = Activator.CreateInstance(
            providerType,
            httpClient,
            config.ClientId,
            config.ClientSecret,
            config.RedirectUrl) as IProvider ?? throw new InvalidOperationException($"Failed to create provider [{provider}].");

        // Configure instance
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
    public ISocialite Extend(string driver, Func<IServiceProvider, IProvider> factory)
    {
        if (string.IsNullOrEmpty(driver))
        {
            throw new ArgumentException("Driver name cannot be empty", nameof(driver));
        }

        ArgumentNullException.ThrowIfNull(factory);

        _customDriverFactories[driver] = factory;

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

        _drivers[name] = typeof(TProvider);
    }
}