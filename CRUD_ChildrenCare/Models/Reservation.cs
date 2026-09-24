namespace CRUD_ChildrenCare.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? StaffId { get; set; } // Doctor or Nurse assigned
        public DateTime ReservedDate { get; set; } = DateTime.Now;
        public DateTime CheckUpTime { get; set; }
        public decimal TotalCost { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverGender { get; set; } = string.Empty;
        public string ReceiverEmail { get; set; } = string.Empty;
        public string ReceiverMobile { get; set; } = string.Empty;
        public string ReceiverAddress { get; set; } = string.Empty;
        public string Status { get; set; } = "Submitted"; // Submitted, Approved, Cancelled, Completed

        public List<ReservationDetail> Details { get; set; } = new();
    }

    public class ReservationDetail
    {
        public int Id { get; set; }
        public int ReservationId { get; set; }
        public int ServiceId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public Service? Service { get; set; }
    }
}