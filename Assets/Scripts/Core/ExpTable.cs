using System;

namespace GrowNa.Core
{
    /// <summary>
    /// 캐릭터 레벨 곡선. 순수 함수만 두어 EditMode 에서 그대로 검증한다.
    ///
    /// 필요 경험치는 지수(1.14^), 획득 경험치는 스테이지 지수(1.13^)로 잡았다.
    /// 획득 쪽 밑이 약간 작아서 같은 스테이지에 머물수록 레벨업이 느려지고,
    /// 스테이지를 밀면 다시 빨라진다 — 방치형의 "진행 = 성장" 압력을 만드는 장치다.
    /// </summary>
    public static class ExpTable
    {
        public const int MaxLevel = 999;

        public const double BaseRequirement = 50.0;
        public const double RequirementGrowth = 1.14;

        public const double BaseMonsterExp = 8.0;
        public const double MonsterExpGrowth = 1.13;

        /// <summary>단계(rank)당 경험치 가산. <see cref="Battle.StageTable.RankMultiplier"/> 와 같은 기울기.</summary>
        public const double RankExpBonus = 0.125;

        /// <summary>보스는 같은 스테이지 최상위 단계 몬스터의 이 배수만큼 준다.</summary>
        public const double BossExpMultiplier = 12.0;

        /// <summary>level -> level+1 에 필요한 경험치. 만렙에서는 무한대라 절대 차오르지 않는다.</summary>
        public static double Required(int level)
        {
            if (level >= MaxLevel) return double.PositiveInfinity;
            int l = level < 1 ? 1 : level;
            return Math.Floor(BaseRequirement * Math.Pow(RequirementGrowth, l - 1));
        }

        public static double MonsterExp(int globalStageIndex, int rank)
        {
            int g = globalStageIndex < 1 ? 1 : globalStageIndex;
            int r = rank < 0 ? 0 : rank;
            return Math.Floor(BaseMonsterExp * Math.Pow(MonsterExpGrowth, g - 1) * (1.0 + RankExpBonus * r));
        }

        public static double BossExp(int globalStageIndex, int topRank)
            => Math.Floor(MonsterExp(globalStageIndex, topRank) * BossExpMultiplier);

        /// <summary>현재 레벨 진행률 [0,1]. 만렙이면 항상 1.</summary>
        public static double Ratio(int level, double exp)
        {
            double required = Required(level);
            if (double.IsInfinity(required)) return 1.0;
            if (required <= 0) return 0.0;
            double r = exp / required;
            return r < 0 ? 0 : r > 1 ? 1 : r;
        }
    }
}
