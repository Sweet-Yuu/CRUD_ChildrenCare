using CRUD_ChildrenCare.Areas.Admin.ViewModels.Users;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Infrastructure;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using CRUD_ChildrenCare.Services.Email;
using CRUD_ChildrenCare.Services.Security;
using CRUD_ChildrenCare.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemData.RoleValues.Admin)]
public sealed class UsersController(
    ApplicationDbContext dbContext,
    IPasswordGenerator passwordGenerator,
    IPasswordHasher<User> passwordHasher,
    IEmailSender emailSender) : Controller
{
    private const int PageSize = 10;
    private static readonly HashSet<string> AllowedSorts =
        new(["Id", "FullName", "Email", "Mobile", "Gender", "Role", "Status"], StringComparer.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? gender,
        int? roleId,
        string? status,
        string? sort,
        string? direction,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        Gender? selectedGender = Enum.TryParse<Gender>(gender, true, out var parsedGender) ? parsedGender : null;
        UserStatus? selectedStatus = Enum.TryParse<UserStatus>(status, true, out var parsedStatus) ? parsedStatus : null;
        var normalizedSort = AllowedSorts.Contains(sort ?? string.Empty) ? sort! : "Id";
        var normalizedDirection = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var query = dbContext.Users.AsNoTracking().AsQueryable();
        if (normalizedSearch is not null)
        {
            var pattern = $"%{normalizedSearch}%";
            query = query.Where(item =>
                EF.Functions.Like(item.FullName, pattern)
                || EF.Functions.Like(item.Email, pattern)
                || EF.Functions.Like(item.Mobile, pattern));
        }

        if (selectedGender.HasValue)
        {
            query = query.Where(item => item.Gender == selectedGender.Value);
        }

        if (roleId.HasValue)
        {
            query = query.Where(item => item.RoleId == roleId.Value);
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
            .Select(item => new UserListItemViewModel
            {
                Id = item.Id,
                FullName = item.FullName,
                Email = item.Email,
                Mobile = item.Mobile,
                Gender = item.Gender,
                RoleName = item.Role.Name,
                Status = item.Status
            })
            .ToListAsync(cancellationToken);

        return View(new UsersIndexViewModel
        {
            Results = new PagedResult<UserListItemViewModel>
            {
                Items = items,
                Page = normalizedPage,
                PageSize = PageSize,
                TotalCount = totalCount
            },
            Roles = await GetFilterRolesAsync(cancellationToken),
            Search = normalizedSearch,
            Gender = selectedGender,
            RoleId = roleId,
            Status = selectedStatus,
            Sort = normalizedSort,
            Direction = normalizedDirection
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(new CreateUserViewModel { Roles = await GetActiveRolesAsync(cancellationToken) });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model, CancellationToken cancellationToken)
    {
        Normalize(model);
        await ValidateCreateAsync(model, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Roles = await GetActiveRolesAsync(cancellationToken);
            return View(model);
        }

        var temporaryPassword = passwordGenerator.Generate();
        var user = new User
        {
            FullName = model.FullName,
            Gender = model.Gender,
            Email = model.Email,
            NormalizedEmail = AccountValidation.NormalizeEmail(model.Email),
            Mobile = model.Mobile,
            Address = model.Address,
            RoleId = model.RoleId,
            Status = UserStatus.Active
        };
        user.PasswordHash = passwordHasher.HashPassword(user, temporaryPassword);
        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            model.Roles = await GetActiveRolesAsync(cancellationToken);
            return View(model);
        }

        await emailSender.SendAsync(
            new EmailMessage(
                user.Email,
                "Your Children Care account",
                $"Your account has been created. Your temporary password is <strong>{temporaryPassword}</strong>. Change it after signing in."),
            cancellationToken);

        TempData["SuccessMessage"] = "The user account has been created and the temporary password was emailed.";
        return RedirectToAction(nameof(Details), new { id = user.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await dbContext.Users
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new UserDetailsViewModel
            {
                Id = item.Id,
                FullName = item.FullName,
                Gender = item.Gender,
                Email = item.Email,
                Mobile = item.Mobile,
                Address = item.Address,
                AvatarUrl = item.AvatarUrl,
                RoleName = item.Role.Name,
                Status = item.Status,
                CreatedDate = item.CreatedDate,
                UpdatedDate = item.UpdatedDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await dbContext.Users
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new EditUserViewModel
            {
                Id = item.Id,
                FullName = item.FullName,
                Email = item.Email,
                Mobile = item.Mobile,
                Address = item.Address,
                RoleId = item.RoleId,
                Status = item.Status
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        model.Roles = await GetActiveRolesAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditUserViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        PopulateReadOnlyFields(model, user);
        if (!await IsActiveRoleAsync(model.RoleId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.RoleId), "Select an active role.");
        }

        if (!ModelState.IsValid)
        {
            model.Roles = await GetActiveRolesAsync(cancellationToken);
            return View(model);
        }

        user.RoleId = model.RoleId;
        user.Status = model.Status;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "The user account has been updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.Status = user.Status == UserStatus.Active ? UserStatus.Inactive : UserStatus.Active;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = user.Status == UserStatus.Active
            ? "The user account has been reactivated."
            : "The user account has been deactivated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static IQueryable<User> ApplySort(IQueryable<User> query, string sort, bool descending) =>
        (sort.ToUpperInvariant(), descending) switch
        {
            ("FULLNAME", false) => query.OrderBy(item => item.FullName).ThenBy(item => item.Id),
            ("FULLNAME", true) => query.OrderByDescending(item => item.FullName).ThenByDescending(item => item.Id),
            ("EMAIL", false) => query.OrderBy(item => item.Email).ThenBy(item => item.Id),
            ("EMAIL", true) => query.OrderByDescending(item => item.Email).ThenByDescending(item => item.Id),
            ("MOBILE", false) => query.OrderBy(item => item.Mobile).ThenBy(item => item.Id),
            ("MOBILE", true) => query.OrderByDescending(item => item.Mobile).ThenByDescending(item => item.Id),
            ("GENDER", false) => query.OrderBy(item => item.Gender).ThenBy(item => item.Id),
            ("GENDER", true) => query.OrderByDescending(item => item.Gender).ThenByDescending(item => item.Id),
            ("ROLE", false) => query.OrderBy(item => item.Role.Name).ThenBy(item => item.Id),
            ("ROLE", true) => query.OrderByDescending(item => item.Role.Name).ThenByDescending(item => item.Id),
            ("STATUS", false) => query.OrderBy(item => item.Status).ThenBy(item => item.Id),
            ("STATUS", true) => query.OrderByDescending(item => item.Status).ThenByDescending(item => item.Id),
            ("ID", true) => query.OrderByDescending(item => item.Id),
            _ => query.OrderBy(item => item.Id)
        };

    private async Task ValidateCreateAsync(CreateUserViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid && !AccountValidation.IsValidFullName(model.FullName))
        {
            ModelState.AddModelError(nameof(model.FullName), "Full name may contain letters and spaces only.");
        }

        if (AccountValidation.IsValidEmail(model.Email))
        {
            var normalizedEmail = AccountValidation.NormalizeEmail(model.Email);
            if (await dbContext.Users.AnyAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken))
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            }
        }

        if (!await IsActiveRoleAsync(model.RoleId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.RoleId), "Select an active role.");
        }
    }

    private Task<bool> IsActiveRoleAsync(int roleId, CancellationToken cancellationToken) =>
        dbContext.Settings.AnyAsync(
            item => item.Id == roleId
                && item.Type == SettingType.UserRole
                && item.Status == SettingStatus.Active,
            cancellationToken);

    private Task<List<RoleOptionViewModel>> GetActiveRolesAsync(CancellationToken cancellationToken) =>
        dbContext.Settings
            .AsNoTracking()
            .Where(item => item.Type == SettingType.UserRole && item.Status == SettingStatus.Active)
            .OrderBy(item => item.Name)
            .Select(item => new RoleOptionViewModel(item.Id, item.Name))
            .ToListAsync(cancellationToken);

    private Task<List<RoleOptionViewModel>> GetFilterRolesAsync(CancellationToken cancellationToken) =>
        dbContext.Settings
            .AsNoTracking()
            .Where(item => item.Type == SettingType.UserRole)
            .OrderBy(item => item.Name)
            .Select(item => new RoleOptionViewModel(item.Id, item.Name))
            .ToListAsync(cancellationToken);

    private static void Normalize(CreateUserViewModel model)
    {
        model.FullName = AccountValidation.NormalizeFullName(model.FullName ?? string.Empty);
        model.Email = model.Email?.Trim() ?? string.Empty;
        model.Mobile = model.Mobile?.Trim() ?? string.Empty;
        model.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
    }

    private static void PopulateReadOnlyFields(EditUserViewModel model, User user)
    {
        model.FullName = user.FullName;
        model.Email = user.Email;
        model.Mobile = user.Mobile;
        model.Address = user.Address;
    }
}
