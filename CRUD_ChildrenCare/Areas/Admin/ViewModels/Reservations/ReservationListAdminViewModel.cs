using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Reservations;

public sealed class ReservationListAdminViewModel
{
    public IEnumerable<Reservation> Reservations { get; set; } = [];
    public IEnumerable<User> Staffs { get; set; } = [];

    public string? SearchQuery { get; set; }
    public ReservationStatus? Status { get; set; }
    public int? StaffId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Sort { get; set; }

    public int PageIndex { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }

    public string CurrentRole { get; set; } = string.Empty;
    public bool IsStaffOnly => CurrentRole is "Doctor" or "Nurse";
}
