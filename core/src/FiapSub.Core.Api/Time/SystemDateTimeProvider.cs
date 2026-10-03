using System.Diagnostics.CodeAnalysis;
using FiapSub.Core.Application.Abstractions;

namespace FiapSub.Core.Api.Time;

[ExcludeFromCodeCoverage(Justification = "Thin wrapper over the system clock; there is nothing to assert.")]
internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
