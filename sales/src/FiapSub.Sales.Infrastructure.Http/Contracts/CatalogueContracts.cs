namespace FiapSub.Sales.Infrastructure.Http.Contracts;

/// <summary>Body of the catalogue's reserve and release calls.</summary>
internal sealed record PaymentCodePayload(string PaymentCode);

/// <summary>One page of the catalogue's export endpoint.</summary>
internal sealed record CataloguePage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>A vehicle as the catalogue serialises it.</summary>
internal sealed record CatalogueVehiclePayload(
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
