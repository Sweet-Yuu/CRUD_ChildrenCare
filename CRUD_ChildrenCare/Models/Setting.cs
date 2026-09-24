namespace CRUD_ChildrenCare.Models
{
    public class Setting
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // User Role, Service Category, Post Category
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool Status { get; set; } = true;
    }
}