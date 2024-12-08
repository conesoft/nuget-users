using System.Text.Json.Serialization;

namespace Conesoft.Users;

record LoginData(
    [property: JsonPropertyName("salt")] string Salt,
    [property: JsonPropertyName("hashed-password")] string HashedPassword,
    [property: JsonPropertyName("roles")] string[] Roles
)
{
    public static LoginData Empty => new("", "", []);
};