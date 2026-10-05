using System.ComponentModel.DataAnnotations;
using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Sliders;

public class SliderFormViewModel
{
    public int Id { get; set; }
    
    [Required, StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;
    
    [Required, StringLength(255)]
    public string ImageUrl { get; set; } = string.Empty;
    
    [Required, StringLength(500)]
    [Url]
    public string BackLink { get; set; } = string.Empty;
    
    public SliderStatus Status { get; set; } = SliderStatus.Active;
    
    [StringLength(500)]
    public string? Notes { get; set; }
}
