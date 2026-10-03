using FiapSub.Core.Application.Abstractions;

namespace FiapSub.Core.Application.Tests.TestDoubles;

internal sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
{
    public static readonly DateTimeOffset Default = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);

    public DateTimeOffset UtcNow { get; set; } = now;

    public static FixedClock AtDefault() => new(Default);
}
