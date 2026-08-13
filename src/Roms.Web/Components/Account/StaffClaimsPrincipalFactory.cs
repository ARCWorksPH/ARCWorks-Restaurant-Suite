using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Roms.Infrastructure.Identity;

namespace Roms.Web.Components.Account;

internal sealed class StaffClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    internal const string MustChangePasswordClaimType = "arcworks:must_change_password";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (!string.IsNullOrWhiteSpace(user.ActiveSessionId))
        {
            identity.AddClaim(new Claim(StaffSessionService.SessionClaimType, user.ActiveSessionId));
        }

        if (user.MustChangePassword)
        {
            identity.AddClaim(new Claim(MustChangePasswordClaimType, bool.TrueString));
        }

        return identity;
    }
}
