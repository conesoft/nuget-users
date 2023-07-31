using Microsoft.AspNetCore.Authentication;

namespace Conesoft.Users;

public static class UserExtensions
{
    private static readonly string cadas = CookieAuthenticationDefaults.AuthenticationScheme;


    public static void AddUsers(this IServiceCollection services, string cookiename, string directory)
    {
        LoginData.UserDirectory = Directory.From(directory);

        services.AddAuthentication(cadas).AddCookie(cadas, options =>
        {

            options.Cookie.Name = cookiename;
            options.ExpireTimeSpan = TimeSpan.FromDays(365);
            options.SlidingExpiration = true;
            options.ReturnUrlParameter = "redirectto";
            options.LoginPath = "/user/login";
            options.LogoutPath = "/user/logout";
        });
        services.AddSingleton<IClaimsTransformation>(_ => new RoleClaimsTransformation());
    }

    private static async Task<ClaimsPrincipal?> FindVerifiedAccount(string username, string password, bool createIfNeeded)
    {
        PasswordHasher<string> passwordHasher = new();
        var userpath = LoginData.UserDirectory / username;

        var userfilepath = userpath / LoginData.LoginDataFilename;

        if (userpath.Exists == false)
        {
            if (createIfNeeded)
            {
                var newsalt = Guid.NewGuid().ToString().ToLower().Replace("-", "");

                await userfilepath.WriteAsJson(new LoginData(
                    Salt: newsalt,
                    HashedPassword: passwordHasher.HashPassword(username, password + newsalt),
                    Roles: Array.Empty<string>()
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
                new[] {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, username)
                },
                cadas
            )
        );
    }

    public static void MapUsers(this WebApplication app)
    {
        app.UseAuthentication();

        app.MapPost("/user/login", async (HttpContext context) =>
        {
            var login = context.GetLoginForm();
            var user = await FindVerifiedAccount(login.Username, login.Password, createIfNeeded: false);

            return StackedResults.Stack()
                .PushIfTrue(user != null, () => Results.SignIn(user!, new() { IsPersistent = true }, cadas))
                .Push(Results.LocalRedirect(login.RedirectTo));
        });

        app.MapPost("/user/register", async (HttpContext context) =>
        {
            var login = context.GetLoginForm();
            var user = await FindVerifiedAccount(login.Username, login.Password, createIfNeeded: true);

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

        app.MapGet("/user/{username}.jpg", (string username) =>
        {
            var path = LoginData.UserDirectory / username / LoginData.ProfilePictureFilename;
            
            path = path.Exists ? path : LoginData.UserDirectory / LoginData.ProfilePictureFilename;

            return Results.File(path.Path);
        });
    }
}