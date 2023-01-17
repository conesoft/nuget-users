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

        return new ClaimsPrincipal(
            new ClaimsIdentity(
                roles
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