using CRUD_ChildrenCare.Options;
using CRUD_ChildrenCare.Services.Files;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace CRUD_ChildrenCare.Tests.Services;

public sealed class LocalAvatarStorageTests : IAsyncLifetime
{
    private readonly string contentRoot = Path.Combine(Path.GetTempPath(), $"children-care-{Guid.NewGuid():N}");
    private LocalAvatarStorage storage = null!;

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(contentRoot);
        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = contentRoot,
            WebRootPath = Path.Combine(contentRoot, "wwwroot")
        };
        storage = new LocalAvatarStorage(
            environment,
            Microsoft.Extensions.Options.Options.Create(new AvatarOptions()));
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(contentRoot))
        {
            Directory.Delete(contentRoot, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(ValidImages))]
    public async Task SaveAsync_StoresValidImageWithRandomName(
        string fileName,
        string contentType,
        byte[] content)
    {
        var file = CreateFile(fileName, contentType, content);

        var webPath = await storage.SaveAsync(file, CancellationToken.None);

        Assert.StartsWith("/uploads/avatars/", webPath, StringComparison.Ordinal);
        Assert.EndsWith(Path.GetExtension(fileName), webPath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            Path.GetFileNameWithoutExtension(fileName),
            Path.GetFileName(webPath),
            StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(contentRoot, "wwwroot", webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task SaveAsync_RejectsEmptyAndOversizedFiles()
    {
        await Assert.ThrowsAsync<AvatarStorageException>(() =>
            storage.SaveAsync(CreateFile("empty.png", "image/png", []), CancellationToken.None));
        await Assert.ThrowsAsync<AvatarStorageException>(() =>
            storage.SaveAsync(
                CreateFile("large.png", "image/png", new byte[AvatarOptions.DefaultMaximumBytes + 1]),
                CancellationToken.None));
    }

    [Theory]
    [InlineData("avatar.gif", "image/gif")]
    [InlineData("../avatar.png", "image/png")]
    [InlineData("folder/avatar.jpg", "image/jpeg")]
    public async Task SaveAsync_RejectsUnsupportedOrTraversalStyleNames(string fileName, string contentType)
    {
        await Assert.ThrowsAsync<AvatarStorageException>(() =>
            storage.SaveAsync(CreateFile(fileName, contentType, [1, 2, 3, 4, 5, 6, 7, 8]), CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_RejectsForgedImageContent()
    {
        await Assert.ThrowsAsync<AvatarStorageException>(() =>
            storage.SaveAsync(
                CreateFile("avatar.png", "image/png", "not a real png"u8.ToArray()),
                CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyFilesOwnedByAvatarStorage()
    {
        var webPath = await storage.SaveAsync(
            CreateFile("avatar.png", "image/png", PngBytes()),
            CancellationToken.None);
        var storedPath = Path.Combine(contentRoot, "wwwroot", webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        await storage.DeleteAsync(webPath, CancellationToken.None);

        Assert.False(File.Exists(storedPath));
        await Assert.ThrowsAsync<AvatarStorageException>(() =>
            storage.DeleteAsync("/../outside.png", CancellationToken.None));
    }

    public static IEnumerable<object[]> ValidImages()
    {
        yield return ["avatar.jpg", "image/jpeg", JpegBytes()];
        yield return ["avatar.png", "image/png", PngBytes()];
        yield return ["avatar.webp", "image/webp", WebPBytes()];
    }

    private static FormFile CreateFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "Avatar", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static byte[] JpegBytes() => [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0];
    private static byte[] PngBytes() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static byte[] WebPBytes() => [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50];

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "CRUD_ChildrenCare.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
