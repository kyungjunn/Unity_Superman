using System;

namespace GrowNa.Battle
{
    public struct StageReward
    {
        public double gold;
        public double grain;
        public double wick;
    }

    public static class StageTable
    {
        public const int MonstersPerStage = 10;
        public const float BossTimeLimit = 30f;

        public static int GlobalIndex(int world, int stage) => (world - 1) * 10 + stage;

        public static double MonsterHp(int world, int stage)
            => 100.0 * Math.Pow(1.18, GlobalIndex(world, stage) - 1);

        public static double MonsterAttack(int world, int stage)
            => 8.0 * Math.Pow(1.16, GlobalIndex(world, stage) - 1);

        public static StageReward KillReward(int world, int stage)
        {
            int g = GlobalIndex(world, stage);
            return new StageReward
            {
                gold = 10.0 * Math.Pow(1.16, g - 1),
                grain = 1.0 * Math.Pow(1.10, g - 1),
                wick = 0,
            };
        }

        public static StageReward BossReward(int world, int stage)
        {
            var r = KillReward(world, stage);
            return new StageReward
            {
                gold = r.gold * 20,
                grain = r.grain * 20,
                wick = 1 + GlobalIndex(world, stage) / 5,
            };
        }

        public static double BossHpMultiplier => 8.0;

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
