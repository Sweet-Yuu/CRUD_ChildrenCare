using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.ViewModels.Admin;

public sealed class SettingListItemViewModel
{
    public int Id { get; init; }
    public SettingType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public SettingStatus Status { get; init; }
}
