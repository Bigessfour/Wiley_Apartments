using Wiley.Apartments.Web.Configuration;

namespace Wiley.Apartments.Tests.Configuration;

public sealed class PaymentPortalConfigurationTests
{
    [Fact]
    public void TryResolve_Empty_ReturnsErrorWithItContact()
    {
        PaymentPortalConfiguration.TryResolve("  ", out var url, out var error).Should().BeFalse();
        url.Should().BeEmpty();
        error.Should().Contain("IT");
    }

    [Fact]
    public void TryResolve_InvalidScheme_ReturnsError()
    {
        PaymentPortalConfiguration.TryResolve("ftp://example.com", out _, out var error).Should().BeFalse();
        error.Should().Contain("invalid");
    }

    [Fact]
    public void TryResolve_ValidHttps_Succeeds()
    {
        PaymentPortalConfiguration.TryResolve(
            "https://secure.paystar.io/town",
            out var url,
            out var error).Should().BeTrue();
        url.Should().Be("https://secure.paystar.io/town");
        error.Should().BeNull();
    }
}
