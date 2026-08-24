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

        public static readonly GearItem Empty = default;

        public static GearItem Of(GearSlot slot, GearTier tier, int plus = 0) => new GearItem
        {
            slot = slot,
            tier = tier,
            plus = plus,
            owned = true,
        };

        public double Power => owned ? GearTable.Power(tier) * (1.0 + 0.08 * plus) : 0.0;

        public string DisplayName => owned ? $"{GearTable.Name(tier)} {GearTable.Name(slot)}" : "빈 슬롯";

        public string ShortLabel => owned
            ? (plus > 0 ? $"{GearTable.Name(tier)}+{plus}" : GearTable.Name(tier))
            : "빈 칸";

        public bool StrongerThan(GearItem other) => Power > other.Power;
    }
}
