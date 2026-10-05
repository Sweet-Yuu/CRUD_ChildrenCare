using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Posts;

public class PostListViewModel
{
    public IEnumerable<Post> Posts { get; set; } = new List<Post>();
    public IEnumerable<Setting> Categories { get; set; } = new List<Setting>();
    public IEnumerable<User> Authors { get; set; } = new List<User>();
    public string? SearchQuery { get; set; }
    public int? CategoryId { get; set; }
    public int? AuthorId { get; set; }
    public string? Status { get; set; }
    public string? Sort { get; set; }
    public int PageIndex { get; set; }
    public int TotalPages { get; set; }
}
