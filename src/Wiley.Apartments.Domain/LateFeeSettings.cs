namespace Wiley.Apartments.Domain;

/// <summary>Singleton portfolio late-fee rule (G2). Default disabled.</summary>
public class LateFeeSettings
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; }
    public decimal Amount { get; set; }
    public int GraceDays { get; set; }
}
