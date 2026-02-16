using osu.Shared;
using Sunrise.Shared.Extensions.Performances;
using Sunrise.Tests.Services.Mock;

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
        // Arrange
        var performance = _mocker.Score.GetRandomPerformanceAttributes();
        performance.PerformancePoints = sourcePp;

        // Act
        var finalizedPerformance = performance.FinalizeForAkatsuki();

        // Assert
        Assert.Equal(expectedPp, finalizedPerformance.PerformancePoints);
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void FinalizeForAkatsuki_ShouldConvertInvalidValuesToZero(double sourcePp)
    {
        // Arrange
        var performance = _mocker.Score.GetRandomPerformanceAttributes();
        performance.PerformancePoints = sourcePp;

        // Act
        var finalizedPerformance = performance.FinalizeForAkatsuki();

        // Assert
        Assert.Equal(0, finalizedPerformance.PerformancePoints);
    }

    [Fact]
    public void NormalizeForPerformanceCalculation_ShouldAddDoubleTimeForNightcore()
    {
        // Arrange
        const Mods mods = Mods.Nightcore;

        // Act
        var normalizedMods = mods.NormalizeForPerformanceCalculation();

        // Assert
        Assert.True(normalizedMods.HasFlag(Mods.Nightcore));
        Assert.True(normalizedMods.HasFlag(Mods.DoubleTime));
    }
}
