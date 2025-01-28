using Conesoft.Users.Content.Forms;
using Conesoft.Users.Content.Storage;
using Conesoft.Users.Helpers;

namespace Conesoft.Users.Extensions.Features;

public static class ApiMapping
{
    private static async Task<ClaimsPrincipal?> FindVerifiedAccount(string username, string password, Content.Storage.Directory userDirectory, bool createIfNeeded)
    {
        PasswordHasher<string> passwordHasher = new();
        var userpath = userDirectory.GetUserPathFor(username);

        var userfilepath = userDirectory.GetLoginDataFor(username);

        if (userpath.Exists == false)
        {
            if (createIfNeeded)
            {
                var newsalt = Guid.NewGuid().ToString().ToLower().Replace("-", "");

                await userfilepath.WriteAsJson(new Data(
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

        var logindata = await userfilepath.ReadFromJson<Data>();
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
                CookieScheme.Cadas
            )
        );
    }

    static public WebApplication MapApi(this WebApplication app)
    {
        app.MapPost("/user/login", async (HttpContext context, Content.Storage.Directory userDirectory) =>
        {
            var login = context.GetLoginForm();
            var user = await FindVerifiedAccount(login.Username, login.Password, userDirectory, createIfNeeded: false);

            return StackedResults.Stack()
                .PushIfTrue(user != null, () => Results.SignIn(user!, new() { IsPersistent = true }, CookieScheme.Cadas))
                .Push(Results.LocalRedirect(login.RedirectTo));
        });

        app.MapPost("/user/register", async (HttpContext context, Content.Storage.Directory userDirectory) =>
        {
            var login = context.GetLoginForm();
            var user = await FindVerifiedAccount(login.Username, login.Password, userDirectory, createIfNeeded: true);

            return StackedResults.Stack()
                .PushIfTrue(user != null, () => Results.SignIn(user!, new() { IsPersistent = true }, CookieScheme.Cadas))
                .Push(Results.LocalRedirect(login.RedirectTo));
        });

        app.MapPost("/user/logout", (HttpContext context) =>
        {
            var logout = context.GetLogoutForm();

            return StackedResults.Stack()
                .Push(Results.SignOut())
                .Push(Results.LocalRedirect(logout.RedirectTo));
        });

        app.MapPost("/user/update/password", async (HttpContext context, Content.Storage.Directory userDirectory) =>
        {
            PasswordHasher<string> passwordHasher = new();
            try
            {
                var change = context.GetPasswordChangeForm() ?? throw new Exception("form data is wrong");

                var storage = userDirectory.GetLoginDataFor(change.Username);

                var user = await storage.ReadFromJson<Data>() ?? throw new Exception("username is wrong");

                if (passwordHasher.VerifyHashedPassword(change.Username, user.HashedPassword, change.CurrentPassword + user.Salt) != PasswordVerificationResult.Success)
                {
                    throw new Exception("current password is wrong");
                }

                await storage.WriteAsJson(user with
                {
                    HashedPassword = passwordHasher.HashPassword(change.Username, change.NewPassword + user.Salt)
                });

                return Results.LocalRedirect(change.RedirectTo);
            }
            catch (Exception ex)
            {
                return Results.UnprocessableEntity(ex);
            }
        });

        app.MapGet("/user/{username}.jpg", (string username, Content.Storage.Directory userDirectory) =>
        {
            var path = userDirectory.GetProfilePictureFor(username);
            path = path.Exists ? path : userDirectory.GetDefaultProfilePicture();

            return Results.File(path.Path, contentType: "image/jpeg");
        });

        app.MapPost("/user/update/profile-picture", async (HttpContext context, IFormFile file, Content.Storage.Directory userDirectory) =>
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

        return app;
    }
}
