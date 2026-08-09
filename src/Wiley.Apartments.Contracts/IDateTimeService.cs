namespace Wiley.Apartments.Contracts;

public interface IDateTimeService
{
    DateTime UtcNow { get; }
    DateTime ToDisplayTime(DateTime utc);
    DateTime ToUtc(DateTime local);
}
