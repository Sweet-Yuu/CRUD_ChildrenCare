using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using CRUD_ChildrenCare.Services.Menus;
using CRUD_ChildrenCare.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace CRUD_ChildrenCare.Tests.Services;

public sealed class AdminMenuServiceTests
{
    [Fact]
    public async Task GetForRoleAsync_ReturnsOnlyActiveAssignedMenusInDeterministicOrder()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.ExecuteDbContextAsync(async context =>
        {
            var role = Setting(SettingType.UserRole, "Manager", "Manager", SettingStatus.Active);
            var menuB = Setting(SettingType.AdminMenu, "B menu", "/admin/b", SettingStatus.Active);
            var menuA2 = Setting(SettingType.AdminMenu, "A menu", "/admin/a2", SettingStatus.Active);
            var menuA1 = Setting(SettingType.AdminMenu, "A menu duplicate", "/admin/a1", SettingStatus.Active);
            var inactive = Setting(SettingType.AdminMenu, "Hidden", "/admin/hidden", SettingStatus.Inactive);
            context.Settings.AddRange(role, menuB, menuA2, menuA1, inactive);
            await context.SaveChangesAsync();
            context.RoleMenus.AddRange(
                new RoleMenu { RoleId = role.Id, MenuId = menuB.Id },
                new RoleMenu { RoleId = role.Id, MenuId = menuA2.Id },
                new RoleMenu { RoleId = role.Id, MenuId = menuA1.Id },
                new RoleMenu { RoleId = role.Id, MenuId = inactive.Id });
            await context.SaveChangesAsync();
        });
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminMenuService>();

        var menus = await service.GetForRoleAsync("Manager", CancellationToken.None);

        Assert.Equal(["A menu", "A menu duplicate", "B menu"], menus.Select(item => item.Name));
        Assert.DoesNotContain(menus, item => item.Url == "/admin/hidden");
    }

    [Fact]
    public async Task GetForRoleAsync_ReturnsEmptyForInactiveRole()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.ExecuteDbContextAsync(async context =>
        {
            context.Settings.Add(Setting(SettingType.UserRole, "Manager", "Manager", SettingStatus.Inactive));
            await context.SaveChangesAsync();
        });
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminMenuService>();

        var menus = await service.GetForRoleAsync("Manager", CancellationToken.None);

        Assert.Empty(menus);
    }

    private static Setting Setting(SettingType type, string name, string value, SettingStatus status) =>
        new() { Type = type, Name = name, Value = value, Status = status };
}
