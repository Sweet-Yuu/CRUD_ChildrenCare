using System.ComponentModel.DataAnnotations;

namespace CRUD_ChildrenCare.ViewModels.Account;

public sealed class LoginViewModel
{
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
