namespace Wiley.Apartments.Domain;

public enum OperatingCostCategory
{
    Utility = 0,
    Repair = 1,
    Replace = 2,
    CommonUpkeep = 3,
    /// <summary>Unit remodel / rehab. Always tagged to a unit for P/L and cost-by-unit reports.</summary>
    Renovation = 4
}
