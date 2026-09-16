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

        /// <summary>광고 시청 보상 배수. 수령 UI 의 2배 버튼이 이 값을 쓴다.</summary>
        public const double AdMultiplier = 2.0;

        public static OfflineResult Compute(int world, int stage, double elapsedSeconds)
        {
            if (elapsedSeconds < MinSeconds || double.IsNaN(elapsedSeconds))
                return new OfflineResult { seconds = Math.Max(0, elapsedSeconds) };

            bool capped = elapsedSeconds > MaxSeconds;
            double seconds = Math.Min(elapsedSeconds, MaxSeconds);
            double kills = seconds * KillsPerSecond * Efficiency;

            return new OfflineResult
            {
                seconds = seconds,
                gold = Math.Floor(StageTable.ExpectedGoldPerKill(world, stage) * kills),
                grain = Math.Floor(StageTable.ExpectedGrainPerKill * kills),
                capped = capped,
            };
        }

        public static OfflineResult Doubled(OfflineResult result)
        {
            result.gold = Math.Floor(result.gold * AdMultiplier);
            result.grain = Math.Floor(result.grain * AdMultiplier);
            return result;
        }
    }
}
