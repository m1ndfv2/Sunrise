using osu.Shared;
using Sunrise.Shared.Database.Models;
using Sunrise.Shared.Objects.Serializable.Performances;
using Sunrise.Shared.Utils.Calculators;
using GameMode = Sunrise.Shared.Enums.Beatmaps.GameMode;

namespace Sunrise.Shared.Extensions.Performances;

public static class PerformanceAttributesExtensions
{
    public static PerformanceAttributes ApplyNotStandardModRecalculationsIfNeeded(this PerformanceAttributes performance, Score score)
    {
        if (score.Mods.HasFlag(Mods.Relax) && score.GameMode == GameMode.RelaxStandard)
        {
            performance.PerformancePoints = RecalculateToRelaxStdPerformance(performance, score.Accuracy, score.Mods);
        }

        if (score.Mods.HasFlag(Mods.Relax) && score.GameMode == GameMode.RelaxCatchTheBeat)
        {
            performance.PerformancePoints = RecalculateToRelaxCtbPerformance(performance, score.Mods);
        }

        if (score.Mods.HasFlag(Mods.Relax2) && score.GameMode == GameMode.AutopilotStandard)
        {
            performance.PerformancePoints = RecalculateToAutopilotStdPerformance(performance);
        }

        return performance;
    }

    public static PerformanceAttributes ApplyNotStandardModRecalculationsIfNeeded(this PerformanceAttributes performance, double accuracy, Mods mods)
    {
        if (mods.HasFlag(Mods.Relax) && performance.Difficulty.Mode == GameMode.Standard)
        {
            performance.PerformancePoints = RecalculateToRelaxStdPerformance(performance, accuracy, mods);
        }

        if (mods.HasFlag(Mods.Relax) && performance.Difficulty.Mode == GameMode.CatchTheBeat)
        {
            performance.PerformancePoints = RecalculateToRelaxCtbPerformance(performance, mods);
        }

        if (mods.HasFlag(Mods.Relax2) && performance.Difficulty.Mode == GameMode.Standard)
        {
            performance.PerformancePoints = RecalculateToAutopilotStdPerformance(performance);
        }

        return performance;
    }

    private static double RecalculateToRelaxStdPerformance(PerformanceAttributes performance, double accuracy, Mods mods)
    {
        return RelaxPerformanceCalculator.CalculateRelaxStdPp(performance, accuracy, mods);
    }

    private static double RecalculateToAutopilotStdPerformance(PerformanceAttributes performance)
    {
        return RelaxPerformanceCalculator.CalculateAutopilotStdPp(performance);
    }

    private static double RecalculateToRelaxCtbPerformance(PerformanceAttributes performance, Mods mods)
    {
        return RelaxPerformanceCalculator.CalculateRelaxCatchPp(performance, mods);
    }

}

