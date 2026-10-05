namespace CRUD_ChildrenCare.Models;

public sealed class RoleMenu
{
    public int RoleId { get; set; }
    public Setting Role { get; set; } = null!;
    public int MenuId { get; set; }
    public Setting Menu { get; set; } = null!;
}
