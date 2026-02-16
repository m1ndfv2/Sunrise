using osu.Shared;
using Sunrise.Shared.Database.Models;
using Sunrise.Shared.Extensions.Performances;
using Sunrise.Tests.Services.Mock;
using GameMode = Sunrise.Shared.Enums.Beatmaps.GameMode;

namespace Sunrise.Server.Tests.Extensions;

public class PerformanceAttributesExtensionsTests
{
    private readonly MockService _mocker = new();

    [Theory]
    [InlineData(123.4567, 123.457)]
    [InlineData(123.4561, 123.456)]
    [InlineData(0, 0)]
    public void FinalizeForAkatsuki_ShouldRoundPpToThreeDecimals(double sourcePp, double expectedPp)
    {
        var performance = _mocker.Score.GetRandomPerformanceAttributes();
        performance.PerformancePoints = sourcePp;

        var finalizedPerformance = performance.FinalizeForAkatsuki();

        Assert.Equal(expectedPp, finalizedPerformance.PerformancePoints);
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void FinalizeForAkatsuki_ShouldConvertInvalidValuesToZero(double sourcePp)
    {
        var performance = _mocker.Score.GetRandomPerformanceAttributes();
        performance.PerformancePoints = sourcePp;

        var finalizedPerformance = performance.FinalizeForAkatsuki();

        Assert.Equal(0, finalizedPerformance.PerformancePoints);
    }

    [Fact]
    public void NormalizeForPerformanceCalculation_ShouldAddDoubleTimeForNightcore()
    {
        const Mods mods = Mods.Nightcore;

        var normalizedMods = mods.NormalizeForPerformanceCalculation();

        Assert.True(normalizedMods.HasFlag(Mods.Nightcore));
        Assert.True(normalizedMods.HasFlag(Mods.DoubleTime));
    }

    [Fact]
    public void ApplyRelaxPerformanceIfNeeded_ShouldApplyFormulaForRelaxStandard()
    {
        var performance = _mocker.Score.GetRandomPerformanceAttributes();
        performance.Difficulty.Mode = GameMode.Standard;
        performance.PerformancePointsAim = 1000;
        performance.PerformancePointsSpeed = 800;
        performance.PerformancePointsAccuracy = 500;

        var result = performance.ApplyRelaxPerformanceIfNeeded(Mods.Relax);

        Assert.NotEqual(0, result.PerformancePoints);
        Assert.True(result.PerformancePoints > 0);
    }

    [Fact]
    public void ApplyRelaxPerformanceIfNeeded_ShouldNotChangeForNonRelax()
    {
        var score = _mocker.Score.GetRandomScore();
        score.GameMode = GameMode.Standard;
        score.Mods = Mods.Hidden;

        var performance = _mocker.Score.GetRandomPerformanceAttributes();
        performance.PerformancePoints = 321.123;

        var result = performance.ApplyRelaxPerformanceIfNeeded(score);

        Assert.Equal(321.123, result.PerformancePoints);
    }
}
