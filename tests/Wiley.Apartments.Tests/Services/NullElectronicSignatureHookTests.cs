using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class NullElectronicSignatureHookTests
{
    [Fact]
    public async Task RequestSignatureAsync_ReturnsNotConfigured()
    {
        var hook = new NullElectronicSignatureHook();
        hook.IsConfigured.Should().BeFalse();

        var result = await hook.RequestSignatureAsync(
            Guid.NewGuid(),
            "leases/3/sample.pdf",
            "clerk@test");

        result.Status.Should().Be("NotConfigured");
        result.Message.Should().Contain("wet-ink");
        result.Message.Should().Contain("upload");
    }
}
