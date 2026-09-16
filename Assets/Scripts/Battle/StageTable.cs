using System;
using GrowNa.Core;

namespace GrowNa.Battle
{
    public struct StageReward
    {
        public double gold;
        public double grain;
        public double wick;
        public double exp;
    }

    public static class StageTable
    {
        public const int MonstersPerStage = 5;
        public const float BossTimeLimit = 30f;

        /// <summary>한 스테이지를 이루는 일반 몬스터 단계 수. 처치 수를 이 단계에 균등 배분한다.</summary>
        public const int MonsterRanks = 5;

        /// <summary>일반 몬스터는 한 마리씩이 아니라 이 범위의 무리로 몰려온다. 보스는 항상 혼자.</summary>
        public const int MinWaveSize = 2;
        public const int MaxWaveSize = 3;

        /// <summary>일반 몬스터가 낟알을 떨굴 확률. 떨어지면 언제나 정확히 1개다.</summary>
        public const double GrainDropChance = 0.12;
        public const double GrainDropPerKill = 1;

        public const double BossGrainMin = 5;
        public const double BossGrainMax = 6;

        static readonly string[] RankNames = { "떠돌이", "무리", "굶주린", "사나운", "정예" };

        public static int GlobalIndex(int world, int stage) => (world - 1) * 10 + stage;

        /// <summary>0-based 처치 수를 0-based 몬스터 단계로 접는다. 5마리 / 5단계 = 처치 1회당 한 단계.</summary>
        public static int RankOf(int killsDone)
        {
            if (killsDone <= 0) return 0;
            int rank = killsDone * MonsterRanks / MonstersPerStage;
            return rank >= MonsterRanks ? MonsterRanks - 1 : rank;
        }

        /// <summary>보스까지 남은 일반 몬스터 수.</summary>
        public static int Remaining(int killsDone) => Math.Max(0, MonstersPerStage - Math.Max(0, killsDone));

        /// <summary>
        /// 이번에 몰려올 무리의 크기. <paramref name="roll01"/> 로 2~3을 고르되
        /// <b>남은 처치 수를 넘지 않는다</b> — 넘기면 보스 진입 카운터가 초과분만큼 헛돈다.
        /// </summary>
        public static int WaveSize(int killsDone, double roll01)
        {
            int remaining = Remaining(killsDone);
            if (remaining <= 0) return 0;
            int size = roll01 < 0.5 ? MinWaveSize : MaxWaveSize;
            return Math.Min(size, remaining);
        }

        /// <summary>단계가 오를수록 강해지고 더 준다. 마지막 단계가 스테이지 기준값의 1.5배.</summary>
        public static double RankMultiplier(int rank)
            => 1.0 + 0.125 * Math.Clamp(rank, 0, MonsterRanks - 1);

        public static string RankName(int rank) => RankNames[Math.Clamp(rank, 0, MonsterRanks - 1)];

        public static double MonsterHp(int world, int stage, int rank = 0)
            => 100.0 * Math.Pow(1.18, GlobalIndex(world, stage) - 1) * RankMultiplier(rank);

        public static double MonsterAttack(int world, int stage, int rank = 0)
            => 8.0 * Math.Pow(1.16, GlobalIndex(world, stage) - 1) * RankMultiplier(rank);

        static double BaseKillGold(int world, int stage, int rank)
            => Math.Floor(10.0 * Math.Pow(1.16, GlobalIndex(world, stage) - 1) * RankMultiplier(rank));

        /// <summary>
        /// 일반 처치 보상. 골드와 경험치는 <b>무조건</b> 나오고, 낟알만 <paramref name="grainRoll01"/> 판정이다.
        /// 판정값을 인자로 받는 덕에 드롭 확률이 테스트로 고정된다.
        /// </summary>
        public static StageReward KillReward(int world, int stage, int rank, double grainRoll01)
            => new StageReward
            {
                gold = BaseKillGold(world, stage, rank),
                grain = grainRoll01 < GrainDropChance ? GrainDropPerKill : 0,
                wick = 0,
                exp = ExpTable.MonsterExp(GlobalIndex(world, stage), rank),
            };

        /// <summary>보스는 낟알을 확정으로 <see cref="BossGrainMin"/>~<see cref="BossGrainMax"/> 개 떨군다.</summary>
        public static StageReward BossReward(int world, int stage, double grainRoll01)
        {
            int g = GlobalIndex(world, stage);
            return new StageReward
            {
                gold = BaseKillGold(world, stage, MonsterRanks - 1) * 20,
                grain = grainRoll01 < 0.5 ? BossGrainMin : BossGrainMax,
                wick = 1 + g / 5,
                exp = ExpTable.BossExp(g, MonsterRanks - 1),
            };
        }

        /// <summary>일반 처치 1회의 낟알 기대값. 오프라인 보상이 확률 대신 이 값을 쓴다.</summary>
        public static double ExpectedGrainPerKill => GrainDropChance * GrainDropPerKill;

        /// <summary>일반 처치 1회의 골드 기대값. 단계가 고르게 섞이므로 중간 단계를 기준으로 잡는다.</summary>
        public static double ExpectedGoldPerKill(int world, int stage)
            => BaseKillGold(world, stage, MonsterRanks / 2);

        /// <summary>
        /// 보스 HP 배수. 1-1 은 장비도 레벨도 없는 상태로 만나는 첫 벽이라 낮게 시작해
        /// 다섯 스테이지에 걸쳐 기준값(8배)까지 오른다.
        /// </summary>
        public static double BossHpMultiplier(int world, int stage)
        {
            int g = GlobalIndex(world, stage);
            if (g >= 5) return 8.0;
            return 3.0 + (8.0 - 3.0) * (g - 1) / 4.0;
        }

        public static string WorldName(int world) => world switch
        {
            1 => "밭두렁",
            2 => "과수원",
            3 => "방앗간",
            4 => "폐허가 된 마을",
            5 => "무너진 성",
            6 => "마른 대지",
            _ => "하늘 위 곡창",
        };
    }
}
