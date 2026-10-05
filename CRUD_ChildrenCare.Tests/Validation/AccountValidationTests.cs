using CRUD_ChildrenCare.Validation;

namespace CRUD_ChildrenCare.Tests.Validation;

public sealed class AccountValidationTests
{
    [Fact]
    public void NormalizeFullName_TrimsOuterWhitespaceAndCollapsesInnerWhitespace()
    {
        var normalized = AccountValidation.NormalizeFullName("  Nguyễn   Văn  An  ");

        Assert.Equal("Nguyễn Văn An", normalized);
        Assert.True(AccountValidation.IsValidFullName(normalized));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("John-Connor")]
    [InlineData("John 2")]
    [InlineData("")]
    public void IsValidFullName_RejectsValuesOutsideTheNameRule(string value)
    {
        Assert.False(AccountValidation.IsValidFullName(value));
    }

    [Theory]
    [InlineData("0123456789", true)]
    [InlineData("1123456789", false)]
    [InlineData("012345678", false)]
    [InlineData("01234567890", false)]
    [InlineData("01234A6789", false)]
    public void IsValidMobile_RequiresTenDigitsBeginningWithZero(string value, bool expected)
    {
        Assert.Equal(expected, AccountValidation.IsValidMobile(value));
    }

    [Theory]
    [InlineData("StrongPass1", true)]
    [InlineData("short1A", false)]
    [InlineData("alllowercase1", false)]
    [InlineData("ALLUPPERCASE1", false)]
    [InlineData("NoDigitsHere", false)]
    public void IsValidPassword_EnforcesLengthAndCharacterClasses(string value, bool expected)
    {
        Assert.Equal(expected, AccountValidation.IsValidPassword(value));
    }

    [Theory]
    [InlineData("person@example.com", true)]
    [InlineData("not-an-email", false)]
    [InlineData("person@", false)]
    [InlineData("@example.com", false)]
    public void IsValidEmail_RejectsMalformedValues(string value, bool expected)
    {
        Assert.Equal(expected, AccountValidation.IsValidEmail(value));
    }
}
