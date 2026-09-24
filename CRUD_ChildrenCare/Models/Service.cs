namespace CRUD_ChildrenCare.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string BriefInfo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string Thumbnail { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public bool Status { get; set; } = true;
    }
}
