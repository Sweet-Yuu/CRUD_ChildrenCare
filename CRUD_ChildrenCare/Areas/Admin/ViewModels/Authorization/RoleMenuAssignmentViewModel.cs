using System.ComponentModel.DataAnnotations;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Authorization;

public sealed class RoleMenuAssignmentViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select an active role.")]
    [Display(Name = "Role")]
    public int RoleId { get; set; }

    public List<int> SelectedMenuIds { get; set; } = [];
    public IReadOnlyList<AuthorizationOptionViewModel> Roles { get; set; } = [];
    public IReadOnlyList<AuthorizationOptionViewModel> Menus { get; set; } = [];
}
