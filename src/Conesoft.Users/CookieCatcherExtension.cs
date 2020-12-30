using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Conesoft.Users
{
    static class CookieCatcherExtension
    {
        public static IApplicationBuilder UseCookieCatcher(this IApplicationBuilder app, string cookieName)
        {
            return app.Use(async (context, next) =>
            {
                var response = context.Response;
                var request = context.Request;

                response.OnStarting(() =>
                {
                    response.GetTypedHeaders().SetCookie = response.GetTypedHeaders().SetCookie.Select(cookie =>
                    {
                        try // needs fixing
                        {
                            if (cookie.Name == cookieName)
                            {
                                var subdomain = request.Host.Host;
                                var domain = "." + string.Join('.', subdomain.Split('.').TakeLast(2));

                                cookie.Domain = domain;
                            }
                        }
                        catch(Exception)
                        {
                        }
                        return cookie;
                    }).ToArray();

                    return Task.CompletedTask;
                });
                await next();
            });
        }
    }
}
