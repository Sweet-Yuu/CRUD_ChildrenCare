using CRUD_ChildrenCare.Areas.Admin.ViewModels.Settings;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Infrastructure;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemData.RoleValues.Admin)]
public sealed class SettingsController(ApplicationDbContext dbContext) : Controller
{
    private const int PageSize = 10;
    private static readonly HashSet<string> AllowedSorts =
        new(["Id", "Type", "Name", "Value", "Status"], StringComparer.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? type,
        string? status,
        string? sort,
        string? direction,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        SettingType? selectedType = Enum.TryParse<SettingType>(type, true, out var parsedType) ? parsedType : null;
        SettingStatus? selectedStatus = Enum.TryParse<SettingStatus>(status, true, out var parsedStatus) ? parsedStatus : null;
        var normalizedSort = AllowedSorts.Contains(sort ?? string.Empty) ? sort! : "Id";
        var normalizedDirection = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var query = dbContext.Settings.AsNoTracking();
        if (normalizedSearch is not null)
        {
            var pattern = $"%{normalizedSearch}%";
            query = query.Where(item =>
                EF.Functions.Like(item.Name, pattern)
                || EF.Functions.Like(item.Value, pattern)
                || (item.Description != null && EF.Functions.Like(item.Description, pattern)));
        }

        if (selectedType.HasValue)
        {
            query = query.Where(item => item.Type == selectedType.Value);
        }

        if (selectedStatus.HasValue)
        {
            query = query.Where(item => item.Status == selectedStatus.Value);
        }

        query = ApplySort(query, normalizedSort, normalizedDirection == "desc");
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)PageSize);
        var normalizedPage = Math.Clamp(page, 1, totalPages);
        var items = await query
            .Skip((normalizedPage - 1) * PageSize)
            .Take(PageSize)
            .Select(item => new SettingListItemViewModel
            {
                Id = item.Id,
                Type = item.Type,
                Name = item.Name,
                Value = item.Value,
                Status = item.Status
            })
            .ToListAsync(cancellationToken);

        return View(new SettingsIndexViewModel
        {
            Results = new PagedResult<SettingListItemViewModel>
            {
                Items = items,
                Page = normalizedPage,
                PageSize = PageSize,
                TotalCount = totalCount
            },
            Search = normalizedSearch,
            Type = selectedType,
            Status = selectedStatus,
            Sort = normalizedSort,
            Direction = normalizedDirection
        });
    }

    [HttpGet]
    public IActionResult Create() => View(new SettingFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SettingFormViewModel model, CancellationToken cancellationToken)
    {
        Normalize(model);
        await ValidateDuplicateAsync(model, null, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        dbContext.Settings.Add(new Setting
        {
            Type = model.Type,
            Name = model.Name,
            Value = model.Value,
            Description = model.Description,
            Status = model.Status
        });

        if (!await TrySaveAsync(cancellationToken))
        {
            return View(model);
        }

        TempData["SuccessMessage"] = "The setting has been created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await dbContext.Settings
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new SettingDetailsViewModel
            {
                Id = item.Id,
                Type = item.Type,
                Name = item.Name,
                Value = item.Value,
                Description = item.Description,
                Status = item.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var setting = await dbContext.Settings.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (setting is null)
        {
            return NotFound();
        }

        return View(ToFormModel(setting));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SettingFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var setting = await dbContext.Settings.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (setting is null)
        {
            return NotFound();
        }

        var isSystemRole = IsSystemRole(setting);
        var immutableRoleValue = setting.Value;
        Normalize(model);
        if (isSystemRole)
        {
            model.Value = immutableRoleValue;
        }

        model.IsSystemRole = isSystemRole;
        await ValidateDuplicateAsync(model, id, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        setting.Type = model.Type;
        setting.Name = model.Name;
        setting.Value = model.Value;
        setting.Description = model.Description;
        setting.Status = model.Status;

        if (!await TrySaveAsync(cancellationToken))
        {
            return View(model);
        }

        TempData["SuccessMessage"] = "The setting has been updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken cancellationToken)
    {
        var setting = await dbContext.Settings.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (setting is null)
        {
            return NotFound();
        }

        setting.Status = setting.Status == SettingStatus.Active
            ? SettingStatus.Inactive
            : SettingStatus.Active;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = setting.Status == SettingStatus.Active
            ? "The setting has been reactivated."
            : "The setting has been deactivated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static IQueryable<Setting> ApplySort(IQueryable<Setting> query, string sort, bool descending) =>
        (sort.ToUpperInvariant(), descending) switch
        {
            ("TYPE", false) => query.OrderBy(item => item.Type).ThenBy(item => item.Id),
            ("TYPE", true) => query.OrderByDescending(item => item.Type).ThenByDescending(item => item.Id),
            ("NAME", false) => query.OrderBy(item => item.Name).ThenBy(item => item.Id),
            ("NAME", true) => query.OrderByDescending(item => item.Name).ThenByDescending(item => item.Id),
            ("VALUE", false) => query.OrderBy(item => item.Value).ThenBy(item => item.Id),
            ("VALUE", true) => query.OrderByDescending(item => item.Value).ThenByDescending(item => item.Id),
            ("STATUS", false) => query.OrderBy(item => item.Status).ThenBy(item => item.Id),
            ("STATUS", true) => query.OrderByDescending(item => item.Status).ThenByDescending(item => item.Id),
            ("ID", true) => query.OrderByDescending(item => item.Id),
            _ => query.OrderBy(item => item.Id)
        };

    private async Task ValidateDuplicateAsync(SettingFormViewModel model, int? currentId, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(model.Type) || string.IsNullOrWhiteSpace(model.Name))
        {
            return;
        }

        if (await dbContext.Settings.AnyAsync(
                item => item.Id != currentId && item.Type == model.Type && item.Name == model.Name,
                cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "A setting with this type and name already exists.");
        }
    }

    private async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(SettingFormViewModel.Name), "A setting with this type and name already exists.");
            return false;
        }
    }

    private static void Normalize(SettingFormViewModel model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Value = model.Value?.Trim() ?? string.Empty;
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
    }

    private static bool IsSystemRole(Setting setting) =>
        setting.Type == SettingType.UserRole && SystemData.RoleValues.All.Contains(setting.Value);

    private static SettingFormViewModel ToFormModel(Setting setting) => new()
    {
        Id = setting.Id,
        Type = setting.Type,
        Name = setting.Name,
        Value = setting.Value,
        Description = setting.Description,
        Status = setting.Status,
        IsSystemRole = IsSystemRole(setting)
    };
}
