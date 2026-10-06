using CRUD_ChildrenCare.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD_ChildrenCare.Models;

public sealed class Reservation
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public DateTime ReservedDate { get; set; }
    public DateTime CheckupTime { get; set; }
    
    [MaxLength(100)]
    public string ReceiverFullName { get; set; } = string.Empty;
    public Gender ReceiverGender { get; set; }
    
    [MaxLength(100)]
    public string ReceiverEmail { get; set; } = string.Empty;
    
    [MaxLength(10)]
    public string ReceiverMobile { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? ReceiverAddress { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [Column(TypeName = "decimal(18,0)")]
    public decimal TotalCost { get; set; }
    
    public ReservationStatus Status { get; set; } = ReservationStatus.Cart;
    
    public int? AssignedStaffId { get; set; }
    [ForeignKey("AssignedStaffId")]
    public User? AssignedStaff { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }

    public ICollection<ReservationItem> ReservationItems { get; set; } = new List<ReservationItem>();
}
