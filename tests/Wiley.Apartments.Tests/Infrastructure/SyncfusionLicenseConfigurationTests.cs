using Microsoft.Extensions.Configuration;
using Wiley.Apartments.Web.Infrastructure;

namespace Wiley.Apartments.Tests.Infrastructure;

public class SyncfusionLicenseConfigurationTests
{
    [Fact]
    public void ResolveLicenseKey_ReturnsNull_WhenMissing()
    {
        var config = new ConfigurationBuilder().Build();

        SyncfusionLicenseConfiguration.ResolveLicenseKey(config).Should().BeNull();
    }

    [Fact]
    public void ResolveLicenseKey_ReadsSynfusionLicenseKeyEnvStyle()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SYNCFUSION_LICENSE_KEY"] = "  test-key  "
            })
            .Build();

        SyncfusionLicenseConfiguration.ResolveLicenseKey(config).Should().Be("test-key");
    }

    [Fact]
    public void ResolveLicenseKey_ReadsNestedSyncfusionSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Syncfusion:LicenseKey"] = "nested-key"
            })
            .Build();

        SyncfusionLicenseConfiguration.ResolveLicenseKey(config).Should().Be("nested-key");
    }
}
