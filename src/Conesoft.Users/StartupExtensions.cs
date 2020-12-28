using Conesoft.Files;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Conesoft.Users
{
    public static class StartupExtensions
    {
        public static void AddUsers(this IServiceCollection services, string applicationName, Directory rootDirectory)
        {
            services
                .AddDataProtection()
                .PersistKeysToFileSystem(rootDirectory.Info)
                .SetApplicationName(applicationName);

            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Cookie.Name = applicationName.Replace(' ', '-');
                    options.ExpireTimeSpan = TimeSpan.FromDays(365);
                    options.SlidingExpiration = true;
                });

            services.AddSingleton(s => new UsersRootDirectory(applicationName, rootDirectory));

            services.AddControllers().AddApplicationPart(typeof(StartupExtensions).Assembly);
        }

        public static void UseUsers(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseCookieCatcher(app.ApplicationServices.GetService<UsersRootDirectory>().ApplicationName.Replace(' ', '-'));
        }
    }
}
