using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Conesoft.Users
{
    public static class StartupExtensions
    {
        public static void AddUsers(this IServiceCollection services, Func<string> rootPathDelegate)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(365);
                options.SlidingExpiration = true;
            });

            UserController.RootPath = rootPathDelegate;
        }

        public static void AddUsers(this IServiceCollection services) => services.AddUsers(() => "");

        public static void AddUsers(this IServiceCollection services, string rootPath) => services.AddUsers(() => rootPath);

        public static void UseUsers(this IApplicationBuilder app)
        {
            app.UseAuthentication();
        }
    }
}
