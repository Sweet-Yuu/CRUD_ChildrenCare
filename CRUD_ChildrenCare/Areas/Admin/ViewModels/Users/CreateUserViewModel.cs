using System.ComponentModel.DataAnnotations;
using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Users;

public sealed class CreateUserViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(Gender))]
    public Gender Gender { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Mobile must contain 10 digits and begin with 0.")]
    public string Mobile { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Address { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select an active role.")]
    [Display(Name = "Role")]
    public int RoleId { get; set; }

    public IReadOnlyList<RoleOptionViewModel> Roles { get; set; } = [];
}
