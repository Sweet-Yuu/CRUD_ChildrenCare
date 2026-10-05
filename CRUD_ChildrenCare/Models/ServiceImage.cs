namespace CRUD_ChildrenCare.Models;

public sealed class ServiceImage
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
