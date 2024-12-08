using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Conesoft.Users;

public static class UserExtensions
{
    private static readonly string cadas = CookieAuthenticationDefaults.AuthenticationScheme;

    public static WebApplicationBuilder AddUsers<Dependency>(this WebApplicationBuilder webApplication, Action<UserOptions, Dependency> configuration) where Dependency : class
    {
        var services = webApplication.Services;

        services.AddOptions<UserOptions>().Configure(configuration);
        services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureCookieAuthenticationOptions>();
        services.AddAuthentication(cadas).AddCookie(cadas);
        services.AddSingleton<IClaimsTransformation, RoleClaimsTransformation>();
        services.AddTransient<UserDirectory>();

        return webApplication;
    }

    [Obsolete("use builder.AddUsers(options => { ... }) instead")]
    public static void AddUsers(this IServiceCollection services, string cookiename, string directory)
    {
        services.AddAuthentication(cadas).AddCookie(cadas, options =>
        {
            options.Cookie.Name = cookiename;
            options.ExpireTimeSpan = TimeSpan.FromDays(365);
            options.SlidingExpiration = true;
            options.ReturnUrlParameter = "redirectto";
            options.LoginPath = "/user/login";
            options.LogoutPath = "/user/logout";
        });
        services.AddSingleton<IClaimsTransformation, RoleClaimsTransformation>();
    }

    private static async Task<ClaimsPrincipal?> FindVerifiedAccount(string username, string password, UserDirectory userDirectory, bool createIfNeeded)
    {
        PasswordHasher<string> passwordHasher = new();
        var userpath = userDirectory.GetUserPathFor(username);

        var userfilepath = userDirectory.GetLoginDataFor(username);

        if (userpath.Exists == false)
        {
            if (createIfNeeded)
            {
                var newsalt = Guid.NewGuid().ToString().ToLower().Replace("-", "");

                await userfilepath.WriteAsJson(new LoginData(
                    Salt: newsalt,
                    HashedPassword: passwordHasher.HashPassword(username, password + newsalt),
                    Roles: []
                ));
            }
            else
            {
                return null;
            }
        }

        var logindata = await userfilepath.ReadFromJson<LoginData>();
        if (logindata == null)
        {
            return null;
        }

        if (passwordHasher.VerifyHashedPassword(username, logindata.HashedPassword, password + logindata.Salt) != PasswordVerificationResult.Success)
        {
            return null;
        }

        return new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, username)
                ],
                cadas
            )
        );
    }

    public static void MapUsers(this WebApplication app)
    {
        app.UseAuthentication();

        app.MapPost("/user/login", async (HttpContext context, UserDirectory userDirectory) =>
        {
            var login = context.GetLoginForm();
            var user = await FindVerifiedAccount(login.Username, login.Password, userDirectory, createIfNeeded: false);

            return StackedResults.Stack()
                .PushIfTrue(user != null, () => Results.SignIn(user!, new() { IsPersistent = true }, cadas))
                .Push(Results.LocalRedirect(login.RedirectTo));
        });

        app.MapPost("/user/register", async (HttpContext context, UserDirectory userDirectory) =>
        {
            var login = context.GetLoginForm();
            var user = await FindVerifiedAccount(login.Username, login.Password, userDirectory, createIfNeeded: true);

            return StackedResults.Stack()
                .PushIfTrue(user != null, () => Results.SignIn(user!, new() { IsPersistent = true }, cadas))
                .Push(Results.LocalRedirect(login.RedirectTo));
        });

        app.MapPost("/user/logout", (HttpContext context) =>
        {
            var logout = context.GetLogoutForm();

            return StackedResults.Stack()
                .Push(Results.SignOut())
                .Push(Results.LocalRedirect(logout.RedirectTo));
        });

        app.MapGet("/user/{username}.jpg", (string username, UserDirectory userDirectory) =>
        {
            var path = userDirectory.GetProfilePictureFor(username);
            path = path.Exists ? path : userDirectory.GetDefaultProfilePicture();

            return Results.File(path.Path, contentType: "image/jpeg");
        });

        app.MapPost("/user/update-profile-picture", async (HttpContext context, IFormFile file, UserDirectory userDirectory) =>
        {
            var username = context.User?.Identity?.Name;
            var logout = context.GetLogoutForm();

            if (username != null)
            {
                var path = userDirectory.GetProfilePictureFor(username);

                using var stream = path.OpenWrite();
                await file.CopyToAsync(stream);
            }

            return Results.LocalRedirect(logout.RedirectTo);
        });
    }
}