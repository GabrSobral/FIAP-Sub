namespace FiapSub.Sales.Domain.Catalog;

/// <summary>
/// Mirrors the catalogue service's vehicle status. It is replicated data, not a state machine
/// this service drives: only the catalogue may change it.
/// </summary>
public enum VehicleAvailability
{
    Available = 1,
    Reserved = 2,
    Sold = 3,
}
