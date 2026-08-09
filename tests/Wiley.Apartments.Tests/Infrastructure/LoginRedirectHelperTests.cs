using Wiley.Apartments.Web.Infrastructure;

namespace Wiley.Apartments.Tests.Infrastructure;

public class LoginRedirectHelperTests
{
    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("   ", "/")]
    [InlineData("/", "/")]
    [InlineData("/units", "/units")]
    [InlineData("/units/abc-123", "/units/abc-123")]
    [InlineData("/settings?tab=appearance", "/settings?tab=appearance")]
    public void GetSafeReturnUrl_AllowsLocalPaths(string? input, string expected) =>
        LoginRedirectHelper.GetSafeReturnUrl(input).Should().Be(expected);

    [Theory]
    [InlineData("https://evil.com")]
    [InlineData("http://evil.com/path")]
    [InlineData("//evil.com")]
    [InlineData("/\\evil.com")]
    [InlineData("\\evil.com")]
    [InlineData("units")]
    public void GetSafeReturnUrl_BlocksExternalOrUnsafeUrls(string input) =>
        LoginRedirectHelper.GetSafeReturnUrl(input).Should().Be("/");

    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("units", "/units")]
    [InlineData("units/abc-123", "/units/abc-123")]
    [InlineData("/units", "/units")]
    [InlineData("settings?tab=appearance", "/settings?tab=appearance")]
    public void FromBaseRelativePath_PrefixesSlashThenValidates(string? input, string expected) =>
        LoginRedirectHelper.FromBaseRelativePath(input).Should().Be(expected);

    [Theory]
    [InlineData("https://evil.com")]
    [InlineData("//evil.com")]
    [InlineData("/\\evil.com")]
    public void FromBaseRelativePath_BlocksUnsafeUrls(string input) =>
        LoginRedirectHelper.FromBaseRelativePath(input).Should().Be("/");
}
