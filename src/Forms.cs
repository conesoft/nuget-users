namespace Conesoft.Users;

static class Forms
{
    public record Login(string Username, string Password, string RedirectTo);
    public record Logout(string RedirectTo);

    public static Login GetLoginForm(this HttpContext context) => new(
        context.Request.Form["username"],
        context.Request.Form["password"],
        context.Request.Form["redirectto"]
    );

    public static Logout GetLogoutForm(this HttpContext context) => new(context.Request.Form["redirectto"]);
}