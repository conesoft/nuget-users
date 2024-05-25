using System.Text.Json.Serialization;

namespace Conesoft.Users;

record LoginData(
    [property: JsonPropertyName("salt")] string Salt,
    [property: JsonPropertyName("hashed-password")] string HashedPassword,
    [property: JsonPropertyName("roles")] string[] Roles
)
{
    public static Directory UserDirectory = Directory.Invalid;
    public static Filename LoginDataFilename => Filename.FromExtended("login-data.json");
    public static Filename ProfilePictureFilename => Filename.FromExtended("profile-picture.jpg");
    public static LoginData Empty => new("", "", []);
};