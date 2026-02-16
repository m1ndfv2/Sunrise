using osu.Shared;

namespace Sunrise.Shared.Extensions.Performances;

public static class ModsExtensions
{
    /// <summary>
    ///     Normalizes mods to match Akatsuki behavior before requesting PP calculation.
    ///     In Akatsuki NC is treated as DT.
    /// </summary>
    public static Mods NormalizeForPerformanceCalculation(this Mods mods)
    {
        if (mods.HasFlag(Mods.Nightcore))
            mods |= Mods.DoubleTime;

        return mods;
    }
}
