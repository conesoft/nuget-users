using Microsoft.Extensions.Options;

namespace Conesoft.Users;

public class UserDirectory(IOptions<UserOptions> userOptions)
{
    readonly Filename profilePictureDefaultFilename = Filename.FromExtended("profile-picture.jpg");
    readonly Filename loginDataDefaultFilename = Filename.FromExtended("login-data.json");

    public Directory Root => Directory.From(userOptions.Value.Directory);

    public Directory GetUserPathFor(string username) => Root / username;
    public File GetLoginDataFor(string username) => Root / username / loginDataDefaultFilename;
    public File GetProfilePictureFor(string username) => Root / username / profilePictureDefaultFilename;
    public File GetDefaultProfilePicture() => Root / profilePictureDefaultFilename;
    public Filename GetLoginDataDefaultFilename() => loginDataDefaultFilename;
}
