using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Users;

public sealed class UserListItemViewModel
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Mobile { get; init; } = string.Empty;
    public Gender Gender { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public UserStatus Status { get; init; }
}
