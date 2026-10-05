using CRUD_ChildrenCare.Areas.Admin.ViewModels.Authorization;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemData.RoleValues.Admin)]
public sealed class AuthorizationController(ApplicationDbContext dbContext) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? roleId, CancellationToken cancellationToken)
    {
        var roles = await GetActiveOptionsAsync(SettingType.UserRole, cancellationToken);
        var selectedRoleId = roleId ?? roles.FirstOrDefault()?.Id ?? 0;
        if (selectedRoleId != 0 && roles.All(item => item.Id != selectedRoleId))
        {
            return NotFound();
        }

        var selectedMenuIds = selectedRoleId == 0
            ? []
            : await dbContext.RoleMenus
                .AsNoTracking()
                .Where(item => item.RoleId == selectedRoleId)
                .Select(item => item.MenuId)
                .ToListAsync(cancellationToken);

        return View(new RoleMenuAssignmentViewModel
        {
            RoleId = selectedRoleId,
            SelectedMenuIds = selectedMenuIds,
            Roles = roles,
            Menus = await GetActiveOptionsAsync(SettingType.AdminMenu, cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RoleMenuAssignmentViewModel model, CancellationToken cancellationToken)
    {
        var roles = await GetActiveOptionsAsync(SettingType.UserRole, cancellationToken);
        var menus = await GetActiveOptionsAsync(SettingType.AdminMenu, cancellationToken);
        model.Roles = roles;
        model.Menus = menus;
        model.SelectedMenuIds = model.SelectedMenuIds.Distinct().ToList();

        if (roles.All(item => item.Id != model.RoleId))
        {
            ModelState.AddModelError(nameof(model.RoleId), "Select an active role.");
        }

        var activeMenuIds = menus.Select(item => item.Id).ToHashSet();
        if (model.SelectedMenuIds.Any(id => !activeMenuIds.Contains(id)))
        {
            ModelState.AddModelError(nameof(model.SelectedMenuIds), "Select only active administration menus.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var existing = await dbContext.RoleMenus
            .Where(item => item.RoleId == model.RoleId)
            .ToListAsync(cancellationToken);
        var selected = model.SelectedMenuIds.ToHashSet();
        dbContext.RoleMenus.RemoveRange(existing.Where(item => !selected.Contains(item.MenuId)));
        var existingIds = existing.Select(item => item.MenuId).ToHashSet();
        foreach (var menuId in selected.Except(existingIds))
        {
            dbContext.RoleMenus.Add(new RoleMenu { RoleId = model.RoleId, MenuId = menuId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        TempData["SuccessMessage"] = "Menu authorization has been updated.";
        return RedirectToAction(nameof(Index), new { roleId = model.RoleId });
    }

    private Task<List<AuthorizationOptionViewModel>> GetActiveOptionsAsync(
        SettingType type,
        CancellationToken cancellationToken) =>
        dbContext.Settings
            .AsNoTracking()
            .Where(item => item.Type == type && item.Status == SettingStatus.Active)
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Select(item => new AuthorizationOptionViewModel(item.Id, item.Name))
            .ToListAsync(cancellationToken);
}
