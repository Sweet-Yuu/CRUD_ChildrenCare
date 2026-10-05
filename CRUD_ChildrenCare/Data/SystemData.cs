using CRUD_ChildrenCare.Models.Enums;

namespace CRUD_ChildrenCare.Data;

public static class SystemData
{
    public static class RoleValues
    {
        public const string Customer = "Customer";
        public const string Doctor = "Doctor";
        public const string Nurse = "Nurse";
        public const string Manager = "Manager";
        public const string Admin = "Admin";

        public static readonly IReadOnlyList<string> All =
            [Customer, Doctor, Nurse, Manager, Admin];
    }

    public static readonly IReadOnlyList<SettingSeed> AdminMenus =
    [
        new(SettingType.AdminMenu, "Users", "/Admin/Users", "Manage user accounts"),
        new(SettingType.AdminMenu, "Settings", "/Admin/Settings", "Manage application settings"),
        new(SettingType.AdminMenu, "Authorization", "/Admin/Authorization", "Assign menus to roles"),
        new(SettingType.AdminMenu, "Posts", "/Admin/Posts", "Manage blog posts"),
        new(SettingType.AdminMenu, "Sliders", "/Admin/Sliders", "Manage home sliders"),
        new(SettingType.AdminMenu, "Services", "/Admin/Services", "Manage health care services")
    ];

    public static readonly IReadOnlyList<SettingSeed> SampleCategories =
    [
        new(SettingType.PostCategory, "Child Health", "child-health", "Child health articles"),
        new(SettingType.PostCategory, "Parenting", "parenting", "Parenting articles"),
        new(SettingType.ServiceCategory, "General Care", "general-care", "General care services"),
        new(SettingType.ServiceCategory, "Specialist Care", "specialist-care", "Specialist care services")
    ];
}

public sealed record SettingSeed(SettingType Type, string Name, string Value, string? Description);
