using System.ComponentModel.DataAnnotations;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Reservations;

public sealed class ReservationEditAdminViewModel
{
    public int Id { get; set; }

    [Display(Name = "Trạng thái")]
    public ReservationStatus Status { get; set; }

    [Display(Name = "Nhân viên phụ trách")]
    public int? AssignedStaffId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian khám")]
    [Display(Name = "Thời gian khám")]
    public DateTime CheckupTime { get; set; }

    [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    [Display(Name = "Họ và tên người nhận")]
    public string ReceiverFullName { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    public string ReceiverMobile { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string ReceiverEmail { get; set; } = string.Empty;

    [Display(Name = "Địa chỉ")]
    public string? ReceiverAddress { get; set; }

    [Display(Name = "Tổng tiền")]
    public decimal TotalCost { get; set; }

    public IEnumerable<User> Staffs { get; set; } = [];
}
