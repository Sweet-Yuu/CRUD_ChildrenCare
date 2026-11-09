using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRUD_ChildrenCare.Controllers;

[Authorize(Roles = "Manager,Doctor,Nurse,Admin")]
public sealed class AdminReservationController : Controller
{
    [HttpGet]
    public IActionResult Index(string? search, int? status, int? staffId, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        return RedirectToAction("Index", "Reservations", new { area = "Admin", search, status, staffId, fromDate, toDate, page });
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        return RedirectToAction("Details", "Reservations", new { area = "Admin", id });
    }
}
