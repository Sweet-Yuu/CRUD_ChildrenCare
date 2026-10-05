using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Models;

public sealed class Setting
{
    public int Id { get; set; }
    public SettingType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SettingStatus Status { get; set; } = SettingStatus.Active;
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<RoleMenu> RoleMenuRoles { get; set; } = new List<RoleMenu>();
    public ICollection<RoleMenu> RoleMenuEntries { get; set; } = new List<RoleMenu>();
}
