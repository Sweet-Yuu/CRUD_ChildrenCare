using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Models;

public sealed class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Setting Category { get; set; } = null!;
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public string BriefInfo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Hidden;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
