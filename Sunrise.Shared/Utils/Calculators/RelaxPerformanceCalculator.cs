using Sunrise.Shared.Objects.Serializable.Performances;
using Mods = osu.Shared.Mods;

namespace Sunrise.Shared.Utils.Calculators;

/// <summary>
///     Recalculates pp for non-standard mods using the legacy Akatsuki-style formulas.
/// </summary>
public static class RelaxPerformanceCalculator
{
    public static double CalculateRelaxStdPp(PerformanceAttributes performance, double accuracy, Mods mods)
    {
        var multiplier = CalculateStdBaseMultiplier(performance);
        var streamsNerf = CalculateStreamsNerf(performance);

        var accDepression = 1.0;

        if (streamsNerf < 1.09)
        {
            var accFactor = (100 - accuracy) / 100;
            accDepression = Math.Max(0.86 - accFactor, 0.5);
        }

        var aim = (performance.PerformancePointsAim ?? 0) * accDepression;
        var speed = performance.PerformancePointsSpeed ?? 0;
        var acc = performance.PerformancePointsAccuracy ?? 0;
        var flashlight = performance.PerformancePointsFlashlight ?? 0;

        if (mods.HasFlag(Mods.HardRock))
        {
            multiplier *= Math.Min(2, Math.Max(1, 1 * (CalculateMissPenalty(performance) / 1.85)));
        }

        var pp = Math.Pow(
            Math.Pow(aim, 1.15) +
            Math.Pow(speed, 0.65 * accDepression) +
            Math.Pow(acc, 1.1) +
            Math.Pow(flashlight, 1.13),
            1.0 / 1.1
        ) * multiplier;

        return double.IsFinite(pp) ? Math.Max(pp, 0) : 0;
    }

    public static double CalculateRelaxCatchPp(PerformanceAttributes performance, Mods mods)
    {
        var pp = performance.PerformancePoints;

        if (mods.HasFlag(Mods.Easy))
        {
            pp *= 0.67;
        }

        return double.IsFinite(pp) ? Math.Max(pp, 0) : 0;
    }

    public static double CalculateAutopilotStdPp(PerformanceAttributes performance)
    {
        var multiplier = CalculateStdBaseMultiplier(performance);

        var aim = performance.PerformancePointsAim ?? 0;
        var speed = performance.PerformancePointsSpeed ?? 0;
        var acc = performance.PerformancePointsAccuracy ?? 0;
        var flashlight = performance.PerformancePointsFlashlight ?? 0;

        var pp = Math.Pow(
            Math.Pow(aim, 0.6) +
            Math.Pow(speed, 1.3) +
            Math.Pow(acc, 1.05) +
            Math.Pow(flashlight, 1.13),
            1.0 / 1.1
        ) * multiplier;

        return double.IsFinite(pp) ? Math.Max(pp, 0) : 0;
    }

    private static double CalculateMissPenalty(PerformanceAttributes performance)
    {
        var missCount = performance.State.Misses ?? 0;
        var diffStrainCount = performance.Difficulty.Aim ?? 0;

        if (diffStrainCount <= 0)
            return 0;

        var logValue = Math.Log(diffStrainCount);
        var denominatorPart = 4.0 * Math.Pow(logValue, 0.94);

        if (double.IsNaN(denominatorPart) || double.IsInfinity(denominatorPart))
            return 0;

        return 2.0 / (missCount / denominatorPart + 1.0);
    }

    private static double CalculateStreamsNerf(PerformanceAttributes performance)
    {
        var aimStrainValue = performance.Difficulty.AimDifficultStrainCount ?? 0;
        var speedStrainValue = performance.Difficulty.SpeedDifficultStrainCount ?? 0;

        if (speedStrainValue <= 0) return 1;

        return Math.Round(aimStrainValue / speedStrainValue * 100) / 100;
    }

    private static double CalculateStdBaseMultiplier(PerformanceAttributes performance)
    {
        var aimValue = performance.PerformancePointsAim ?? 0;
        var speedValue = performance.PerformancePointsSpeed ?? 0;
        var accuracyValue = performance.PerformancePointsAccuracy ?? 0;
        var flashlightValue = performance.PerformancePointsFlashlight ?? 0;
        var ppValue = performance.PerformancePoints;

        var sum = Math.Pow(
            Math.Pow(aimValue, 1.1) +
            Math.Pow(speedValue, 1.1) +
            Math.Pow(accuracyValue, 1.1) +
            Math.Pow(flashlightValue, 1.1),
            1.0 / 1.1
        );

        if (sum <= 0) return 0;

        var multiplier = ppValue / sum;

        return double.IsFinite(multiplier) ? Math.Max(multiplier, 0) : 0;
    }
}
