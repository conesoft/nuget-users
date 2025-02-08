using System.Text.Json.Serialization;

namespace Conesoft.Users.Content.Forms;


static class FormExtensions
{
    public static LoginForm GetLoginForm(this HttpContext context) => new(
        context.Request.Form["username"].FirstOrDefault() ?? "",
        context.Request.Form["password"].FirstOrDefault() ?? "",
        context.Request.Form["redirectto"].FirstOrDefault() ?? context.GetLocalReferrer()
    );

    public static PasswordChangeForm? GetPasswordChangeForm(this HttpContext context) =>
        context.User.Identity != null ? new(
            context.User.Identity.Name!,
            context.Request.Form["current-password"].FirstOrDefault() ?? "",
            context.Request.Form["new-password"].FirstOrDefault() ?? "",
            context.Request.Form["redirectto"].FirstOrDefault() ?? context.GetLocalReferrer()
        ) : null;

    public static LogoutForm GetLogoutForm(this HttpContext context) => new(
        context.Request.Form["redirectto"].FirstOrDefault() ?? context.GetLocalReferrer()
    );

    private static string GetLocalReferrer(this HttpContext context)
    {
        var host = context.Request.Headers.TryGetValue("X-Forwarded-Host", out var value)
            ? value.ToString()
            : context.Request.Host.ToString();

        if(context.Request.GetTypedHeaders().Referer is Uri referer && host == referer.Host)
        {
            return referer.PathAndQuery;
        };
        return "";
    }
}