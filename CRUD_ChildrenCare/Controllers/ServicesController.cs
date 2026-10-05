using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models.Enums;
using CRUD_ChildrenCare.ViewModels.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Controllers;

[AllowAnonymous]
public sealed class ServicesController(ApplicationDbContext dbContext) : Controller
{
    private const int PageSize = 10;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
    {
        var query = dbContext.Services
            .Include(s => s.Category)
            .Where(s => s.Status == ServiceStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Title.Contains(search) || s.BriefInfo.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(s => s.CategoryId == categoryId.Value);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var services = await query
            .OrderByDescending(s => s.UpdatedDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var categories = await dbContext.Settings
            .Where(s => s.Type == SettingType.ServiceCategory && s.Status == SettingStatus.Active)
            .ToListAsync();

        return View(new ServiceListPublicViewModel
        {
            Services = services,
            Categories = categories,
            SearchQuery = search,
            CategoryId = categoryId,
            PageIndex = page,
            TotalPages = totalPages
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var service = await dbContext.Services
            .Include(s => s.Category)
            .Include(s => s.ServiceImages)
            .FirstOrDefaultAsync(s => s.Id == id && s.Status == ServiceStatus.Active);

        if (service == null)
        {
            return NotFound();
        }

        var relatedServices = await dbContext.Services
            .Where(s => s.CategoryId == service.CategoryId && s.Id != service.Id && s.Status == ServiceStatus.Active)
            .Take(3)
            .ToListAsync();

        return View(new ServiceDetailPublicViewModel
        {
            Service = service,
            GalleryImages = service.ServiceImages.OrderBy(img => img.SortOrder).ToList(),
            RelatedServices = relatedServices
        });
    }
}
