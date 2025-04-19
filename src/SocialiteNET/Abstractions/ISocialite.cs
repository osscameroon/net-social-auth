using System;

namespace SocialiteNET.Abstractions;

/// <summary>
/// Main interface 
/// </summary>
public interface ISocialite
{
    /// <summary>
    /// Gets a specific OAuth provider, or the default provider
    /// </summary>
    /// <param name="driver">The driver name to use, or null for the default driver</param>
    /// <returns>Provider instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when no driver is specified and no default driver is configured</exception>
    IProvider GetProvider(string? driver = null);

    /// <summary>
    /// Builds an OAuth provider with custom configuration
    /// </summary>
    /// <param name="provider">Provider type</param>
    /// <param name="config">Provider configuration</param>
    /// <returns>Provider instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
    /// <exception cref="ArgumentException">Thrown when required config properties are missing</exception>
    [Obsolete("Please use the GetProvider(ProviderEnum provider, ProviderConfig config) method")] IProvider BuildProvider(string provider, ProviderConfig config);

    /// <summary>
    /// Builds an OAuth provider with custom configuration
    /// </summary>
    /// <param name="provider">Provider type</param>
    /// <param name="config">Provider configuration</param>
    /// <returns>Provider instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
    /// <exception cref="ArgumentException">Thrown when required config properties are missing</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when provider is not supported</exception>
    IProvider BuildProvider(ProviderEnum provider, ProviderConfig config);

    /// <summary>
    /// Adds a custom driver
    /// </summary>
    /// <param name="driver">Driver name</param>
    /// <param name="factory">Provider factory function</param>
    /// <returns>This Socialite instance</returns>
    /// <exception cref="ArgumentException">Thrown when driver is null or empty</exception>
    /// <exception cref="ArgumentNullException">Thrown when factory is null</exception>
    ISocialite Extend(string driver, Func<IServiceProvider, IProvider> factory);
}