namespace CRUD_ChildrenCare.Services.Security;

public interface ISecureTokenService
{
    string CreateToken();
    string HashToken(string token);
}
