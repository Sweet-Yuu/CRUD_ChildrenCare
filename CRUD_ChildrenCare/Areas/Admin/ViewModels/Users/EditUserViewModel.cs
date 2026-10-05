using System.ComponentModel.DataAnnotations;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Users;

public sealed class EditUserViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select an active role.")]
    [Display(Name = "Role")]
    public int RoleId { get; set; }

    [Required]
    [EnumDataType(typeof(UserStatus))]
    public UserStatus Status { get; set; }

    [BindNever]
    public string FullName { get; set; } = string.Empty;

    [BindNever]
    public string Email { get; set; } = string.Empty;

    [BindNever]
    public string Mobile { get; set; } = string.Empty;

    [BindNever]
    public string? Address { get; set; }

    public IReadOnlyList<RoleOptionViewModel> Roles { get; set; } = [];
}
