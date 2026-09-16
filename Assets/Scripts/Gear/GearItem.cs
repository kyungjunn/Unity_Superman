using System;

namespace GrowNa.Gear
{
    [Serializable]
    public struct GearItem
    {
        public GearSlot slot;
        public GearTier tier;
        public int plus;
        public bool owned;
        public int level;

        public static readonly GearItem Empty = default;

        public static GearItem Of(GearSlot slot, GearTier tier, int plus = 0) => new GearItem
        {
            slot = slot,
            tier = tier,
            plus = plus,
            owned = true,
            level = GearTable.MinItemLevel,
        };

        /// <summary>획득 시점의 캐릭터 레벨을 찍어 만든다. 봉헌·합성·교환이 전부 이 경로를 쓴다.</summary>
        public static GearItem At(GearSlot slot, GearTier tier, int level, int plus = 0) => new GearItem
        {
            slot = slot,
            tier = tier,
            plus = plus,
            owned = true,
            level = GearTable.ClampItemLevel(level),
        };

        /// <summary>v2 이하 세이브의 장비는 level 이 0이라 최소 레벨로 읽는다.</summary>
        public int Level => GearTable.ClampItemLevel(level);

        public double Power => owned
            ? GearTable.Power(tier) * GearTable.LevelFactor(Level) * (1.0 + 0.08 * plus)
            : 0.0;

        public double SellGold => owned
            ? GearTable.SellGold(tier) * GearTable.LevelFactor(Level) * (1.0 + 0.08 * plus)
            : 0.0;

        public string DisplayName => owned ? $"{GearTable.Name(tier)} {GearTable.Name(slot)}" : "빈 슬롯";

        public string ShortLabel => owned
            ? (plus > 0 ? $"Lv.{Level} {GearTable.Name(tier)}+{plus}" : $"Lv.{Level} {GearTable.Name(tier)}")
            : "빈 칸";

        public bool StrongerThan(GearItem other) => Power > other.Power;
    }
}
