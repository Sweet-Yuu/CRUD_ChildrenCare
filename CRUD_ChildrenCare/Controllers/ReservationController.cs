using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRUD_ChildrenCare.Services.Reservations;
using CRUD_ChildrenCare.ViewModels.Reservations;
using System.Security.Claims;

namespace CRUD_ChildrenCare.Controllers;

public class ReservationController(IReservationService reservationService) : Controller
{
    private int? GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdStr, out int userId) ? userId : null;
    }

    private string GetCartSessionId()
    {
        var sessionCartId = HttpContext.Session.GetString("CartId");
        if (string.IsNullOrEmpty(sessionCartId))
        {
            // Usually we create a cart here, but we let service handle it and we return the id back
            return string.Empty; // handled in service
        }
        return sessionCartId;
    }

    [HttpGet]
    public async Task<IActionResult> Cart()
    {
        var cart = await reservationService.GetOrCreateCartAsync(GetCurrentUserId(), GetCartSessionId());
        if (!User.Identity.IsAuthenticated && string.IsNullOrEmpty(HttpContext.Session.GetString("CartId")))
        {
            HttpContext.Session.SetString("CartId", cart.Id.ToString());
        }
        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(int serviceId, int quantity = 1, int numberOfPerson = 1)
    {
        var cartSessionId = GetCartSessionId();
        await reservationService.AddToCartAsync(GetCurrentUserId(), cartSessionId, serviceId, quantity, numberOfPerson);
        
        var cart = await reservationService.GetOrCreateCartAsync(GetCurrentUserId(), cartSessionId);
        if (!User.Identity.IsAuthenticated && string.IsNullOrEmpty(cartSessionId))
        {
            HttpContext.Session.SetString("CartId", cart.Id.ToString());
        }

        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCartItem(int itemId, int quantity, int numberOfPerson)
    {
        await reservationService.UpdateCartItemAsync(GetCurrentUserId(), GetCartSessionId(), itemId, quantity, numberOfPerson);
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int itemId)
    {
        await reservationService.RemoveFromCartAsync(GetCurrentUserId(), GetCartSessionId(), itemId);
        return RedirectToAction(nameof(Cart));
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = await reservationService.GetOrCreateCartAsync(GetCurrentUserId(), GetCartSessionId());
        if (cart.ReservationItems.Count == 0)
        {
            return RedirectToAction(nameof(Cart));
        }

        var model = new ReservationSubmitViewModel
        {
            ReservationId = cart.Id,
            Cart = cart,
            CheckupTime = DateTime.Today.AddDays(1).AddHours(8) // Default to tomorrow 8 AM
        };

        // Fill with user profile if logged in
        if (User.Identity.IsAuthenticated)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userName = User.FindFirstValue(ClaimTypes.Name);
            var userMobile = User.FindFirstValue(ClaimTypes.MobilePhone); // Assuming these claims exist
            
            model.ReceiverFullName = userName ?? string.Empty;
            model.ReceiverEmail = userEmail ?? string.Empty;
            model.ReceiverMobile = userMobile ?? string.Empty;
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(ReservationSubmitViewModel model)
    {
        var cart = await reservationService.GetOrCreateCartAsync(GetCurrentUserId(), GetCartSessionId());
        
        if (!ModelState.IsValid)
        {
            model.Cart = cart;
            return View(model);
        }

        var submitData = new Reservation
        {
            CheckupTime = model.CheckupTime,
            ReceiverFullName = model.ReceiverFullName,
            ReceiverGender = model.ReceiverGender,
            ReceiverEmail = model.ReceiverEmail,
            ReceiverMobile = model.ReceiverMobile,
            ReceiverAddress = model.ReceiverAddress,
            Notes = model.Notes
        };

        bool success = await reservationService.SubmitReservationAsync(cart.Id, submitData);

        if (success)
        {
            if (!User.Identity.IsAuthenticated)
            {
                HttpContext.Session.Remove("CartId");
            }
            return RedirectToAction(nameof(Success), new { id = cart.Id });
        }
        else
        {
            ModelState.AddModelError("", "Dịch vụ đã hết số lượng hoặc có lỗi xảy ra.");
            model.Cart = cart;
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Success(int id)
    {
        return View(id);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyReservations(int page = 1)
    {
        var userId = GetCurrentUserId().Value;
        int pageSize = 10;
        var result = await reservationService.GetMyReservationsAsync(userId, page, pageSize);

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = result.TotalPages;

        return View(result.Reservations);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var reservation = await reservationService.GetReservationAsync(id);
        if (reservation == null || reservation.UserId != GetCurrentUserId())
        {
            return NotFound();
        }

        return View(reservation);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var success = await reservationService.CancelReservationAsync(id, GetCurrentUserId().Value);
        if (success)
        {
            TempData["Message"] = "Hủy đặt lịch thành công.";
        }
        else
        {
            TempData["Error"] = "Không thể hủy đặt lịch này.";
        }
        return RedirectToAction(nameof(Details), new { id = id });
    }
}
