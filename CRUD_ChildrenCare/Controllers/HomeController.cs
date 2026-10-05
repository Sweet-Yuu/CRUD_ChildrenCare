using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models.Enums;
using CRUD_ChildrenCare.ViewModels.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Controllers;

[AllowAnonymous]
public sealed class HomeController(ApplicationDbContext dbContext) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var sliders = await dbContext.Sliders
            .Where(s => s.Status == SliderStatus.Active)
            .OrderBy(s => s.Id) // Fallback sorting, assuming sort order could be id
            .ToListAsync();

        var featuredPosts = await dbContext.Posts
            .Include(p => p.Category)
            .Include(p => p.Author)
            .Where(p => p.Status == PostStatus.Published && p.IsFeatured)
            .OrderByDescending(p => p.UpdatedDate)
            .Take(6) // Take top 6 featured posts for home
            .ToListAsync();

        var model = new HomeViewModel
        {
            Sliders = sliders,
            FeaturedPosts = featuredPosts
        };

        return View(model);
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View();
    }

    [ActionName("NotFound")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult NotFoundPage(int statusCode = StatusCodes.Status404NotFound)
    {
        Response.StatusCode = statusCode;
        return statusCode == StatusCodes.Status404NotFound
            ? View("NotFound")
            : new EmptyResult();
    }
}
