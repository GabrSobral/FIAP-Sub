namespace FiapSub.Sales.Api.Contracts;

/// <summary>
/// Body of <c>POST /api/sales</c>: which vehicle, who is buying, and optionally the date to
/// record as the sale date. When omitted the sale date is the instant the payment is approved.
/// </summary>
public sealed record OpenSaleRequest(Guid VehicleId, string? Cpf, DateTimeOffset? SaleDate);

/// <summary>
/// Body of <c>PUT /api/internal/vehicles/{id}</c>, pushed by the catalogue service whenever one
/// of its vehicles changes.
/// </summary>
public sealed record VehicleProjectionRequest(
    Guid VehicleId,
    string? Brand,
    string? Model,
    int Year,
    string? Color,
    decimal Price,
    string? Currency,
    string? Status,
    long Version,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Body of <c>POST /api/internal/payments/result</c>, pushed by the catalogue service once the
/// payment provider's webhook has been applied there.
/// </summary>
public sealed record PaymentResultRequest(
    string? PaymentCode,
    string? Status,
    Guid VehicleId,
    DateTimeOffset OccurredOn);
