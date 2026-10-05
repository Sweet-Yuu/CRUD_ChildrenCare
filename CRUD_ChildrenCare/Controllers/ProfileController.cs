using System.Security.Claims;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Security;
using CRUD_ChildrenCare.Services.Files;
using CRUD_ChildrenCare.Validation;
using CRUD_ChildrenCare.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Controllers;

[Authorize]
public sealed class ProfileController(
    ApplicationDbContext dbContext,
    IAvatarStorage avatarStorage,
    ILogger<ProfileController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Challenge();
        }

        var profile = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new ProfileViewModel
            {
                FullName = user.FullName,
                Gender = user.Gender,
                Email = user.Email,
                Mobile = user.Mobile,
                Address = user.Address,
                AvatarUrl = user.AvatarUrl,
                RoleName = user.Role.Name
            })
            .SingleOrDefaultAsync(cancellationToken);

        return profile is null ? Challenge() : View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Challenge();
        }

        var model = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new EditProfileViewModel
            {
                FullName = user.FullName,
                Gender = user.Gender,
                Email = user.Email,
                Mobile = user.Mobile,
                Address = user.Address,
                AvatarUrl = user.AvatarUrl
            })
            .SingleOrDefaultAsync(cancellationToken);

        return model is null ? Challenge() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Challenge();
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return Challenge();
        }

        model.Email = user.Email;
        model.AvatarUrl = user.AvatarUrl;
        if (ModelState.IsValid && !AccountValidation.IsValidFullName(model.FullName))
        {
            ModelState.AddModelError(nameof(model.FullName), "Full name may contain letters and spaces only.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string? newAvatarPath = null;
        if (model.Avatar is not null)
        {
            try
            {
                newAvatarPath = await avatarStorage.SaveAsync(model.Avatar, cancellationToken);
            }
            catch (AvatarStorageException exception)
            {
                ModelState.AddModelError(nameof(model.Avatar), exception.Message);
                return View(model);
            }
        }

        var oldAvatarPath = user.AvatarUrl;
        user.FullName = AccountValidation.NormalizeFullName(model.FullName);
        user.Gender = model.Gender;
        user.Mobile = model.Mobile;
        user.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
        if (newAvatarPath is not null)
        {
            user.AvatarUrl = newAvatarPath;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (newAvatarPath is not null)
            {
                await TryDeleteAvatarAsync(newAvatarPath, cancellationToken);
            }

            throw;
        }

        if (newAvatarPath is not null && !string.IsNullOrWhiteSpace(oldAvatarPath))
        {
            await TryDeleteAvatarAsync(oldAvatarPath, cancellationToken);
        }

        TempData["SuccessMessage"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(AppClaimTypes.UserId), out userId);

    private async Task TryDeleteAvatarAsync(string webPath, CancellationToken cancellationToken)
    {
        try
        {
            await avatarStorage.DeleteAsync(webPath, cancellationToken);
        }
        catch (AvatarStorageException exception)
        {
            logger.LogWarning(exception, "Could not delete avatar at {AvatarPath}.", webPath);
        }
    }
}
