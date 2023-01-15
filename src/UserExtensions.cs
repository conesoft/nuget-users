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
        options.ReturnUrlParameter = "returnto";
        options.LoginPath = "/user/login";
        options.LogoutPath = "/user/logout";
    });

    private static bool VerifyPersistedAccount(string user, string password)
    {
        PasswordHasher<string> passwordHasher = new();
        if (File.Exists(Path.Combine(directory, user + ".txt")))
        {
            var lines = File.ReadAllLines(Path.Combine(directory, user + ".txt"));
            var salt = lines.First();
            var hashed = lines.Last();
            return passwordHasher.VerifyHashedPassword(user, hashed, password + salt) == PasswordVerificationResult.Success;
        }
        else
        {
            var salt = Guid.NewGuid().ToString().ToLower().Replace("-", "");
            File.WriteAllLines(Path.Combine(directory, user + ".txt"), new[] { salt, passwordHasher.HashPassword(user, password + salt) });
            return true;
        }
    }

    public static void MapUsers(this WebApplication app)
    {
        app.UseAuthentication();

        app.MapPost("/user/login", (HttpContext context) =>
        {
            var login = context.GetLoginForm();
            return VerifyPersistedAccount(login.Username, login.Password)
                ? SignInAndRedirect(Principal.From(login.Username), new() { IsPersistent = true }, cadas, login.RedirectTo)
                : Results.Unauthorized();
        });

        app.MapPost("/user/logout", (HttpContext context) => {
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