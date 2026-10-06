using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CRUD_ChildrenCare.ViewModels.Reservations;

public class ReservationSubmitViewModel
{
    public int ReservationId { get; set; }

    [Required]
    public DateTime CheckupTime { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
    [MaxLength(100)]
    public string ReceiverFullName { get; set; } = string.Empty;

    [Required]
    public Gender ReceiverGender { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập email người nhận")]
    [EmailAddress]
    [MaxLength(100)]
    public string ReceiverEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại người nhận")]
    [MaxLength(10)]
    [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string ReceiverMobile { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? ReceiverAddress { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public Reservation? Cart { get; set; }
}
