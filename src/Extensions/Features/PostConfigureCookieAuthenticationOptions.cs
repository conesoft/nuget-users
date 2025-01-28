using Microsoft.Extensions.Options;

namespace Conesoft.Users.Extensions.Features;

// thanks to https://stackoverflow.com/a/47036122/1528847
public class PostConfigureCookieAuthenticationOptions(IOptions<Content.Options.UserOptions> userOptions) : IPostConfigureOptions<CookieAuthenticationOptions>
{
    void IPostConfigureOptions<CookieAuthenticationOptions>.PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        options.Cookie.Name = userOptions.Value.CookieName;
        options.ExpireTimeSpan = TimeSpan.FromDays(365);
        options.SlidingExpiration = true;
        options.ReturnUrlParameter = "redirectto";
        options.LoginPath = "/user/login";
        options.LogoutPath = "/user/logout";
    }
}
