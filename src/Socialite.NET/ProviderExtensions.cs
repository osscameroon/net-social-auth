using System;
using Microsoft.Extensions.DependencyInjection;
using Socialite.NET.Abstractions;
using Socialite.NET.Providers.GitHub;
using Socialite.NET.Providers.Google;

namespace Socialite.NET;

/// <summary>
/// Extensions for adding providers
/// </summary>
public static class ProviderExtensions
{
    /// <summary>
    /// Adds the Google provider
    /// </summary>
    /// <param name="builder">Socialite builder</param>
    /// <param name="configureOptions">Options configuration</param>
    /// <returns>Socialite builder</returns>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null</exception>
    public static ISocialiteBuilder AddGoogle(
        this ISocialiteBuilder builder,
        Action<GoogleConfig> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ArgumentNullException.ThrowIfNull(configureOptions);

        GoogleConfig options = new GoogleConfig();
        configureOptions(options);

        builder.Services.Configure<GoogleConfig>(opt =>
        {
            opt.ClientId = options.ClientId;
            opt.ClientSecret = options.ClientSecret;
            opt.RedirectUrl = options.RedirectUrl;
            opt.Stateless = options.Stateless;
            opt.UsesPkce = options.UsesPkce;
            opt.ScopeSeparator = options.ScopeSeparator;

            foreach (string scope in options.Scopes)
            {
                if (!string.IsNullOrEmpty(scope) && !opt.Scopes.Contains(scope))
                {
                    opt.Scopes.Add(scope);
                }
            }

            foreach ((string key, string value) in options.Parameters)
            {
                opt.Parameters[key] = value;
            }
        });

        builder.Services.AddHttpClient<GoogleProvider>();
        builder.Services.AddTransient<GoogleProvider>();
        builder.Services.AddTransient<IProvider>(sp => sp.GetRequiredService<GoogleProvider>());

        builder.DriverRegistrations.Add(manager =>
            manager.RegisterDriver<GoogleProvider>("google"));

        return builder;
    }

    /// <summary>
    /// Adds the GitHub provider
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    public static ISocialiteBuilder AddGithub(this ISocialiteBuilder builder, Action<GitHubConfig> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ArgumentNullException.ThrowIfNull(configureOptions);

        GitHubConfig options = new GitHubConfig();
        configureOptions(options);


        builder.Services.Configure<GitHubConfig>(opt =>
        {
            opt.ClientId = options.ClientId;
            opt.ClientSecret = options.ClientSecret;
            opt.RedirectUrl = options.RedirectUrl;
            opt.Stateless = options.Stateless;
            opt.UsesPkce = options.UsesPkce;
            opt.ScopeSeparator = options.ScopeSeparator;
        });

        foreach (string scope in options.Scopes)
        {
            if (!string.IsNullOrEmpty(scope) && !options.Scopes.Contains(scope))
            {
                options.Scopes.Add(scope);
            }

            foreach ((string key, string value) in options.Parameters)
            {
                options.Parameters[key] = value;
            }
        }

        builder.Services.AddHttpClient<GitHubProvider>();
        builder.Services.AddTransient<GitHubProvider>();
        builder.Services.AddTransient<IProvider>(sp => sp.GetRequiredService<GitHubProvider>());

        builder.DriverRegistrations.Add(manager =>
            manager.RegisterDriver<GitHubProvider>("github"));


        return builder;
    }
}