using System.ComponentModel.DataAnnotations;

namespace CRUD_ChildrenCare.ViewModels.Public;

public sealed class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
}
