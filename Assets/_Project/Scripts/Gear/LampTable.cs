using System;
using UnityEngine;

namespace GrowNa.Gear
{
    public static class LampTable
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 200;
        public const int PityThreshold = 200;
        public const double SingleOfferCost = 100;
        public const double TenOfferCost = 900;
        public const int TenOfferCount = 10;
        public static readonly GearTier TenOfferFloor = GearTier.Rare;

        static readonly int[] AnchorLevels = { 1, 5, 15, 30, 60, 100, 150, 200 };

        static readonly double[][] AnchorPercents =
        {
            new[] { 70.0, 25.0,  5.0,  0.0,  0.0,  0.0, 0.0, 0.0 },
            new[] { 55.0, 30.0, 14.0,  1.0,  0.0,  0.0, 0.0, 0.0 },
            new[] { 30.0, 35.0, 25.0,  9.5,  0.5,  0.0, 0.0, 0.0 },
            new[] { 12.0, 28.0, 33.0, 22.0,  5.0,  0.0, 0.0, 0.0 },
            new[] {  2.0, 12.0, 30.0, 35.0, 18.0,  3.0, 0.0, 0.0 },
            new[] {  0.0,  3.0, 15.0, 35.0, 32.0, 14.0, 1.0, 0.0 },
            new[] {  0.0,  0.0,  5.0, 22.0, 38.0, 27.0, 7.5, 0.5 },
            new[] {  0.0,  0.0,  0.0, 10.0, 30.0, 38.0, 18.0, 4.0 },
        };

        public static int ClampLevel(int level) => Mathf.Clamp(level, MinLevel, MaxLevel);

        /// <summary>GDD 5.2 표를 앵커로 두고 사이 레벨은 선형 보간. 합은 항상 1.</summary>
        public static double[] Weights(int level)
        {
            level = ClampLevel(level);
            var result = new double[GearTable.TierCount];

            int upper = 0;
            while (upper < AnchorLevels.Length - 1 && AnchorLevels[upper] < level) upper++;

            if (AnchorLevels[upper] == level || upper == 0)
            {
                Array.Copy(AnchorPercents[upper], result, result.Length);
            }
            else
            {
                int lower = upper - 1;
                double span = AnchorLevels[upper] - AnchorLevels[lower];
                double t = (level - AnchorLevels[lower]) / span;
                for (int i = 0; i < result.Length; i++)
                {
                    // 아직 등장하지 않은 티어(하한 앵커가 0)는 보간하지 않는다.
                    // 상한 앵커 레벨에 도달하는 순간 0% -> N% 로 튀어야 GDD 5.2의 "0이 아니게 되는 순간" 연출이 산다.
                    result[i] = AnchorPercents[lower][i] <= 0
                        ? 0
                        : AnchorPercents[lower][i] + (AnchorPercents[upper][i] - AnchorPercents[lower][i]) * t;
                }
            }

            double sum = 0;
            for (int i = 0; i < result.Length; i++) sum += result[i];
            if (sum <= 0) return result;
            for (int i = 0; i < result.Length; i++) result[i] /= sum;
            return result;
        }

        public static double TierChance(int level, GearTier tier) => Weights(level)[(int)tier];

        public static GearTier HighestTier(int level)
        {
            var weights = Weights(level);
            for (int i = weights.Length - 1; i >= 0; i--)
                if (weights[i] > 0.0) return (GearTier)i;
            return GearTier.Worn;
        }

        /// <summary>roll01 은 [0,1). 누적 분포를 훑어 티어를 고른다.</summary>
        public static GearTier Roll(double[] weights, double roll01)
        {
            double cursor = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                cursor += weights[i];
                if (roll01 < cursor) return (GearTier)i;
            }
            for (int i = weights.Length - 1; i >= 0; i--)
                if (weights[i] > 0.0) return (GearTier)i;
            return GearTier.Worn;
        }

        public static double OfferCost(int count) => count >= TenOfferCount ? TenOfferCost : SingleOfferCost * count;

        static double GoldMultiplier(int level)
            => level <= 30 ? 1.0
             : level <= 60 ? 5.0
             : level <= 100 ? 25.0
             : level <= 150 ? 150.0
                            : 1000.0;

        static (int from, int to, int lo, int hi) WickBand(int level)
            => level <= 30 ? (1, 30, 2, 20)
             : level <= 60 ? (31, 60, 30, 80)
             : level <= 100 ? (61, 100, 100, 250)
             : level <= 150 ? (101, 150, 300, 700)
                            : (151, 200, 900, 2500);

        /// <summary>level -> level+1 로 올리는 데 드는 심지. GDD 5.3 구간표를 구간 내 선형 보간.</summary>
        public static double WickCost(int level)
        {
            if (level >= MaxLevel) return 0;
            level = ClampLevel(level);
            var (from, to, lo, hi) = WickBand(level);
            double t = to == from ? 0 : (level - from) / (double)(to - from);
            return Math.Floor(lo + (hi - lo) * t);
        }

        public static double GoldCost(int level)
        {
            if (level >= MaxLevel) return 0;
            level = ClampLevel(level);
            return Math.Floor(500 * GoldMultiplier(level) * Math.Pow(1.06, level - 1));
        }

        public static Color FlameColor(int level)
            => level < 30 ? new Color(1.00f, 0.55f, 0.18f)
             : level < 60 ? new Color(0.25f, 0.85f, 0.80f)
             : level < 100 ? new Color(0.66f, 0.42f, 0.95f)
                           : new Color(1.00f, 0.84f, 0.30f);
    }
}
