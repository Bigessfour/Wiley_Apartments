namespace Wiley.Apartments.Domain;

/// <summary>Rentable Community Center rooms. Whole building occupies every room.</summary>
public enum FacilitySpace
{
    FireplaceRoom = 0,
    Kitchen = 1,
    MainHall = 2,
    WholeBuilding = 3
}

public static class FacilitySpaceInfo
{
    public static string DisplayName(FacilitySpace space) => space switch
    {
        FacilitySpace.FireplaceRoom => "Fireplace Room",
        FacilitySpace.Kitchen => "Kitchen",
        FacilitySpace.MainHall => "Main Space (Hall)",
        FacilitySpace.WholeBuilding => "Entire Facility",
        _ => space.ToString()
    };

    /// <summary>Short label for calendar blocks: Hall and Entire Facility must be obvious at a glance.</summary>
    public static string CalendarLabel(FacilitySpace space) => space switch
    {
        FacilitySpace.FireplaceRoom => "Fireplace Room",
        FacilitySpace.Kitchen => "Kitchen",
        FacilitySpace.MainHall => "Hall",
        FacilitySpace.WholeBuilding => "Entire Facility",
        _ => space.ToString()
    };

    public static string CalendarCss(FacilitySpace space) => space switch
    {
        FacilitySpace.FireplaceRoom => "cs-sched-cc-fireplace",
        FacilitySpace.Kitchen => "cs-sched-cc-kitchen",
        FacilitySpace.MainHall => "cs-sched-cc-hall",
        FacilitySpace.WholeBuilding => "cs-sched-cc-entire",
        _ => "cs-sched-facility-rental"
    };

    /// <summary>
    /// Same room conflicts. Whole building conflicts with every room.
    /// Kitchen and Hall at the same time do not conflict.
    /// </summary>
    public static bool Conflicts(FacilitySpace a, FacilitySpace b) =>
        a == FacilitySpace.WholeBuilding
        || b == FacilitySpace.WholeBuilding
        || a == b;
}
