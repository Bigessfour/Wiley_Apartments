namespace Wiley.Apartments.Domain;

/// <summary>Another clerk saved this record first — reload and retry (spec edge case 1).</summary>
public sealed class ConcurrencyConflictException(string entityName) : InvalidOperationException(
        $"{entityName} was changed by another clerk. Reload the page and try again so you do not overwrite their work.")
{
}
