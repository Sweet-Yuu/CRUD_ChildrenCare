using CRUD_ChildrenCare.Areas.Admin.ViewModels.Services;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using CRUD_ChildrenCare.Services.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CRUD_ChildrenCare.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemData.RoleValues.Manager + "," + SystemData.RoleValues.Admin + "," + SystemData.RoleValues.Doctor + "," + SystemData.RoleValues.Nurse)]
public sealed class ServicesController(
    ApplicationDbContext dbContext,
    IAvatarStorage avatarStorage) : Controller
{
    private const int PageSize = 10;

    private bool IsManagerOrAdmin => User.IsInRole(SystemData.RoleValues.Manager) || User.IsInRole(SystemData.RoleValues.Admin);

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? categoryId, string? status, string? sort, int page = 1)
    {
        var query = dbContext.Services
            .Include(s => s.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Title.Contains(search) || s.BriefInfo.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(s => s.CategoryId == categoryId.Value);
        }

        if (Enum.TryParse<ServiceStatus>(status, out var st))
        {
            query = query.Where(s => s.Status == st);
        }

        query = sort switch
        {
            "Title" => query.OrderBy(s => s.Title),
            "TitleDesc" => query.OrderByDescending(s => s.Title),
            "Category" => query.OrderBy(s => s.Category.Name),
            "ListPrice" => query.OrderBy(s => s.ListPrice),
            "ListPriceDesc" => query.OrderByDescending(s => s.ListPrice),
            "SalePrice" => query.OrderBy(s => s.SalePrice),
            "SalePriceDesc" => query.OrderByDescending(s => s.SalePrice),
            "Status" => query.OrderBy(s => s.Status),
            "Featured" => query.OrderByDescending(s => s.IsFeatured),
            _ => query.OrderByDescending(s => s.UpdatedDate)
        };

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var services = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        var categories = await dbContext.Settings
            .Where(s => s.Type == SettingType.ServiceCategory)
            .ToListAsync();

        return View(new ServiceListAdminViewModel
        {
            Services = services,
            Categories = categories,
            SearchQuery = search,
            CategoryId = categoryId,
            Status = status,
            Sort = sort,
            PageIndex = page,
            TotalPages = totalPages,
            CanManage = IsManagerOrAdmin
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var service = await dbContext.Services
            .Include(s => s.Category)
            .Include(s => s.ServiceImages)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
        {
            return NotFound();
        }

        return View(service);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsManagerOrAdmin)
        {
            return Forbid();
        }

        return View(new ServiceFormViewModel
        {
            Categories = await GetActiveCategoriesAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceFormViewModel model)
    {
        if (!IsManagerOrAdmin)
        {
            return Forbid();
        }

        if (model.SalePrice > model.ListPrice)
        {
            ModelState.AddModelError(nameof(model.SalePrice), "Sale price cannot be greater than list price.");
        }

        if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
        {
            try
            {
                model.Thumbnail = await avatarStorage.SaveAsync(model.ThumbnailFile, HttpContext.RequestAborted);
            }
            catch (AvatarStorageException ex)
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), ex.Message);
            }
        }
        else if (string.IsNullOrWhiteSpace(model.Thumbnail))
        {
            ModelState.AddModelError(nameof(model.Thumbnail), "Thumbnail image is required.");
        }

        if (!ModelState.IsValid)
        {
            model.Categories = await GetActiveCategoriesAsync();
            return View(model);
        }

        var service = new Service
        {
            Title = model.Title,
            Thumbnail = model.Thumbnail,
            CategoryId = model.CategoryId,
            BriefInfo = model.BriefInfo,
            Description = model.Description,
            NumberOfPerson = model.NumberOfPerson,
            ListPrice = model.ListPrice,
            SalePrice = model.SalePrice,
            AvailableQuantity = model.AvailableQuantity,
            IsFeatured = model.IsFeatured,
            Status = model.Status
        };

        dbContext.Services.Add(service);
        await dbContext.SaveChangesAsync();

        if (model.NewImageFiles != null && model.NewImageFiles.Count > 0)
        {
            int sortOrder = 1;
            foreach (var file in model.NewImageFiles.Take(10))
            {
                if (file.Length > 0)
                {
                    try
                    {
                        var url = await avatarStorage.SaveAsync(file, HttpContext.RequestAborted);
                        dbContext.ServiceImages.Add(new ServiceImage
                        {
                            ServiceId = service.Id,
                            ImageUrl = url,
                            SortOrder = sortOrder++
                        });
                    }
                    catch
                    {
                    }
                }
            }
            await dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsManagerOrAdmin)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        var service = await dbContext.Services
            .Include(s => s.ServiceImages)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
        {
            return NotFound();
        }

        return View(new ServiceFormViewModel
        {
            Id = service.Id,
            Title = service.Title,
            Thumbnail = service.Thumbnail,
            CategoryId = service.CategoryId,
            BriefInfo = service.BriefInfo,
            Description = service.Description,
            NumberOfPerson = service.NumberOfPerson,
            ListPrice = service.ListPrice,
            SalePrice = service.SalePrice,
            AvailableQuantity = service.AvailableQuantity,
            IsFeatured = service.IsFeatured,
            Status = service.Status,
            Categories = await GetActiveCategoriesAsync(),
            ExistingImages = service.ServiceImages.OrderBy(i => i.SortOrder).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ServiceFormViewModel model)
    {
        if (!IsManagerOrAdmin)
        {
            return Forbid();
        }

        if (model.SalePrice > model.ListPrice)
        {
            ModelState.AddModelError(nameof(model.SalePrice), "Sale price cannot be greater than list price.");
        }

        var service = await dbContext.Services
            .Include(s => s.ServiceImages)
            .FirstOrDefaultAsync(s => s.Id == model.Id);

        if (service == null)
        {
            return NotFound();
        }

        if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
        {
            try
            {
                model.Thumbnail = await avatarStorage.SaveAsync(model.ThumbnailFile, HttpContext.RequestAborted);
            }
            catch (AvatarStorageException ex)
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), ex.Message);
            }
        }

        if (!ModelState.IsValid)
        {
            model.Categories = await GetActiveCategoriesAsync();
            model.ExistingImages = service.ServiceImages.OrderBy(i => i.SortOrder).ToList();
            return View(model);
        }

        service.Title = model.Title;
        if (!string.IsNullOrWhiteSpace(model.Thumbnail)) service.Thumbnail = model.Thumbnail;
        service.CategoryId = model.CategoryId;
        service.BriefInfo = model.BriefInfo;
        service.Description = model.Description;
        service.NumberOfPerson = model.NumberOfPerson;
        service.ListPrice = model.ListPrice;
        service.SalePrice = model.SalePrice;
        service.AvailableQuantity = model.AvailableQuantity;
        service.IsFeatured = model.IsFeatured;
        service.Status = model.Status;

        if (model.NewImageFiles != null && model.NewImageFiles.Count > 0)
        {
            int currentCount = service.ServiceImages.Count;
            int maxNew = 10 - currentCount;
            int sortOrder = currentCount + 1;

            foreach (var file in model.NewImageFiles.Take(Math.Max(0, maxNew)))
            {
                if (file.Length > 0)
                {
                    try
                    {
                        var url = await avatarStorage.SaveAsync(file, HttpContext.RequestAborted);
                        dbContext.ServiceImages.Add(new ServiceImage
                        {
                            ServiceId = service.Id,
                            ImageUrl = url,
                            SortOrder = sortOrder++
                        });
                    }
                    catch
                    {
                    }
                }
            }
        }

        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        if (!IsManagerOrAdmin)
        {
            return Forbid();
        }

        var service = await dbContext.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound();
        }

        service.Status = service.Status == ServiceStatus.Active ? ServiceStatus.Inactive : ServiceStatus.Active;
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int serviceId)
    {
        if (!IsManagerOrAdmin)
        {
            return Forbid();
        }

        var image = await dbContext.ServiceImages.FirstOrDefaultAsync(img => img.Id == imageId && img.ServiceId == serviceId);
        if (image != null)
        {
            dbContext.ServiceImages.Remove(image);
            await dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Edit), new { id = serviceId });
    }

    private Task<List<Setting>> GetActiveCategoriesAsync()
    {
        return dbContext.Settings
            .Where(s => s.Type == SettingType.ServiceCategory && s.Status == SettingStatus.Active)
            .ToListAsync();
    }
}
