using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Models;

public sealed class Slider
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string BackLink { get; set; } = string.Empty;
    public SliderStatus Status { get; set; } = SliderStatus.Active;
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
