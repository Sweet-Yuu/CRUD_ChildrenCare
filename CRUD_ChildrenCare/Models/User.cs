namespace CRUD_ChildrenCare.Models
{
    public class User
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Role { get; set; } = "Customer"; // Admin, Manager, Staff, Customer
        public bool Status { get; set; } = true;
    }
}
