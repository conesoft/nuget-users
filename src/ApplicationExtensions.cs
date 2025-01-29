using Conesoft.Users.Extensions.Features;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Conesoft.Users;

public static class ApplicationExtensions
{
    public static WebApplicationBuilder AddUsers<Dependency>(this WebApplicationBuilder webApplication, Action<Content.Options.UserOptions, Dependency> configuration) where Dependency : class
    {
        var services = webApplication.Services;

        services.AddOptions<Content.Options.UserOptions>().Configure(configuration);
        services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, Extensions.Features.PostConfigureCookieAuthenticationOptions>();
        services.AddAuthentication(CookieScheme.Cadas).AddCookie(CookieScheme.Cadas);
        services.AddSingleton<IClaimsTransformation, RoleClaimsTransformation>();
        services.AddTransient<Content.Storage.Directory>();
        services.AddAntiforgery();
        services.AddCascadingAuthenticationState();
        services.AddAuthorization();

        return webApplication;
    }


    public static WebApplication MapUsers(this WebApplication app)
    {
        app
            .UseAuthentication()
            .UseAuthorization()
            .UseAntiforgery()
            ;

        app.MapApi();

        return app;
    }
}