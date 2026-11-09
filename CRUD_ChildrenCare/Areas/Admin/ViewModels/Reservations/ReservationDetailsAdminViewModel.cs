using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Reservations;

public sealed class ReservationDetailsAdminViewModel
{
    public required Reservation Reservation { get; set; }
    public IEnumerable<User> Staffs { get; set; } = [];
    public string CurrentRole { get; set; } = string.Empty;

    public bool CanManage => CurrentRole is "Manager" or "Admin";
    public bool CanComplete => (CurrentRole is "Doctor" or "Manager" or "Admin") && Reservation.Status == ReservationStatus.Submitted;
    public bool CanCancel => CanManage && Reservation.Status == ReservationStatus.Submitted;
}
