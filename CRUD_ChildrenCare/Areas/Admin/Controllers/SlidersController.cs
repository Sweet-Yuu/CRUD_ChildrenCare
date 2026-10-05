using CRUD_ChildrenCare.Areas.Admin.ViewModels.Sliders;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemData.RoleValues.Manager + "," + SystemData.RoleValues.Admin)]
public sealed class SlidersController(ApplicationDbContext dbContext) : Controller
{
    private const int PageSize = 10;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, string? sort, int page = 1)
    {
        var query = dbContext.Sliders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Title.Contains(search) || p.BackLink.Contains(search));
        if (Enum.TryParse<SliderStatus>(status, out var s)) query = query.Where(p => p.Status == s);

        query = sort switch
        {
            "Title" => query.OrderBy(p => p.Title),
            "TitleDesc" => query.OrderByDescending(p => p.Title),
            "BackLink" => query.OrderBy(p => p.BackLink),
            "Status" => query.OrderBy(p => p.Status),
            _ => query.OrderByDescending(p => p.UpdatedDate),
        };

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var sliders = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        return View(new SliderListViewModel
        {
            Sliders = sliders, SearchQuery = search, Status = status, Sort = sort,
            PageIndex = page, TotalPages = totalPages
        });
    }

    [HttpGet]
    public IActionResult Create() => View(new SliderFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SliderFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var slider = new Slider
        {
            Title = model.Title, ImageUrl = model.ImageUrl, BackLink = model.BackLink,
            Status = model.Status, Notes = model.Notes
        };

        dbContext.Sliders.Add(slider);
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var slider = await dbContext.Sliders.FindAsync(id);
        if (slider == null) return NotFound();

        return View(new SliderFormViewModel
        {
            Id = slider.Id, Title = slider.Title, ImageUrl = slider.ImageUrl, BackLink = slider.BackLink,
            Status = slider.Status, Notes = slider.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SliderFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var slider = await dbContext.Sliders.FindAsync(model.Id);
        if (slider == null) return NotFound();

        slider.Title = model.Title; slider.ImageUrl = model.ImageUrl; slider.BackLink = model.BackLink;
        slider.Status = model.Status; slider.Notes = model.Notes;

        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var slider = await dbContext.Sliders.FindAsync(id);
        if (slider == null) return NotFound();
        slider.Status = slider.Status == SliderStatus.Active ? SliderStatus.Inactive : SliderStatus.Active;
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
