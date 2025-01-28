using System.Text.Json.Serialization;

namespace Conesoft.Users.Content.Storage;

record Data(
    [property: JsonPropertyName("salt")] string Salt,
    [property: JsonPropertyName("hashed-password")] string HashedPassword,
    [property: JsonPropertyName("roles")] string[] Roles
)
{
    public static Data Empty => new("", "", []);
};