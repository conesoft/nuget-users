namespace Conesoft.Users.Content.Forms;


static class FormExtensions
{
    public static LoginForm GetLoginForm(this HttpContext context) => new(
        context.Request.Form["username"].FirstOrDefault() ?? "",
        context.Request.Form["password"].FirstOrDefault() ?? "",
        context.Request.Form["redirectto"].FirstOrDefault() ?? ""
    );

    public static PasswordChangeForm? GetPasswordChangeForm(this HttpContext context) =>
        context.User.Identity != null ? new(
            context.User.Identity.Name!,
            context.Request.Form["current-password"].FirstOrDefault() ?? "",
            context.Request.Form["new-password"].FirstOrDefault() ?? "",
            context.Request.Form["redirectto"].FirstOrDefault() ?? ""
        ) : null;

    public static LogoutForm GetLogoutForm(this HttpContext context) => new(
        context.Request.Form["redirectto"].FirstOrDefault() ?? ""
    );
}