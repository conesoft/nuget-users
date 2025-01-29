using Conesoft.Users.Content.Storage;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using System.Threading;

namespace Conesoft.Users.Extensions.Features;

public class RoleClaimsTransformation : IClaimsTransformation, IDisposable
{
    private Dictionary<string, Data> users = [];
    private readonly CancellationTokenSource? cancellationTokenSource;

    public RoleClaimsTransformation(Content.Storage.Directory userDirectory)
    {
        cancellationTokenSource = userDirectory.Root.Live(async () =>
        {
            var files = await userDirectory.Root.AllFiles.Where(f => f.Name == userDirectory.GetLoginDataDefaultFilename().FilenameWithExtension).ReadFromJson<Data>();
            users = files.ToDictionary(f => f.Parent.Name, f => f.Content);
        }, allDirectories: true);
    }

    public void Dispose()
    {
        cancellationTokenSource?.Cancel();
        GC.SuppressFinalize(this);
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.Name != null && users.TryGetValue(principal.Identity?.Name!, out var user))
        {
            var rolesIdentity = new ClaimsIdentity();
            rolesIdentity.AddClaims(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
            principal.AddIdentity(rolesIdentity);
        }
        return Task.FromResult(principal);
    }
}