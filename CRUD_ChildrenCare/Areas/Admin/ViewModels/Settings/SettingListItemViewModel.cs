using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Settings;

public sealed class SettingListItemViewModel
{
    public int Id { get; init; }
    public SettingType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public SettingStatus Status { get; init; }
}
