using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.ViewModels.Admin;

public class SliderListViewModel
{
    public IEnumerable<Slider> Sliders { get; set; } = new List<Slider>();
    public string? SearchQuery { get; set; }
    public string? Status { get; set; }
    public string? Sort { get; set; }
    public int PageIndex { get; set; }
    public int TotalPages { get; set; }
}
