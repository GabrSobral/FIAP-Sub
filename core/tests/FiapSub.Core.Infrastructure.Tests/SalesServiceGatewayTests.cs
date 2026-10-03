using System.Net;
using FiapSub.Core.Application.Gateways;
using FiapSub.Core.Infrastructure.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapSub.Core.Infrastructure.Tests;

public class SalesServiceGatewayTests
{
    private static readonly VehicleProjectionMessage Projection = new(
        Guid.CreateVersion7(),
        "Fiat",
        "Argo Drive",
        2023,
        "Vermelho",
        78_900.00m,
        "BRL",
        "Available",
        Version: 1,
        UpdatedAt: new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero));

    private static readonly PaymentResultMessage PaymentResult = new(
        "PAY-0001",
        PaymentResultMessage.Approved,
        Projection.VehicleId,
        new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero));

    private static (ISalesServiceGateway Gateway, RecordingHandler Handler, HttpClient Client) Build(
        HttpStatusCode statusCode = HttpStatusCode.NoContent)
    {
        var handler = new RecordingHandler(statusCode);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://sales-api.test/") };
        var gateway = new SalesServiceGateway(client, NullLogger<SalesServiceGateway>.Instance);

        return (gateway, handler, client);
    }

    [Fact]
    public async Task PublishVehicleAsync_PutsTheProjectionOnTheVehicleRoute()
    {
        var (gateway, handler, client) = Build();
        using var _ = client;

        await gateway.PublishVehicleAsync(Projection, TestContext.Current.CancellationToken);

        var request = handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Put);
        request.RequestUri!.AbsolutePath.ShouldBe($"/api/internal/vehicles/{Projection.VehicleId}");
        handler.Bodies.ShouldHaveSingleItem().ShouldContain("\"brand\":\"Fiat\"");
    }

    [Fact]
    public async Task PublishPaymentResultAsync_PostsToThePaymentResultRoute()
    {
        var (gateway, handler, client) = Build();
        using var _ = client;

        await gateway.PublishPaymentResultAsync(PaymentResult, TestContext.Current.CancellationToken);

        var request = handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Post);
        request.RequestUri!.AbsolutePath.ShouldBe("/api/internal/payments/result");
        handler.Bodies.ShouldHaveSingleItem().ShouldContain("\"status\":\"approved\"");
    }

    [Fact]
    public async Task PublishVehicleAsync_FailsLoudlyOnANonSuccessResponse()
    {
        var (gateway, _, client) = Build(HttpStatusCode.InternalServerError);
        using var _unused = client;

        await Should.ThrowAsync<HttpRequestException>(
            () => gateway.PublishVehicleAsync(Projection, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PublishPaymentResultAsync_FailsLoudlyOnANonSuccessResponse()
    {
        var (gateway, _, client) = Build(HttpStatusCode.BadGateway);
        using var _unused = client;

        await Should.ThrowAsync<HttpRequestException>(
            () => gateway.PublishPaymentResultAsync(PaymentResult, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task NullMessagesAreRejected()
    {
        var (gateway, _, client) = Build();
        using var _unused = client;

        await Should.ThrowAsync<ArgumentNullException>(
            () => gateway.PublishVehicleAsync(null!, TestContext.Current.CancellationToken));
        await Should.ThrowAsync<ArgumentNullException>(
            () => gateway.PublishPaymentResultAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void OptionsCarryTheInternalTokenHeaderName()
    {
        SalesServiceOptions.InternalTokenHeader.ShouldBe("X-Internal-Token");
        new SalesServiceOptions().RetryCount.ShouldBe(3);
    }
}
