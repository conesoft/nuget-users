using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Conesoft.Users
{
    public static class StartupExtensions
    {
        private static void AddUsersAuthentication(this IServiceCollection services)
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
        }

        public static void AddUsers(this IServiceCollection services, Func<IServiceProvider, string> rootPathDelegate)
        {
            services.AddUsersAuthentication();
            services.AddSingleton(s => new UsersRootPath(rootPathDelegate(s)));
        }

        public static void AddUsers(this IServiceCollection services)
        {
            services.AddUsersAuthentication();
            services.AddSingleton(new UsersRootPath(""));
        }

        public static void AddUsers(this IServiceCollection services, string rootPath)
        {
            services.AddUsersAuthentication();
            services.AddSingleton(new UsersRootPath(rootPath));
        }

        public static void UseUsers(this IApplicationBuilder app)
        {
            app.UseAuthentication();
        }
    }
}
