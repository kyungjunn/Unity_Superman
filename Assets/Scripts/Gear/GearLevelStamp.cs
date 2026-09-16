using GrowNa.Core;

namespace GrowNa.Gear
{
    public static class GearLevelStamp
    {
        public static int Of(PlayerStats stats)
            => GearTable.ClampItemLevel(stats != null ? stats.Level : GearTable.MinItemLevel);
    }
}
