using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CRUD_ChildrenCare.Controllers;

[Authorize(Roles = "Manager,Doctor,Nurse")]
public class AdminReservationController(ApplicationDbContext dbContext) : Controller
{
    private string GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdStr, out int userId) ? userId : 0;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, ReservationStatus? status, int? staffId, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var role = GetCurrentUserRole();
        var userId = GetCurrentUserId();

        var query = dbContext.Reservations
            .Include(r => r.User)
            .Include(r => r.AssignedStaff)
            .Where(r => r.Status != ReservationStatus.Cart)
            .AsQueryable();

        // Staff can only see their own assigned reservations
        if (role == "Doctor" || role == "Nurse")
        {
            query = query.Where(r => r.AssignedStaffId == userId);
        }
        else if (staffId.HasValue && role == "Manager")
        {
            query = query.Where(r => r.AssignedStaffId == staffId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.ReceiverFullName.Contains(search) || r.Id.ToString() == search);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.ReservedDate >= fromDate.Value.Date);
        }
        
        if (toDate.HasValue)
        {
            query = query.Where(r => r.ReservedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        int pageSize = 10;
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var reservations = await query
            .OrderByDescending(r => r.ReservedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.StaffId = staffId;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Role = role;

        if (role == "Manager")
        {
            ViewBag.Staffs = await dbContext.Users
                .Where(u => u.Role.Type == SettingType.UserRole && (u.Role.Name == "Doctor" || u.Role.Name == "Nurse"))
                .ToListAsync();
        }

        return View(reservations);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var role = GetCurrentUserRole();
        var userId = GetCurrentUserId();

        var query = dbContext.Reservations
            .Include(r => r.ReservationItems).ThenInclude(i => i.Service)
            .Include(r => r.User)
            .Include(r => r.AssignedStaff)
            .Where(r => r.Id == id);

        if (role == "Doctor" || role == "Nurse")
        {
            query = query.Where(r => r.AssignedStaffId == userId);
        }

        var reservation = await query.FirstOrDefaultAsync();

        if (reservation == null) return NotFound();

        if (role == "Manager")
        {
            ViewBag.Staffs = await dbContext.Users
                .Where(u => u.Role.Type == SettingType.UserRole && (u.Role.Name == "Doctor" || u.Role.Name == "Nurse"))
                .ToListAsync();
        }

        return View(reservation);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> UpdateStatusAndStaff(int id, ReservationStatus status, int? staffId)
    {
        var reservation = await dbContext.Reservations.FindAsync(id);
        if (reservation == null) return NotFound();

        reservation.Status = status;
        reservation.AssignedStaffId = staffId;
        reservation.UpdatedDate = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        TempData["Message"] = "Cập nhật thành công.";
        return RedirectToAction(nameof(Details), new { id = id });
    }
}
