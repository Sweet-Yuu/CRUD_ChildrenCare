using System.Security.Claims;
using CRUD_ChildrenCare.Areas.Admin.ViewModels.Reservations;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SystemData.RoleValues.Manager + "," + SystemData.RoleValues.Admin + "," + SystemData.RoleValues.Doctor + "," + SystemData.RoleValues.Nurse)]
public sealed class ReservationsController(ApplicationDbContext dbContext) : Controller
{
    private const int PageSize = 10;

    private string GetCurrentUserRole() =>
        User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdStr, out int userId) ? userId : 0;
    }

    private bool IsManagerOrAdmin =>
        User.IsInRole(SystemData.RoleValues.Manager) || User.IsInRole(SystemData.RoleValues.Admin);

    private async Task<List<User>> GetEligibleStaffsAsync()
    {
        return await dbContext.Users
            .Include(u => u.Role)
            .Where(u => u.Role.Type == SettingType.UserRole &&
                        (u.Role.Name == SystemData.RoleValues.Doctor || u.Role.Name == SystemData.RoleValues.Nurse) &&
                        u.Status == UserStatus.Active)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        ReservationStatus? status,
        int? staffId,
        DateTime? fromDate,
        DateTime? toDate,
        string? sort,
        int page = 1)
    {
        var role = GetCurrentUserRole();
        var userId = GetCurrentUserId();

        var query = dbContext.Reservations
            .Include(r => r.User)
            .Include(r => r.AssignedStaff)
            .Where(r => r.Status != ReservationStatus.Cart)
            .AsQueryable();

        // Staff can only view their own assigned reservations
        if (role is "Doctor" or "Nurse")
        {
            query = query.Where(r => r.AssignedStaffId == userId);
        }
        else if (staffId.HasValue && IsManagerOrAdmin)
        {
            query = query.Where(r => r.AssignedStaffId == staffId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            if (int.TryParse(trimmed, out int searchId))
            {
                query = query.Where(r => r.Id == searchId ||
                                         r.ReceiverFullName.Contains(trimmed) ||
                                         r.ReceiverMobile.Contains(trimmed) ||
                                         r.ReceiverEmail.Contains(trimmed));
            }
            else
            {
                query = query.Where(r => r.ReceiverFullName.Contains(trimmed) ||
                                         r.ReceiverMobile.Contains(trimmed) ||
                                         r.ReceiverEmail.Contains(trimmed));
            }
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

        query = sort switch
        {
            "ReservedDateAsc" => query.OrderBy(r => r.ReservedDate),
            "CheckupTime" => query.OrderBy(r => r.CheckupTime),
            "CheckupTimeDesc" => query.OrderByDescending(r => r.CheckupTime),
            "Receiver" => query.OrderBy(r => r.ReceiverFullName),
            "ReceiverDesc" => query.OrderByDescending(r => r.ReceiverFullName),
            "TotalCost" => query.OrderBy(r => r.TotalCost),
            "TotalCostDesc" => query.OrderByDescending(r => r.TotalCost),
            "Status" => query.OrderBy(r => r.Status),
            _ => query.OrderByDescending(r => r.ReservedDate)
        };

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var reservations = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var staffs = IsManagerOrAdmin ? await GetEligibleStaffsAsync() : [];

        var viewModel = new ReservationListAdminViewModel
        {
            Reservations = reservations,
            Staffs = staffs,
            SearchQuery = search,
            Status = status,
            StaffId = staffId,
            FromDate = fromDate,
            ToDate = toDate,
            Sort = sort,
            PageIndex = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            CurrentRole = role
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var role = GetCurrentUserRole();
        var userId = GetCurrentUserId();

        var reservation = await dbContext.Reservations
            .Include(r => r.ReservationItems).ThenInclude(i => i.Service)
            .Include(r => r.User)
            .Include(r => r.AssignedStaff)
            .FirstOrDefaultAsync(r => r.Id == id && r.Status != ReservationStatus.Cart);

        if (reservation == null)
        {
            return NotFound();
        }

        if (role is "Doctor" or "Nurse" && reservation.AssignedStaffId != userId)
        {
            return Forbid();
        }

        var staffs = IsManagerOrAdmin ? await GetEligibleStaffsAsync() : [];

        var viewModel = new ReservationDetailsAdminViewModel
        {
            Reservation = reservation,
            Staffs = staffs,
            CurrentRole = role
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Roles = SystemData.RoleValues.Manager + "," + SystemData.RoleValues.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var reservation = await dbContext.Reservations
            .Include(r => r.AssignedStaff)
            .FirstOrDefaultAsync(r => r.Id == id && r.Status != ReservationStatus.Cart);

        if (reservation == null)
        {
            return NotFound();
        }

        var viewModel = new ReservationEditAdminViewModel
        {
            Id = reservation.Id,
            Status = reservation.Status,
            AssignedStaffId = reservation.AssignedStaffId,
            CheckupTime = reservation.CheckupTime,
            Notes = reservation.Notes,
            ReceiverFullName = reservation.ReceiverFullName,
            ReceiverMobile = reservation.ReceiverMobile,
            ReceiverEmail = reservation.ReceiverEmail,
            ReceiverAddress = reservation.ReceiverAddress,
            TotalCost = reservation.TotalCost,
            Staffs = await GetEligibleStaffsAsync()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SystemData.RoleValues.Manager + "," + SystemData.RoleValues.Admin)]
    public async Task<IActionResult> Edit(int id, ReservationEditAdminViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var reservation = await dbContext.Reservations
            .Include(r => r.ReservationItems).ThenInclude(i => i.Service)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Staffs = await GetEligibleStaffsAsync();
            model.ReceiverFullName = reservation.ReceiverFullName;
            model.ReceiverMobile = reservation.ReceiverMobile;
            model.ReceiverEmail = reservation.ReceiverEmail;
            model.ReceiverAddress = reservation.ReceiverAddress;
            model.TotalCost = reservation.TotalCost;
            return View(model);
        }

        // Restore availability if transitioning to Cancelled
        if (reservation.Status != ReservationStatus.Cancelled && model.Status == ReservationStatus.Cancelled)
        {
            foreach (var item in reservation.ReservationItems)
            {
                if (item.Service != null)
                {
                    item.Service.AvailableQuantity += item.Quantity;
                }
            }
        }

        reservation.Status = model.Status;
        reservation.AssignedStaffId = model.AssignedStaffId;
        reservation.CheckupTime = model.CheckupTime;
        reservation.Notes = model.Notes;
        reservation.UpdatedDate = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Reservation updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatusAndStaff(int id, ReservationStatus status, int? staffId)
    {
        var role = GetCurrentUserRole();
        var userId = GetCurrentUserId();

        var reservation = await dbContext.Reservations
            .Include(r => r.ReservationItems).ThenInclude(i => i.Service)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        // Doctor can only mark their assigned reservation as Success
        if (role == "Doctor")
        {
            if (reservation.AssignedStaffId != userId)
            {
                return Forbid();
            }
            if (status != ReservationStatus.Success)
            {
                return BadRequest("Doctors can only mark reservations as Success.");
            }
            reservation.Status = ReservationStatus.Success;
        }
        else if (IsManagerOrAdmin)
        {
            if (reservation.Status != ReservationStatus.Cancelled && status == ReservationStatus.Cancelled)
            {
                foreach (var item in reservation.ReservationItems)
                {
                    if (item.Service != null)
                    {
                        item.Service.AvailableQuantity += item.Quantity;
                    }
                }
            }
            reservation.Status = status;
            reservation.AssignedStaffId = staffId;
        }
        else
        {
            return Forbid();
        }

        reservation.UpdatedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Reservation status updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SystemData.RoleValues.Manager + "," + SystemData.RoleValues.Admin)]
    public async Task<IActionResult> Cancel(int id)
    {
        var reservation = await dbContext.Reservations
            .Include(r => r.ReservationItems).ThenInclude(i => i.Service)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        if (reservation.Status != ReservationStatus.Cancelled)
        {
            reservation.Status = ReservationStatus.Cancelled;
            reservation.UpdatedDate = DateTime.UtcNow;

            foreach (var item in reservation.ReservationItems)
            {
                if (item.Service != null)
                {
                    item.Service.AvailableQuantity += item.Quantity;
                }
            }

            await dbContext.SaveChangesAsync();
            TempData["SuccessMessage"] = "Reservation cancelled and service quantity restored.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
