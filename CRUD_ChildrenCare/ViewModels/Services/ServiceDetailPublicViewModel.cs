using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.ViewModels.Services;

public sealed class ServiceDetailPublicViewModel
{
    public Service Service { get; set; } = null!;
    public IReadOnlyList<ServiceImage> GalleryImages { get; set; } = [];
    public IReadOnlyList<Service> RelatedServices { get; set; } = [];
}
