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

        // using get, which is bad, but pretty code
        app.MapGet("/user/login", (string user, string password) =>
            VerifyPersistedAccount(user, password)
            ? Results.SignIn(Principal.From(user), new() { IsPersistent = true }, cadas)
            : Results.Unauthorized());

        app.MapGet("/user/logout", () => Results.SignOut());

        // using post, which would be 'right' (example code)
        app.MapPost("/user/login", (LoginForm form) =>
            VerifyPersistedAccount(form.User, form.Password)
            ? SignInAndRedirect(Principal.From(form.User), new() { IsPersistent = true }, cadas, form.ReturnTo)
            : Results.Unauthorized());

        app.MapPost("/user/logout", (LogoutForm form) => SignOutAndRedirect(form.ReturnTo));
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

    public record LoginForm(string User, string Password, string ReturnTo)
    {
        public static ValueTask<LoginForm?> BindAsync(HttpContext context, ParameterInfo _) => ValueTask.FromResult<LoginForm?>(new(
            context.Request.Form["user"],
            context.Request.Form["password"],
            context.Request.Form["returnto"]
        ));
    }

    public record LogoutForm(string ReturnTo)
    {
        public static ValueTask<LogoutForm?> BindAsync(HttpContext context, ParameterInfo _) => ValueTask.FromResult<LogoutForm?>(new(context.Request.Form["returnto"]));
    }
}