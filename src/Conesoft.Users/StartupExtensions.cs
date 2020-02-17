using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace Conesoft.Users
{
    public static class StartupExtensions
    {
        private static void AddUsersAuthentication(this IServiceCollection services, string applicationName, string rootPath = "")
        {
            services
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(rootPath))
                .SetApplicationName(applicationName);

            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromDays(365);
                    options.SlidingExpiration = true;
                });

            services.AddSingleton(s => new UsersRootPath(rootPathDelegate(s)));
        }

        public static void UseUsers(this IApplicationBuilder app)
        {
            app.UseAuthentication();
        }
    }
}
