using CRUD_ChildrenCare.Infrastructure;
using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Settings;

public sealed class SettingsIndexViewModel
{
    public required PagedResult<SettingListItemViewModel> Results { get; init; }
    public string? Search { get; init; }
    public SettingType? Type { get; init; }
    public SettingStatus? Status { get; init; }
    public string Sort { get; init; } = "Id";
    public string Direction { get; init; } = "asc";
}
