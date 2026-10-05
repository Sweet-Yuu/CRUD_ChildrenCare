using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using CRUD_ChildrenCare.ViewModels.Blogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Controllers;

[AllowAnonymous]
public sealed class BlogsController(ApplicationDbContext dbContext) : Controller
{
    private const int PageSize = 10;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
    {
        var query = dbContext.Posts
            .Include(p => p.Category)
            .Include(p => p.Author)
            .Where(p => p.Status == PostStatus.Published)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Title.Contains(search) || p.BriefInfo.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var posts = await query
            .OrderByDescending(p => p.UpdatedDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var categories = await dbContext.Settings
            .Where(s => s.Type == SettingType.PostCategory && s.Status == SettingStatus.Active)
            .ToListAsync();

        var model = new BlogListViewModel
        {
            Posts = posts,
            Categories = categories,
            SearchQuery = search,
            CategoryId = categoryId,
            PageIndex = page,
            TotalPages = totalPages
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var post = await dbContext.Posts
            .Include(p => p.Category)
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == id && p.Status == PostStatus.Published);

        if (post == null)
        {
            return NotFound();
        }

        return View(post);
    }
}
