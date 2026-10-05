namespace CRUD_ChildrenCare.Options;

public sealed class AvatarOptions
{
    public const string SectionName = "Avatar";
    public const int DefaultMaximumBytes = 2 * 1024 * 1024;

    public string UploadDirectory { get; set; } = "uploads/avatars";
    public int MaximumBytes { get; set; } = DefaultMaximumBytes;
}
