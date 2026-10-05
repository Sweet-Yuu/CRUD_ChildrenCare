using System.Security.Claims;
using CRUD_ChildrenCare.Services.Menus;
using Microsoft.AspNetCore.Mvc;

namespace CRUD_ChildrenCare.ViewComponents;

public sealed class AdminMenuViewComponent(IAdminMenuService menuService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var roleValue = UserClaimsPrincipal.FindFirstValue(ClaimTypes.Role);
        var menus = string.IsNullOrWhiteSpace(roleValue)
            ? []
            : await menuService.GetForRoleAsync(roleValue, HttpContext.RequestAborted);
        return View(menus);
    }
}
