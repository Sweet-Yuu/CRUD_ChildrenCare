using System.ComponentModel.DataAnnotations;
using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Settings;

public sealed class SettingFormViewModel
{
    public int Id { get; set; }

    [Required]
    [EnumDataType(typeof(SettingType))]
    public SettingType Type { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Value { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [EnumDataType(typeof(SettingStatus))]
    public SettingStatus Status { get; set; } = SettingStatus.Active;

    public bool IsSystemRole { get; set; }
}
