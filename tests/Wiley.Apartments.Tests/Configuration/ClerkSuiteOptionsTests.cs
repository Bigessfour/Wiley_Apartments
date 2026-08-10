using Microsoft.Extensions.Options;
using Wiley.Apartments.Web.Configuration;

namespace Wiley.Apartments.Tests.Configuration;

public class ClerkSuiteOptionsTests
{
    [Fact]
    public void Defaults_MatchLockedPlanningDecisions()
    {
        var options = new ClerkSuiteOptions();

        options.DatabaseProvider.Should().Be("Sqlite");
        options.LateFeesEnabled.Should().BeFalse();
        options.MaxUnits.Should().Be(0);
        options.PaymentPortalUrl.Should().Contain("townofwiley.gov");
    }

    [Fact]
    public void SectionName_IsClerkSuite()
    {
        ClerkSuiteOptions.SectionName.Should().Be("ClerkSuite");
    }
}
