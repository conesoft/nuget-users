using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Conesoft.Users
{
    [Route("[controller]")]
    public class UserController : Controller
    {
        internal static string RootPath { get; set; }

        string UserFile(string username) => RootPath != "" ? System.IO.Path.Combine(RootPath, username + ".txt") : username + ".txt";

        [HttpPost("login")]
        public async Task<IActionResult> PostLoginAsync(string username, string password, string redirectto)
        {
            var passwordHasher = new PasswordHasher<string>();

            if (System.IO.File.Exists(UserFile(username)))
            {
                var lines = await System.IO.File.ReadAllLinesAsync(UserFile(username));

                var salt = lines.First();
                var hashed = lines.Last();

                var validLogin = passwordHasher.VerifyHashedPassword(username, hashed, password + salt);

                if (validLogin == PasswordVerificationResult.Success)
                {
                    // Create the identity from the user info
                    var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, username));
                    identity.AddClaim(new Claim(ClaimTypes.Name, username));

                    // Authenticate using the identity
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = true });

                }
            }

            return Redirect(redirectto);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> PostLogoutAsync(string redirectto)
        {
            await HttpContext.SignOutAsync();
            return Redirect(redirectto);
        }

        [HttpPost("register")]
        public async Task<IActionResult> PostRegisterAsync(string username, string password, string redirectto)
        {
            var passwordHasher = new PasswordHasher<string>();

            if (System.IO.File.Exists(UserFile(username)) == false)
            {
                var newsalt = Guid.NewGuid().ToString().ToLower().Replace("-", "");
                await System.IO.File.WriteAllLinesAsync(UserFile(username), new[] {
                    newsalt,
                    passwordHasher.HashPassword(username, password + newsalt)
                });
            }

            // Create the identity from the user info
            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, username));
            identity.AddClaim(new Claim(ClaimTypes.Name, username));

            // Authenticate using the identity
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = true });

            return Redirect(redirectto);
        }
    }
}
