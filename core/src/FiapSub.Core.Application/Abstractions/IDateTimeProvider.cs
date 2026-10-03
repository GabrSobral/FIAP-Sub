namespace FiapSub.Core.Application.Abstractions;

/// <summary>The clock, injected so time-dependent behaviour stays testable.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
