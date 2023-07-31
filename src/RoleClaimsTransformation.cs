﻿using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;

namespace Conesoft.Users;

public class RoleClaimsTransformation : IClaimsTransformation
{
    private Dictionary<string, LoginData> users = new();

    public RoleClaimsTransformation()
    {
        var watcher = Task.Run(async () =>
        {
            await foreach (var changes in LoginData.UserDirectory.Live(allDirectories: true).Changes())
            {
                if (changes.ThereAreChanges)
                {
                    var files = await changes.All.ReadFromJson<LoginData>();

                    users = files.Where(f => f.Content != null).ToDictionary(f => f.Parent.Name, f => f.Content!);
                }
            }
        });
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