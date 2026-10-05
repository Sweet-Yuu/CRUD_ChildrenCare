using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Services.Menus;

public sealed class AdminMenuService(ApplicationDbContext dbContext) : IAdminMenuService
{
    public async Task<IReadOnlyList<AdminMenuItem>> GetForRoleAsync(
        string roleValue,
        CancellationToken cancellationToken) =>
        await dbContext.RoleMenus
            .AsNoTracking()
            .Where(item =>
                item.Role.Type == SettingType.UserRole
                && item.Role.Status == SettingStatus.Active
                && item.Role.Value == roleValue
                && item.Menu.Type == SettingType.AdminMenu
                && item.Menu.Status == SettingStatus.Active)
            .OrderBy(item => item.Menu.Name)
            .ThenBy(item => item.MenuId)
            .Select(item => new AdminMenuItem(item.MenuId, item.Menu.Name, item.Menu.Value))
            .ToListAsync(cancellationToken);
}
