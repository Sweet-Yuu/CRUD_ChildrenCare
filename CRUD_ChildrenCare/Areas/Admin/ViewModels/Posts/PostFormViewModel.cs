using System.ComponentModel.DataAnnotations;
using CRUD_ChildrenCare.Models.Enums;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Posts;

public class PostFormViewModel
{
    public int Id { get; set; }
    
    [Required, StringLength(200, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;
    
    [Required, StringLength(255)]
    public string Thumbnail { get; set; } = string.Empty;
    
    [Required]
    public int CategoryId { get; set; }
    
    [Required, StringLength(500, MinimumLength = 10)]
    public string BriefInfo { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public bool IsFeatured { get; set; }
    
    public PostStatus Status { get; set; } = PostStatus.Hidden;

    public IEnumerable<Setting> Categories { get; set; } = new List<Setting>();
}
