namespace CRUD_ChildrenCare.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string BriefInfo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Thumbnail { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public bool IsFeatured { get; set; }
        public bool Status { get; set; }
    }
}
