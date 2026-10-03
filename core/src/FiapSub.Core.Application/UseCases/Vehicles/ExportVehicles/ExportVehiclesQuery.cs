namespace FiapSub.Core.Application.UseCases.Vehicles.ExportVehicles;

/// <summary>
/// Lets the sales service rebuild or repair its vehicle projection by pulling the catalogue,
/// which is how any push lost to a transient failure gets reconciled.
/// </summary>
public sealed record ExportVehiclesQuery(int Page = 1, int PageSize = 100)
{
    public const int MaxPageSize = 500;
}
