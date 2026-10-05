using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Models;

public sealed class Service
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Setting Category { get; set; } = null!;
    public string BriefInfo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int NumberOfPerson { get; set; } = 1;
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int AvailableQuantity { get; set; }
    public bool IsFeatured { get; set; }
    public ServiceStatus Status { get; set; } = ServiceStatus.Inactive;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }

    public ICollection<ServiceImage> ServiceImages { get; set; } = new List<ServiceImage>();
}
