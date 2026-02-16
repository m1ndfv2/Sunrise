using Sunrise.Shared.Objects.Serializable.Performances;

namespace Sunrise.Shared.Extensions.Performances;

public static class PerformanceAttributesExtensions
{
    /// <summary>
    ///     Normalizes pp output to match Akatsuki behavior.
    ///     NaN/Infinity => 0, otherwise rounded to 3 decimals.
    /// </summary>
    public static PerformanceAttributes FinalizeForAkatsuki(this PerformanceAttributes performance)
    {
        performance.PerformancePoints = !double.IsFinite(performance.PerformancePoints)
            ? 0
            : Math.Round(performance.PerformancePoints, 3);

        return performance;
    }
}
