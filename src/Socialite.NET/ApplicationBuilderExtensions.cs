using System;
using Microsoft.AspNetCore.Builder;

namespace Socialite.NET;

/// <summary>
/// Extensions for the HTTP pipeline
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Socialite middleware
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder</returns>
    /// <exception cref="ArgumentNullException">Thrown when app is null</exception>
    public static IApplicationBuilder UseSocialite(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Add session and auth middleware
        app.UseSession();
        app.UseAuthentication();
            
        return app;
    }
}