using System;

namespace GrowNa.Gear
{
    /// <summary>
    /// GDD 4.3 장비 강화. 골드의 2차 소모처.
    /// <b>실패해도 파괴·하락이 없다</b> — 방치형에서 파괴는 이탈 요인이라 골드만 태운다.
    /// </summary>
    public static class EnhanceTable
    {
        public const int MaxPlus = 20;
        public const double PowerPerPlus = 0.08;

        /// <summary>실패 누적 이 횟수에 도달하면 다음 시도는 확정 성공 (GDD 4.3 소프트 천장).</summary>
        public const int FailStreakForGuarantee = 3;

        static readonly int[] BandStart = { 0, 5, 10, 15 };
        static readonly double[] BandChance = { 1.00, 0.70, 0.40, 0.15 };
        static readonly double[] BandCostMultiplier = { 1, 4, 20, 120 };

        public static bool IsMax(int plus) => plus >= MaxPlus;

        static int Band(int plus)
        {
            int band = 0;
            for (int i = BandStart.Length - 1; i >= 0; i--)
                if (plus >= BandStart[i]) { band = i; break; }
            return band;
        }

        public static double SuccessChance(int plus)
        {
            if (IsMax(plus)) return 0;
            return BandChance[Band(Math.Max(0, plus))];
        }

        /// <summary>실패 스트릭을 반영한 실효 확률. 소프트 천장에 걸리면 1.</summary>
        public static double EffectiveChance(int plus, int failStreak)
        {
            if (IsMax(plus)) return 0;
            return failStreak >= FailStreakForGuarantee ? 1.0 : SuccessChance(plus);
        }

        /// <summary>비용은 티어 파워에 비례하고 구간 배율과 강화 수치로 다시 붇는다.</summary>
        public static double Cost(GearTier tier, int plus)
        {
            if (IsMax(plus)) return 0;
            plus = Math.Max(0, plus);
            return Math.Floor(100 * GearTable.Power(tier) * BandCostMultiplier[Band(plus)] * Math.Pow(1.15, plus));
        }

        public static double PowerMultiplier(int plus) => 1.0 + PowerPerPlus * Math.Max(0, plus);

        public static string ChanceText(int plus, int failStreak)
            => IsMax(plus) ? "최대" : failStreak >= FailStreakForGuarantee ? "확정" : Core.BigNum.Percent(SuccessChance(plus), 0);
    }
}
