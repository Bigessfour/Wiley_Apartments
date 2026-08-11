namespace Wiley.Apartments.Domain;

public enum LeaseStatus
{
    Draft = 0,
    Active = 1,
    Expired = 2,
    Terminated = 3,
    Amended = 4,
    /// <summary>Prior lease superseded by a renewal (successor lease created).</summary>
    Renewed = 5
}
