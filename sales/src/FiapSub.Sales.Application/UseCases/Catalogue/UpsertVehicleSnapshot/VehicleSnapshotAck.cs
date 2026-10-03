namespace FiapSub.Sales.Application.UseCases.Catalogue.UpsertVehicleSnapshot;

/// <param name="Applied">
/// False when the push was older than what we already hold, i.e. the call was a replay or arrived
/// out of order and was correctly ignored.
/// </param>
public sealed record VehicleSnapshotAck(Guid VehicleId, long Version, bool Applied, string Status);
