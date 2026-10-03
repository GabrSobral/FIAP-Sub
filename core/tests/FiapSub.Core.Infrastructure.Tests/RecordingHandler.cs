using System.Net;

namespace FiapSub.Core.Infrastructure.Tests;

/// <summary>
/// Captures the requests a typed client sends and replies with a canned status, so the gateway's
/// wire contract can be asserted without a server.
/// </summary>
internal sealed class RecordingHandler(HttpStatusCode statusCode = HttpStatusCode.NoContent) : HttpMessageHandler
{
    internal List<HttpRequestMessage> Requests { get; } = [];

    internal List<string> Bodies { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        return new HttpResponseMessage(statusCode) { RequestMessage = request };
    }
}
