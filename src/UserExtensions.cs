using System.Text.Json;
using System.Text.Json.Serialization;

namespace Conesoft.Users;

public static class UserExtensions
{
    private static string directory = ".";
    private static readonly string cadas = CookieAuthenticationDefaults.AuthenticationScheme;

    private static readonly string logindatafile = "login-data.json";

    public static void AddUsers(this IServiceCollection services, string cookiename, string directory) => services.AddAuthentication(cadas).AddCookie(cadas, options =>
    {
        UserExtensions.directory = directory;

        options.Cookie.Name = cookiename;
        options.ExpireTimeSpan = TimeSpan.FromDays(365);
        options.SlidingExpiration = true;
        options.ReturnUrlParameter = "redirectto";
        options.LoginPath = "/user/login";
        options.LogoutPath = "/user/logout";
    });

    record LoginDataFile(
        [property: JsonPropertyName("salt")] string Salt,
        [property: JsonPropertyName("hashed-password")] string HashedPassword,
        [property: JsonPropertyName("roles")] string[] Roles
    );

    private static async Task<ClaimsPrincipal?> FindVerifiedAccount(string username, string password, bool createIfNeeded)
    {
        PasswordHasher<string> passwordHasher = new();
        var userpath = Path.Combine(directory, username);

        var userfilepath = Path.Combine(userpath, logindatafile);

        if (Directory.Exists(userpath) == false)
        {
            if (createIfNeeded)
            {
                var newsalt = Guid.NewGuid().ToString().ToLower().Replace("-", "");

                Directory.CreateDirectory(userpath);

                await File.WriteAllTextAsync(userfilepath, JsonSerializer.Serialize(new LoginDataFile(
                    Salt: newsalt,
                    HashedPassword: passwordHasher.HashPassword(username, password + newsalt),
                    Roles: Array.Empty<string>()
                ), new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            else
            {
                return null;
            }
        }

        var logindata = JsonSerializer.Deserialize<LoginDataFile>(await File.ReadAllTextAsync(userfilepath));
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
                logindata.Roles
                    .Select(r => new Claim(ClaimTypes.Role, r))
                    .Append(new Claim(ClaimTypes.Name, username))
                    .Append(new Claim(ClaimTypes.NameIdentifier, username)),
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
    }
}