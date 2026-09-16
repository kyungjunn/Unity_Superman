using System;

namespace GrowNa.Core
{
    public enum StatKind
    {
        Attack = 0,
        Health = 1,
        Crit = 2,
    }

    /// <summary>
    /// 골드 소모처. 방치형의 기본 루프는 "전투로 번 골드를 스탯에 바로 태우는 것"이라
    /// 비용은 지수, 효과는 선형으로 둔다 (레벨이 오를수록 체감 성장이 느려짐 = 다음 티어 압력).
    /// </summary>
    public static class UpgradeTable
    {
        public const int MaxLevel = 9999;

        public static readonly StatKind[] All = { StatKind.Attack, StatKind.Health, StatKind.Crit };

        public static string DisplayName(StatKind kind) => kind switch
        {
            StatKind.Attack => "공격력",
            StatKind.Health => "체력",
            _ => "치명타",
        };

        static double BaseCost(StatKind kind) => kind switch
        {
            StatKind.Attack => 25,
            StatKind.Health => 30,
            _ => 400,
        };

        static double CostGrowth(StatKind kind) => kind switch
        {
            StatKind.Crit => 1.35,
            _ => 1.15,
        };

        /// <summary>level = 현재 강화 레벨(0부터). 반환값은 다음 1레벨 비용.</summary>
        public static double Cost(StatKind kind, int level)
        {
            if (level < 0) level = 0;
            return Math.Floor(BaseCost(kind) * Math.Pow(CostGrowth(kind), level));
        }

        /// <summary>공격/체력은 배수(1.0 기준), 치명타는 확률 가산값.</summary>
        public static double Bonus(StatKind kind, int level)
        {
            if (level < 0) level = 0;
            return kind switch
            {
                StatKind.Attack => 1.0 + 0.12 * level,
                StatKind.Health => 1.0 + 0.10 * level,
                _ => 0.004 * level,
            };
        }

        public const double CritChanceCap = 0.60;

        public static string BonusText(StatKind kind, int level) => kind switch
        {
            StatKind.Attack => $"공격 +{(Bonus(kind, level) - 1.0) * 100:0}%",
            StatKind.Health => $"체력 +{(Bonus(kind, level) - 1.0) * 100:0}%",
            _ => $"치명 +{Bonus(kind, level) * 100:0.0}%p",
        };
    }
}
