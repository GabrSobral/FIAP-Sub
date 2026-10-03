namespace FiapSub.Core.Api.Contracts;

/// <summary>Body of <c>POST /api/vehicles</c>.</summary>
public sealed record RegisterVehicleRequest(
    string? Brand,
    string? Model,
    int Year,
    string? Color,
    decimal Price);

/// <summary>Body of <c>PUT /api/vehicles/{id}</c>. The id comes from the route.</summary>
public sealed record UpdateVehicleRequest(
    string? Brand,
    string? Model,
    int Year,
    string? Color,
    decimal Price);

/// <summary>Body of the internal reserve and release calls.</summary>
public sealed record PaymentCodeRequest(string? PaymentCode);
