using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.ViewModels.Profile;

public sealed class ProfileViewModel
{
    public string FullName { get; init; } = string.Empty;
    public Gender Gender { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Mobile { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? AvatarUrl { get; init; }
    public string RoleName { get; init; } = string.Empty;
}
