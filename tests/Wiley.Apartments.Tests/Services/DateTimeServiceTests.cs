using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class DateTimeServiceTests
{
    private readonly DateTimeService _service = new();

    [Fact]
    public void ToDisplayTime_ConvertsUtcToAmericaDenver()
    {
        var utc = new DateTime(2026, 1, 15, 19, 0, 0, DateTimeKind.Utc);

        var display = _service.ToDisplayTime(utc);

        display.Kind.Should().Be(DateTimeKind.Unspecified);
        display.Hour.Should().Be(12);
    }

    [Fact]
    public void ToUtc_ConvertsLocalMountainTimeToUtc()
    {
        var local = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

        var utc = _service.ToUtc(local);

        utc.Kind.Should().Be(DateTimeKind.Utc);
        utc.Hour.Should().Be(19);
    }

    [Fact]
    public void UtcNow_IsCloseToSystemUtc()
    {
        var before = DateTime.UtcNow;
        var actual = _service.UtcNow;
        var after = DateTime.UtcNow;

        actual.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
