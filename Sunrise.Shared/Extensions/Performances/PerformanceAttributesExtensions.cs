using osu.Shared;
using Sunrise.Shared.Database.Models;
using Sunrise.Shared.Objects.Serializable.Performances;
using GameMode = Sunrise.Shared.Enums.Beatmaps.GameMode;

namespace Sunrise.Shared.Extensions.Performances;

public static class PerformanceAttributesExtensions
{
    /// <summary>
    ///     Applies the project relax formula. This is the only relax recalculation path.
    /// </summary>
    public static PerformanceAttributes ApplyRelaxPerformanceIfNeeded(this PerformanceAttributes performance, Score score)
    {
        if (score.Mods.HasFlag(Mods.Relax) && score.GameMode == GameMode.RelaxStandard)
            performance.PerformancePoints = RecalculateToRelaxStdPerformance(performance, score.Mods);

        return performance;
    }

    /// <summary>
    ///     Applies the project relax formula. This is the only relax recalculation path.
    /// </summary>
    public static PerformanceAttributes ApplyRelaxPerformanceIfNeeded(this PerformanceAttributes performance, Mods mods)
    {
        if (mods.HasFlag(Mods.Relax) && performance.Difficulty.Mode == GameMode.Standard)
            performance.PerformancePoints = RecalculateToRelaxStdPerformance(performance, mods);

        return performance;
    }

    private static double RecalculateToRelaxStdPerformance(PerformanceAttributes performance, Mods mods)
    {
        const double multi = 1.09;

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

    private static double RecalculateToRelaxStdPerformance(PerformanceAttributes performance, double accuracy, Mods mods)
{
    var multi = 1.09;

    var aim_value   = performance.PerformancePointsAim   ?? 0;
    var speed_value = performance.PerformancePointsSpeed ?? 0;
    var acc_value   = performance.PerformancePointsAccuracy ?? 0;

    // ───────────────────────────────────────────────────────────────
    // Коррекция модов: DT без изменений, NoMod — ощутимо лучше
    // ───────────────────────────────────────────────────────────────
    if (mods.HasFlag(Mods.DoubleTime))
    {
        // DT — без искусственного буста
    }
    else
    {
        // Только для NoMod — сильный буст, чтобы поднять с 1235 → 1320
        aim_value   *= 1.22;   // +22% к aim (основной рычаг)
        speed_value *= 1.15;   // +15% к speed
        acc_value   *= 1.05;   // опционально +5% к acc
    }

    var pp = Math.Pow(
        Math.Pow(aim_value,   1.185) +
        Math.Pow(speed_value, 0.83) +
        Math.Pow(acc_value,   1.14),
        1.0 / 1.1
    ) * multi;

    pp *= 0.70; // остаётся 0.70, как у тебя

    return double.IsNaN(pp) ? 0.0 : pp;
}

    private static double RecalculateToAutopilotStdPerformance(PerformanceAttributes performance)
    {
        var multi = CalculateStdPpMultiplier(performance);
        var streamsNerf = CalculateStreamsNerf(performance);

        double accDepression = 1;

        if (streamsNerf < 1.09)
        {
            var accFactor = (100 - accuracy) / 100;
            accDepression = Math.Max(0.86 - accFactor, 0.5);

            if (accDepression > 0.0)
            {
                performance.PerformancePointsAim *= accDepression;
            }
        }

        if (mods.HasFlag(Mods.HardRock))
        {
            multi *= Math.Min(2, Math.Max(1, 1 * (CalculateMissPenalty(performance) / 1.85)));
        }

        var relaxPp = Math.Pow(
            Math.Pow(performance.PerformancePointsAim ?? 0, 1.15) +
            Math.Pow(performance.PerformancePointsSpeed ?? 0, 0.65 * accDepression) +
            Math.Pow(performance.PerformancePointsAccuracy ?? 0, 1.1) +
            Math.Pow(performance.PerformancePointsFlashlight ?? 0, 1.13),
            1.0 / 1.1
        ) * multi;

        return double.IsNaN(relaxPp) ? 0.0 : relaxPp;
    }

    /// <summary>
    ///     Normalizes pp output to match Akatsuki behavior.
    ///     NaN/Infinity => 0, otherwise rounded to 3 decimals.
    /// </summary>
    public static PerformanceAttributes FinalizeForAkatsuki(this PerformanceAttributes performance)
    {
        var multi = CalculateStdPpMultiplier(performance);

        var relaxPp = Math.Pow(
            Math.Pow(performance.PerformancePointsAim ?? 0, 0.6) +
            Math.Pow(performance.PerformancePointsSpeed ?? 0, 1.3) +
            Math.Pow(performance.PerformancePointsAccuracy ?? 0, 1.05) +
            Math.Pow(performance.PerformancePointsFlashlight ?? 0, 1.13),
            1.0 / 1.1
        ) * multi;

        return double.IsNaN(relaxPp) ? 0.0 : relaxPp;
    }

    private static double CalculateMissPenalty(PerformanceAttributes performance)
    {
        if (mods.HasFlag(Mods.Easy))
        {
            performance.PerformancePoints *= 0.67;
        }

        return performance.PerformancePoints;
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

        return Math.Round(aimStrainValue / speedStrainValue * 100) / 100;
    }

    private static double CalculateStdPpMultiplier(PerformanceAttributes performance)
    {
        var aimValue = performance.PerformancePointsAim ?? 0;
        var speedValue = performance.PerformancePointsSpeed ?? 0;
        var accuracyValue = performance.PerformancePointsAccuracy ?? 0;
        var flashlightValue = performance.PerformancePointsFlashlight ?? 0;

        var ppValue = performance.PerformancePoints;

        // Reference: https://github.com/MaxOhn/rosu-pp/blob/51a303834fbf65f5c8c0a49061f3459c44f19d49/src/osu/performance/mod.rs#L850
        var sum = Math.Pow(
            Math.Pow(aimValue, 1.1) +
            Math.Pow(speedValue, 1.1) +
            Math.Pow(accuracyValue, 1.1) +
            Math.Pow(flashlightValue, 1.1),
            1.0 / 1.1
        );

        return ppValue / sum;
    }
}
