using CRUD_ChildrenCare.Options;
using Microsoft.Extensions.Options;

namespace CRUD_ChildrenCare.Services.Files;

public sealed class LocalAvatarStorage(
    IWebHostEnvironment environment,
    IOptions<AvatarOptions> options) : IAvatarStorage
{
    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

    private readonly AvatarOptions avatarOptions = options.Value;

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw new AvatarStorageException("Please select a non-empty avatar image.");
        }

        if (file.Length > avatarOptions.MaximumBytes)
        {
            throw new AvatarStorageException("The avatar must not exceed 2 MB.");
        }

        var originalFileName = file.FileName;
        if (string.IsNullOrWhiteSpace(originalFileName)
            || !string.Equals(Path.GetFileName(originalFileName), originalFileName, StringComparison.Ordinal)
            || originalFileName.Contains('/')
            || originalFileName.Contains('\\'))
        {
            throw new AvatarStorageException("The avatar file name is invalid.");
        }

        var extension = Path.GetExtension(originalFileName);
        if (!AllowedContentTypes.TryGetValue(extension, out var expectedContentType)
            || !string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new AvatarStorageException("Only JPG, PNG, and WebP avatar images are supported.");
        }

        await using var input = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await input.ReadAsync(header.AsMemory(), cancellationToken);
        if (!HasValidSignature(extension, header.AsSpan(0, bytesRead)))
        {
            throw new AvatarStorageException("The selected file is not a valid image.");
        }

        input.Position = 0;
        var normalizedExtension = extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? ".jpg" : extension.ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{normalizedExtension}";
        var uploadRoot = GetUploadRoot();
        Directory.CreateDirectory(uploadRoot);
        var storedPath = Path.Combine(uploadRoot, storedFileName);

        await using var output = new FileStream(storedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await input.CopyToAsync(output, cancellationToken);

        return $"/{avatarOptions.UploadDirectory.Trim('/').Replace('\\', '/')}/{storedFileName}";
    }

    public Task DeleteAsync(string? webPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(webPath))
        {
            return Task.CompletedTask;
        }

        var expectedPrefix = $"/{avatarOptions.UploadDirectory.Trim('/').Replace('\\', '/')}/";
        if (!webPath.StartsWith(expectedPrefix, StringComparison.Ordinal)
            || webPath.Contains("..", StringComparison.Ordinal)
            || !string.Equals(Path.GetFileName(webPath), webPath[expectedPrefix.Length..], StringComparison.Ordinal))
        {
            throw new AvatarStorageException("The avatar path is outside the managed upload directory.");
        }

        var uploadRoot = GetUploadRoot();
        var filePath = Path.GetFullPath(Path.Combine(uploadRoot, Path.GetFileName(webPath)));
        if (!filePath.StartsWith(uploadRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new AvatarStorageException("The avatar path is outside the managed upload directory.");
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private string GetUploadRoot()
    {
        var relativeDirectory = avatarOptions.UploadDirectory.Trim().Trim('/', '\\');
        if (string.IsNullOrWhiteSpace(relativeDirectory)
            || Path.IsPathRooted(relativeDirectory)
            || relativeDirectory.Split('/', '\\').Any(segment => segment is "." or ".."))
        {
            throw new AvatarStorageException("The avatar upload directory is invalid.");
        }

        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        var normalizedWebRoot = Path.GetFullPath(webRoot);
        var uploadRoot = Path.GetFullPath(Path.Combine(normalizedWebRoot, relativeDirectory));
        if (!uploadRoot.StartsWith(normalizedWebRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new AvatarStorageException("The avatar upload directory is invalid.");
        }

        return uploadRoot;
    }

    private static bool HasValidSignature(string extension, ReadOnlySpan<byte> header) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => header.Length >= 3
            && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
        ".png" => header.Length >= 8
            && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
        ".webp" => header.Length >= 12
            && header[..4].SequenceEqual("RIFF"u8)
            && header[8..12].SequenceEqual("WEBP"u8),
        _ => false
    };
}
