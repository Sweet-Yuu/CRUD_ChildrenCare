using Microsoft.AspNetCore.Http;

namespace CRUD_ChildrenCare.Services.Files;

public interface IAvatarStorage
{
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken);
    Task DeleteAsync(string? webPath, CancellationToken cancellationToken);
}
