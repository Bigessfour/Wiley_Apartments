namespace Wiley.Apartments.Contracts;

/// <summary>
/// How operational ledger views treat occupancy.
/// Former tenants and their charges stay on file; they are only hidden from current operations.
/// </summary>
public enum OccupancyFilter
{
    /// <summary>Open occupancy (or unit.CurrentTenantId roster fallback).</summary>
    Current,

    /// <summary>Ended occupancy only — collections / archive.</summary>
    Former,

    /// <summary>Every tenant/unit pair. Used when a clerk opens one specific account.</summary>
    All
}
