using osu.Shared;
using Sunrise.Shared.Database.Models;
using Sunrise.Shared.Objects.Serializable.Performances;
using GameMode = Sunrise.Shared.Enums.Beatmaps.GameMode;

namespace Sunrise.Shared.Extensions.Performances;

public static class PerformanceAttributesExtensions
{
    /// <summary>
    ///     Applies legacy relax-standard recalculation on top of calculator result.
    /// </summary>
    public static PerformanceAttributes ApplyLegacyRelaxStdRecalculationIfNeeded(this PerformanceAttributes performance, Score score)
    {
        if (score.Mods.HasFlag(Mods.Relax) && score.GameMode == GameMode.RelaxStandard)
            performance.PerformancePoints = RecalculateToRelaxStdPerformance(performance, score.Accuracy, score.Mods);

        return performance;
    }

    /// <summary>
    ///     Applies legacy relax-standard recalculation on top of calculator result.
    /// </summary>
    public static PerformanceAttributes ApplyLegacyRelaxStdRecalculationIfNeeded(this PerformanceAttributes performance, double accuracy, Mods mods)
    {
        if (mods.HasFlag(Mods.Relax) && performance.Difficulty.Mode == GameMode.Standard)
            performance.PerformancePoints = RecalculateToRelaxStdPerformance(performance, accuracy, mods);

        return performance;
    }

    /// <summary>
    ///     Legacy relax std pp formula from the old pp system, adapted to the new pipeline.
    /// </summary>
    private static double RecalculateToRelaxStdPerformance(PerformanceAttributes performance, double accuracy, Mods mods)
    {
        var multi = 1.09;

        var aimValue = performance.PerformancePointsAim ?? 0;
        var speedValue = performance.PerformancePointsSpeed ?? 0;
        var accValue = performance.PerformancePointsAccuracy ?? 0;

        if (!mods.HasFlag(Mods.DoubleTime))
        {
            aimValue *= 1.22;
            speedValue *= 1.15;
            accValue *= 1.05;
        }

        var pp = Math.Pow(
                     Math.Pow(aimValue, 1.185) +
                     Math.Pow(speedValue, 0.83) +
                     Math.Pow(accValue, 1.14),
                     1.0 / 1.1
                 ) * multi;

        pp *= 0.70;

        return double.IsFinite(pp) ? pp : 0;
    }

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
