using static Conesoft.Users.UserExtensions;

namespace Conesoft.Users;
public static class UserExtensions
{
    private static string directory = ".";
    private static readonly string cadas = CookieAuthenticationDefaults.AuthenticationScheme;
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

    private static async Task<ClaimsPrincipal?> FindVerifiedAccount(string username, string password, bool createIfNeeded)
    {
        PasswordHasher<string> passwordHasher = new();
        var userfilepath = Path.Combine(directory, username + ".txt");

        if (File.Exists(userfilepath) == false)
        {
            if (createIfNeeded)
            {
                var newsalt = Guid.NewGuid().ToString().ToLower().Replace("-", "");
                await File.WriteAllLinesAsync(userfilepath, new[] { newsalt, passwordHasher.HashPassword(username, password + newsalt) });
            }
            else
            {
                return null;
            }
        }

        var lines = await File.ReadAllLinesAsync(userfilepath);
        var salt = lines.First();
        var hashed = lines.Skip(1).First();
        var roles = lines.Skip(2).ToArray();

        if (passwordHasher.VerifyHashedPassword(username, hashed, password + salt) != PasswordVerificationResult.Success)
        {
            return null;
        }

        return Principal.From(roles
            .Select(r => new Claim(ClaimTypes.Role, r))
            .Append(new Claim(ClaimTypes.Name, username))
            .ToArray()
        );
    }

    public static void MapUsers(this WebApplication app)
    {
        app.UseAuthentication();

        app.MapPost("/user/login", async (HttpContext context) =>
        {
            var login = context.GetLoginForm();
            return await FindVerifiedAccount(login.Username, login.Password, createIfNeeded: false) switch
            {
                var user when user is not null => SignInAndRedirect(user, new() { IsPersistent = true }, cadas, login.RedirectTo),
                _ => Results.Unauthorized()
            };
        });

        app.MapPost("/user/register", async (HttpContext context) =>
        {
            var login = context.GetLoginForm();
            return await FindVerifiedAccount(login.Username, login.Password, createIfNeeded: true) switch
            {
                var user when user is not null => SignInAndRedirect(user, new() { IsPersistent = true }, cadas, login.RedirectTo),
                _ => Results.Unauthorized()
            };
        });

        app.MapPost("/user/logout", (HttpContext context) =>
        {
            var logout = context.GetLogoutForm();
            return SignOutAndRedirect(logout.RedirectTo);
        });
    }

    private static IResult SignInAndRedirect(ClaimsPrincipal claimsPrincipal, AuthenticationProperties? properties, string authenticationScheme, string url) => StackedResults.Stack(
        Results.SignIn(claimsPrincipal, properties, authenticationScheme),
        Results.Redirect(url)
    );

    private static IResult SignOutAndRedirect(string url) => StackedResults.Stack(
        Results.SignOut(),
        Results.LocalRedirect(url)
    );

    record StackedResults(IResult[] Results) : IResult
    {
        public static IResult Stack(params IResult[] results) => new StackedResults(results);

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            foreach (var result in Results)
            {
                await result.ExecuteAsync(httpContext);
            }
        }
    }

    public record LoginForm(string Username, string Password, string RedirectTo);
    public record LogoutForm(string RedirectTo);
}

static class Extensions
{
    public static LoginForm GetLoginForm(this HttpContext context) => new(
        context.Request.Form["username"],
        context.Request.Form["password"],
        context.Request.Form["redirectto"]
    );

    public static LogoutForm GetLogoutForm(this HttpContext context) => new(context.Request.Form["redirectto"]);
}