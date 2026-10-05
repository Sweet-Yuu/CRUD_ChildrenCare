namespace CRUD_ChildrenCare.Services.Menus;

public interface IAdminMenuService
{
    Task<IReadOnlyList<AdminMenuItem>> GetForRoleAsync(string roleValue, CancellationToken cancellationToken);
}
