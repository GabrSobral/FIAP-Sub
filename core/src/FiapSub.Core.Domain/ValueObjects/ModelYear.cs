using System.Globalization;
using FiapSub.Core.Domain.Exceptions;

namespace FiapSub.Core.Domain.ValueObjects;

/// <summary>
/// The model year printed on a vehicle's documents. Bounded below by the dawn of the
/// automobile and above by next year's models, which are already sold today.
/// </summary>
public readonly record struct ModelYear
{
    public const int Earliest = 1900;

    private ModelYear(int value) => Value = value;

    public int Value { get; }

    /// <param name="latestAllowed">
    /// Usually <c>now.Year + 1</c>, supplied by the caller so the domain stays free of ambient time.
    /// </param>
    public static ModelYear Create(int value, int latestAllowed)
    {
        if (value < Earliest || value > latestAllowed)
        {
            throw new DomainValidationException($"Model year must be between {Earliest} and {latestAllowed} (got {value}).");
        }

        return new ModelYear(value);
    }

    /// <summary>
    /// Rebuilds a model year from storage without re-checking the upper bound: the value was
    /// already validated when it was written, and the bound moves as years pass.
    /// </summary>
    public static ModelYear Restore(int value) => new(value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
