using CRUD_ChildrenCare.Services.Security;

namespace CRUD_ChildrenCare.Tests.Services;

public sealed class SecureTokenServiceTests
{
    [Fact]
    public void CreateToken_ReturnsDifferentHighEntropyValues()
    {
        var service = new SecureTokenService();

        var first = service.CreateToken();
        var second = service.CreateToken();

        Assert.NotEqual(first, second);
        Assert.True(first.Length >= 43);
        Assert.True(second.Length >= 43);
    }

    [Fact]
    public void HashToken_IsDeterministicAndDoesNotStoreTheRawToken()
    {
        var service = new SecureTokenService();
        var token = service.CreateToken();

        var firstHash = service.HashToken(token);
        var secondHash = service.HashToken(token);

        Assert.Equal(firstHash, secondHash);
        Assert.NotEqual(token, firstHash);
        Assert.Equal(64, firstHash.Length);
    }
}
