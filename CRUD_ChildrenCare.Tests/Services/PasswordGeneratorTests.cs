using CRUD_ChildrenCare.Services.Security;
using CRUD_ChildrenCare.Validation;

namespace CRUD_ChildrenCare.Tests.Services;

public sealed class PasswordGeneratorTests
{
    [Fact]
    public void Generate_ReturnsStrongSixteenCharacterPassword()
    {
        var generator = new PasswordGenerator();

        var password = generator.Generate();

        Assert.Equal(16, password.Length);
        Assert.True(AccountValidation.IsValidPassword(password));
        Assert.Contains(password, char.IsUpper);
        Assert.Contains(password, char.IsLower);
        Assert.Contains(password, char.IsDigit);
    }

    [Fact]
    public void Generate_ReturnsDifferentValues()
    {
        var generator = new PasswordGenerator();

        var passwords = Enumerable.Range(0, 20).Select(_ => generator.Generate()).ToHashSet();

        Assert.Equal(20, passwords.Count);
    }
}
