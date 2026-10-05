using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.ViewModels.Blogs;

public class BlogListViewModel
{
    public IEnumerable<Post> Posts { get; set; } = new List<Post>();
    public IEnumerable<Setting> Categories { get; set; } = new List<Setting>();
    public string? SearchQuery { get; set; }
    public int? CategoryId { get; set; }
    public int PageIndex { get; set; }
    public int TotalPages { get; set; }
}
