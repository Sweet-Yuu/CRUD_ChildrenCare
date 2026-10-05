using System.ComponentModel.DataAnnotations;
using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.ViewModels.Account;

public sealed class RegisterViewModel
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

    [Required]
    [DataType(DataType.Password)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,50}$",
        ErrorMessage = "Password must be 8-50 characters and include uppercase, lowercase, and a number.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
