using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.ViewModels.Public;

public sealed class ServiceDetailPublicViewModel
{
    public Service Service { get; set; } = null!;
    public IReadOnlyList<ServiceImage> GalleryImages { get; set; } = [];
    public IReadOnlyList<Service> RelatedServices { get; set; } = [];
}
