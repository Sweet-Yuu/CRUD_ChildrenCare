using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Services.Reservations;

public class ReservationService(ApplicationDbContext dbContext) : IReservationService
{
    public async Task<Reservation> GetOrCreateCartAsync(int? userId, string sessionCartId)
    {
        Reservation? cart = null;

        if (userId.HasValue)
        {
            cart = await dbContext.Reservations
                .Include(r => r.ReservationItems).ThenInclude(i => i.Service)
                .FirstOrDefaultAsync(r => r.UserId == userId.Value && r.Status == ReservationStatus.Cart);
        }
        else if (!string.IsNullOrEmpty(sessionCartId) && int.TryParse(sessionCartId, out int cartId))
        {
            cart = await dbContext.Reservations
                .Include(r => r.ReservationItems).ThenInclude(i => i.Service)
                .FirstOrDefaultAsync(r => r.Id == cartId && r.UserId == null && r.Status == ReservationStatus.Cart);
        }

        if (cart == null)
        {
            cart = new Reservation
            {
                UserId = userId,
                Status = ReservationStatus.Cart,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            dbContext.Reservations.Add(cart);
            await dbContext.SaveChangesAsync();
        }

        // Update latest prices
        bool priceUpdated = false;
        foreach (var item in cart.ReservationItems)
        {
            if (item.Service != null && item.UnitPrice != item.Service.SalePrice)
            {
                item.UnitPrice = item.Service.SalePrice;
                item.TotalCost = item.UnitPrice * item.Quantity * item.NumberOfPerson;
                priceUpdated = true;
            }
        }
        
        cart.TotalCost = cart.ReservationItems.Sum(i => i.TotalCost);
        if (priceUpdated || cart.TotalCost != cart.ReservationItems.Sum(i => i.TotalCost))
        {
            await dbContext.SaveChangesAsync();
        }

        return cart;
    }

    public async Task AddToCartAsync(int? userId, string sessionCartId, int serviceId, int quantity, int numberOfPerson)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionCartId);
        var service = await dbContext.Services.FindAsync(serviceId);
        
        if (service == null || service.Status != ServiceStatus.Active) return;

        var existingItem = cart.ReservationItems.FirstOrDefault(i => i.ServiceId == serviceId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
            existingItem.TotalCost = existingItem.UnitPrice * existingItem.Quantity * existingItem.NumberOfPerson;
        }
        else
        {
            var newItem = new ReservationItem
            {
                ReservationId = cart.Id,
                ServiceId = serviceId,
                UnitPrice = service.SalePrice,
                Quantity = quantity,
                NumberOfPerson = numberOfPerson,
                TotalCost = service.SalePrice * quantity * numberOfPerson
            };
            dbContext.ReservationItems.Add(newItem);
            cart.ReservationItems.Add(newItem);
        }

        cart.TotalCost = cart.ReservationItems.Sum(i => i.TotalCost);
        cart.UpdatedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateCartItemAsync(int? userId, string sessionCartId, int itemId, int quantity, int numberOfPerson)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionCartId);
        var item = cart.ReservationItems.FirstOrDefault(i => i.Id == itemId);
        
        if (item != null)
        {
            item.Quantity = quantity;
            item.NumberOfPerson = numberOfPerson;
            item.TotalCost = item.UnitPrice * quantity * numberOfPerson;
            
            cart.TotalCost = cart.ReservationItems.Sum(i => i.TotalCost);
            cart.UpdatedDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task RemoveFromCartAsync(int? userId, string sessionCartId, int itemId)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionCartId);
        var item = cart.ReservationItems.FirstOrDefault(i => i.Id == itemId);
        
        if (item != null)
        {
            dbContext.ReservationItems.Remove(item);
            cart.ReservationItems.Remove(item);
            cart.TotalCost = cart.ReservationItems.Sum(i => i.TotalCost);
            cart.UpdatedDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<Reservation?> GetReservationAsync(int id)
    {
        return await dbContext.Reservations
            .Include(r => r.ReservationItems).ThenInclude(i => i.Service)
            .Include(r => r.User)
            .Include(r => r.AssignedStaff)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> SubmitReservationAsync(int id, Reservation submitData)
    {
        var reservation = await dbContext.Reservations
            .Include(r => r.ReservationItems).ThenInclude(i => i.Service)
            .FirstOrDefaultAsync(r => r.Id == id && r.Status == ReservationStatus.Cart);

        if (reservation == null || !reservation.ReservationItems.Any()) return false;

        // Check availability
        foreach (var item in reservation.ReservationItems)
        {
            if (item.Service.AvailableQuantity < item.Quantity)
                return false;
        }

        // Deduct availability
        foreach (var item in reservation.ReservationItems)
        {
            item.Service.AvailableQuantity -= item.Quantity;
        }

        reservation.Status = ReservationStatus.Submitted;
        reservation.ReservedDate = DateTime.UtcNow;
        reservation.CheckupTime = submitData.CheckupTime;
        reservation.ReceiverFullName = submitData.ReceiverFullName;
        reservation.ReceiverGender = submitData.ReceiverGender;
        reservation.ReceiverEmail = submitData.ReceiverEmail;
        reservation.ReceiverMobile = submitData.ReceiverMobile;
        reservation.ReceiverAddress = submitData.ReceiverAddress;
        reservation.Notes = submitData.Notes;
        reservation.UpdatedDate = DateTime.UtcNow;

        // Optionally assign random doctor/nurse here based on some logic, skipping for brevity
        
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<(List<Reservation> Reservations, int TotalPages)> GetMyReservationsAsync(int userId, int pageIndex, int pageSize)
    {
        var query = dbContext.Reservations
            .Include(r => r.ReservationItems).ThenInclude(i => i.Service)
            .Where(r => r.UserId == userId && r.Status != ReservationStatus.Cart)
            .OrderByDescending(r => r.ReservedDate);

        var totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var list = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        return (list, totalPages);
    }

    public async Task<bool> CancelReservationAsync(int id, int userId)
    {
        var reservation = await dbContext.Reservations
            .Include(r => r.ReservationItems).ThenInclude(i => i.Service)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && r.Status == ReservationStatus.Submitted);

        if (reservation == null) return false;

        reservation.Status = ReservationStatus.Cancelled;
        reservation.UpdatedDate = DateTime.UtcNow;

        // Restore availability
        foreach (var item in reservation.ReservationItems)
        {
            item.Service.AvailableQuantity += item.Quantity;
        }

        await dbContext.SaveChangesAsync();
        return true;
    }
}
