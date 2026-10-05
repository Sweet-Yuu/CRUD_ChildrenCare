using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Models;

public sealed class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public Setting Role { get; set; } = null!;
    public UserStatus Status { get; set; }
    public string? VerifyTokenHash { get; set; }
    public DateTime? VerifyTokenExpiry { get; set; }
    public string? ResetTokenHash { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
