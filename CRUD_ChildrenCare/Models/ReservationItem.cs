using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD_ChildrenCare.Models;

public sealed class ReservationItem
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,0)")]
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int NumberOfPerson { get; set; }
    
    [Column(TypeName = "decimal(18,0)")]
    public decimal TotalCost { get; set; }
}
