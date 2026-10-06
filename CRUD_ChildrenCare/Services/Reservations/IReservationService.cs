using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Services.Reservations;

public interface IReservationService
{
    Task<Reservation> GetOrCreateCartAsync(int? userId, string sessionCartId);
    Task AddToCartAsync(int? userId, string sessionCartId, int serviceId, int quantity, int numberOfPerson);
    Task UpdateCartItemAsync(int? userId, string sessionCartId, int itemId, int quantity, int numberOfPerson);
    Task RemoveFromCartAsync(int? userId, string sessionCartId, int itemId);
    Task<Reservation?> GetReservationAsync(int id);
    Task<bool> SubmitReservationAsync(int id, Reservation submitData);
    Task<(List<Reservation> Reservations, int TotalPages)> GetMyReservationsAsync(int userId, int pageIndex, int pageSize);
    Task<bool> CancelReservationAsync(int id, int userId);
}
