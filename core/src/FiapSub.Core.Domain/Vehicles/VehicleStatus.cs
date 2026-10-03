namespace FiapSub.Core.Domain.Vehicles;

/// <summary>Where a vehicle stands in its selling lifecycle.</summary>
public enum VehicleStatus
{
    /// <summary>On the shelf and listable.</summary>
    Available = 1,

    /// <summary>A buyer started a purchase; the payment has not settled yet.</summary>
    Reserved = 2,

    /// <summary>Payment approved. Terminal state.</summary>
    Sold = 3,
}
