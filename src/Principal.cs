using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

static class Principal
{
    public static ClaimsPrincipal From(params Claim[] claims) => new(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    public static ClaimsPrincipal From(string name) => From(new(ClaimTypes.NameIdentifier, name), new(ClaimTypes.Name, name));
}