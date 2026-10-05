using CRUD_ChildrenCare.Areas.Admin.ViewModels.Posts;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CRUD_ChildrenCare.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemData.RoleValues.Manager + "," + SystemData.RoleValues.Admin)]
public sealed class PostsController(ApplicationDbContext dbContext) : Controller
{
    private const int PageSize = 10;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? categoryId, int? authorId, string? status, string? sort, int page = 1)
    {
        var query = dbContext.Posts
            .Include(p => p.Category)
            .Include(p => p.Author)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Title.Contains(search));
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        if (authorId.HasValue) query = query.Where(p => p.AuthorId == authorId.Value);
        if (Enum.TryParse<PostStatus>(status, out var s)) query = query.Where(p => p.Status == s);

        query = sort switch
        {
            "Title" => query.OrderBy(p => p.Title),
            "TitleDesc" => query.OrderByDescending(p => p.Title),
            "Category" => query.OrderBy(p => p.Category.Name),
            "Author" => query.OrderBy(p => p.Author.FullName),
            "Status" => query.OrderBy(p => p.Status),
            _ => query.OrderByDescending(p => p.UpdatedDate),
        };

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var posts = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        var categories = await dbContext.Settings.Where(s => s.Type == SettingType.PostCategory).ToListAsync();
        var authors = await dbContext.Users.Where(u => u.Role.Name == SystemData.RoleValues.Manager || u.Role.Name == SystemData.RoleValues.Admin).ToListAsync();

        return View(new PostListViewModel
        {
            Posts = posts, Categories = categories, Authors = authors,
            SearchQuery = search, CategoryId = categoryId, AuthorId = authorId, Status = status, Sort = sort,
            PageIndex = page, TotalPages = totalPages
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(new PostFormViewModel
        {
            Categories = await dbContext.Settings.Where(s => s.Type == SettingType.PostCategory && s.Status == SettingStatus.Active).ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PostFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await dbContext.Settings.Where(s => s.Type == SettingType.PostCategory && s.Status == SettingStatus.Active).ToListAsync();
            return View(model);
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        var post = new Post
        {
            Title = model.Title, Thumbnail = model.Thumbnail, CategoryId = model.CategoryId,
            BriefInfo = model.BriefInfo, Description = model.Description, IsFeatured = model.IsFeatured,
            Status = model.Status, AuthorId = userId
        };

        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var post = await dbContext.Posts.FindAsync(id);
        if (post == null) return NotFound();

        return View(new PostFormViewModel
        {
            Id = post.Id, Title = post.Title, Thumbnail = post.Thumbnail, CategoryId = post.CategoryId,
            BriefInfo = post.BriefInfo, Description = post.Description, IsFeatured = post.IsFeatured, Status = post.Status,
            Categories = await dbContext.Settings.Where(s => s.Type == SettingType.PostCategory && s.Status == SettingStatus.Active).ToListAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PostFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await dbContext.Settings.Where(s => s.Type == SettingType.PostCategory && s.Status == SettingStatus.Active).ToListAsync();
            return View(model);
        }

        var post = await dbContext.Posts.FindAsync(model.Id);
        if (post == null) return NotFound();

        post.Title = model.Title; post.Thumbnail = model.Thumbnail; post.CategoryId = model.CategoryId;
        post.BriefInfo = model.BriefInfo; post.Description = model.Description; post.IsFeatured = model.IsFeatured;
        post.Status = model.Status;

        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var post = await dbContext.Posts.FindAsync(id);
        if (post == null) return NotFound();
        post.Status = post.Status == PostStatus.Published ? PostStatus.Hidden : PostStatus.Published;
        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
