using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Socialite.NET.Abstractions;
using Socialite.NET.Core;

namespace Socialite.NET;

/// <summary>
/// Extensions for configuring Socialite in ASP.NET Core
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Socialite to the services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Options configuration</param>
    /// <returns>Socialite builder</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null</exception>
    public static ISocialiteBuilder AddSocialite(
        this IServiceCollection services,
        Action<SocialiteOptions>? configureOptions = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        SocialiteOptions options = new SocialiteOptions();
        configureOptions?.Invoke(options);

        // Required services
        services.AddHttpClient();
        services.AddMemoryCache();
        services.AddSession(opt =>
        {
            opt.IdleTimeout = TimeSpan.FromMinutes(30);
            opt.Cookie.HttpOnly = true;
            opt.Cookie.IsEssential = true;
        });

        SocialiteBuilder builder = new SocialiteBuilder(services);

        services.AddSingleton<ISocialiteBuilder>(builder);

        services.AddSingleton<SocialiteManager>(sp =>
        {
            SocialiteManager manager = new SocialiteManager(sp, options.DefaultDriver);
            var socialiteBuilder = sp.GetRequiredService<ISocialiteBuilder>();
            foreach (var registration in socialiteBuilder.DriverRegistrations)
            {
                registration(manager);
            }
            return manager;
        });
        services.AddSingleton<ISocialite>(sp =>
            sp.GetRequiredService<SocialiteManager>());

        return builder;
    }
}

/// <summary>
/// Options for Socialite
/// </summary>
public class SocialiteOptions
{
    /// <summary>
    /// Default driver
    /// </summary>
    public string? DefaultDriver { get; set; }
}

/// <summary>
/// Interface for the Socialite builder
/// </summary>
public interface ISocialiteBuilder
{
    /// <summary>
    /// ASP.NET Core services
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// 
    /// </summary>
    IList<Action<SocialiteManager>> DriverRegistrations { get; }


}

/// <summary>
/// Builder for Socialite
/// </summary>
public class SocialiteBuilder : ISocialiteBuilder
{
    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public IList<Action<SocialiteManager>> DriverRegistrations { get; } =
        new List<Action<SocialiteManager>>();


    /// <summary>
    /// Initializes a new instance of the SocialiteBuilder
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null</exception>
    public SocialiteBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }
}