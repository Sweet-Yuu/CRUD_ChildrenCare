using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.ViewModels.Services;

public sealed class ServiceListPublicViewModel
{
    public IReadOnlyList<Service> Services { get; set; } = [];
    public IReadOnlyList<Setting> Categories { get; set; } = [];
    public string? SearchQuery { get; set; }
    public int? CategoryId { get; set; }
    public int PageIndex { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}
