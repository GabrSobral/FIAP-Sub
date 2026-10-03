using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.UseCases.Vehicles.ListVehicles;

/// <summary>
/// Operational listing of the catalogue. The customer-facing, price-ordered listings live in the
/// sales service, which is what the public traffic hits.
/// </summary>
public sealed record ListVehiclesQuery(VehicleStatus? Status, int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
}
