using CRUD_ChildrenCare.Infrastructure;
using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Users;

public sealed class UsersIndexViewModel
{
    public required PagedResult<UserListItemViewModel> Results { get; init; }
    public required IReadOnlyList<RoleOptionViewModel> Roles { get; init; }
    public string? Search { get; init; }
    public Gender? Gender { get; init; }
    public int? RoleId { get; init; }
    public UserStatus? Status { get; init; }
    public string Sort { get; init; } = "Id";
    public string Direction { get; init; } = "asc";
}
