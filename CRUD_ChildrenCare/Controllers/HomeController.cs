using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRUD_ChildrenCare.Controllers;

[AllowAnonymous]
public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

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
