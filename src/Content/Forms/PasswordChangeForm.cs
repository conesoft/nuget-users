namespace Conesoft.Users.Content.Forms;

public record PasswordChangeForm(string Username, string CurrentPassword, string NewPassword, string RedirectTo);
