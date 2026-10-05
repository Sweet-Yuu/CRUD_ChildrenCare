using System.Net.Mail;
using System.Text.RegularExpressions;

namespace CRUD_ChildrenCare.Validation;

public static partial class AccountValidation
{
    public const int MinimumPasswordLength = 8;
    public const int MaximumPasswordLength = 50;

    public static string NormalizeFullName(string value) =>
        WhitespaceRegex().Replace(value.Trim(), " ");

    public static string NormalizeEmail(string value) => value.Trim().ToUpperInvariant();

    public static bool IsValidFullName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = NormalizeFullName(value);
        return normalized.Length is >= 2 and <= 100 && FullNameRegex().IsMatch(normalized);
    }

    public static bool IsValidEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        return normalized.Length <= 100
            && !normalized.Contains(' ')
            && MailAddress.TryCreate(normalized, out var address)
            && string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValidMobile(string? value) =>
        !string.IsNullOrWhiteSpace(value) && MobileRegex().IsMatch(value);

    public static bool IsValidPassword(string? value) =>
        value is not null
        && value.Length is >= MinimumPasswordLength and <= MaximumPasswordLength
        && value.Any(char.IsUpper)
        && value.Any(char.IsLower)
        && value.Any(char.IsDigit);

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^\p{L}+(?: \p{L}+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex FullNameRegex();

    [GeneratedRegex(@"^0\d{9}$", RegexOptions.CultureInvariant)]
    private static partial Regex MobileRegex();
}
