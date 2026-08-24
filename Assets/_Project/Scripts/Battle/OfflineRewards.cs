using System;

namespace GrowNa.Battle
{
    public struct OfflineResult
    {
        public double seconds;
        public double gold;
        public double grain;
        public bool capped;
        public bool Any => gold > 0 || grain > 0;
    }

    public static class OfflineRewards
    {
        public const double MaxSeconds = 8 * 3600;
        public const double MinSeconds = 60;
        public const double KillsPerSecond = 0.55;
        public const double Efficiency = 0.5;

        public static OfflineResult Compute(int world, int stage, double elapsedSeconds)
        {
            if (elapsedSeconds < MinSeconds || double.IsNaN(elapsedSeconds))
                return new OfflineResult { seconds = Math.Max(0, elapsedSeconds) };

            bool capped = elapsedSeconds > MaxSeconds;
            double seconds = Math.Min(elapsedSeconds, MaxSeconds);
            var reward = StageTable.KillReward(world, stage);
            double kills = seconds * KillsPerSecond * Efficiency;

            return new OfflineResult
            {
                seconds = seconds,
                gold = Math.Floor(reward.gold * kills),
                grain = Math.Floor(reward.grain * kills),
                capped = capped,
            };
        }
    }
}
