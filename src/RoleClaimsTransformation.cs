﻿using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using System.Threading;

namespace Conesoft.Users;

public class RoleClaimsTransformation : IClaimsTransformation, IDisposable
{
    private Dictionary<string, LoginData> users = [];
    private readonly CancellationTokenSource cancellationTokenSource = new();

    public RoleClaimsTransformation(UserDirectory userDirectory)
    {
        var watcher = Task.Run(async () =>
        {
            await foreach (var changes in userDirectory.Root.Live(allDirectories: true, cancellationTokenSource.Token))
            {
                var files = await userDirectory.Root.AllFiles.Where(f => f.Name == userDirectory.GetLoginDataDefaultFilename().FilenameWithExtension).ReadFromJson<LoginData>();
                users = files.ToDictionary(f => f.Parent.Name, f => f.Content);
            }
        });
    }

    public void Dispose() => cancellationTokenSource.Cancel();

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