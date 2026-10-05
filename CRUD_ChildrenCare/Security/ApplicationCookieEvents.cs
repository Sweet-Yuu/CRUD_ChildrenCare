using System.Security.Claims;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Security;

public sealed class ApplicationCookieEvents(ApplicationDbContext dbContext) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userIdValue = context.Principal?.FindFirstValue(AppClaimTypes.UserId);
        if (!int.TryParse(userIdValue, out var userId))
        {
            context.RejectPrincipal();
            return;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(item => item.Role)
            .SingleOrDefaultAsync(item => item.Id == userId, context.HttpContext.RequestAborted);
        if (user is null
            || user.Status != UserStatus.Active
            || user.Role.Type != SettingType.UserRole
            || user.Role.Status != SettingStatus.Active)
        {
            context.RejectPrincipal();
            return;
        }

        if (ClaimsAreCurrent(context.Principal!, user))
        {
            return;
        }

        context.ReplacePrincipal(CreatePrincipal(user));
        context.ShouldRenew = true;
    }

    public static ClaimsPrincipal CreatePrincipal(User user)
    {
        var claims = new[]
        {
            new Claim(AppClaimTypes.UserId, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Value)
        };
        return new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    private static bool ClaimsAreCurrent(ClaimsPrincipal principal, User user) =>
        string.Equals(principal.FindFirstValue(ClaimTypes.Name), user.FullName, StringComparison.Ordinal)
        && string.Equals(principal.FindFirstValue(ClaimTypes.Email), user.Email, StringComparison.OrdinalIgnoreCase)
        && string.Equals(principal.FindFirstValue(ClaimTypes.Role), user.Role.Value, StringComparison.Ordinal);
}
