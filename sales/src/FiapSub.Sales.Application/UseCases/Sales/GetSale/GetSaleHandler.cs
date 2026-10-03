using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Contracts;
using FiapSub.Sales.Domain.Sales;

namespace FiapSub.Sales.Application.UseCases.Sales.GetSale;

public sealed class GetSaleHandler(ISaleRepository sales) : IUseCase<GetSaleQuery, SaleResponse>
{
    public async Task<Result<SaleResponse>> ExecuteAsync(GetSaleQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sale = await sales.GetByIdAsync(request.SaleId, cancellationToken);

        return sale is null
            ? Result.Failure<SaleResponse>(
                Error.NotFound(ErrorCodes.SaleNotFound, $"No sale with id {request.SaleId}."))
            : Result.Success(SaleResponse.From(sale));
    }
}
