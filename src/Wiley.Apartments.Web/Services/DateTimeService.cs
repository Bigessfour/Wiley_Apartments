using Wiley.Apartments.Contracts;

namespace Wiley.Apartments.Web.Services;

public sealed class DateTimeService : IDateTimeService
{
    private static readonly TimeZoneInfo MountainTime =
        TimeZoneInfo.FindSystemTimeZoneById("America/Denver");

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime ToDisplayTime(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            MountainTime);

    public DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local, DateTimeKind.Unspecified),
            MountainTime);
}
