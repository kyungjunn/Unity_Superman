using UnityEngine;

namespace GrowNa.Gear
{
    public enum GearSlot
    {
        Weapon = 0,
        Helmet = 1,
        Armor = 2,
        Gloves = 3,
        Boots = 4,
        Amulet = 5,
        Ring = 6,
        Charm = 7,
    }

    public enum GearTier
    {
        Worn = 0,
        Decent = 1,
        Rare = 2,
        Heroic = 3,
        Legend = 4,
        Myth = 5,
        Spirit = 6,
        Earth = 7,
    }

    public static class GearTable
    {
        public const int SlotCount = 8;
        public const int TierCount = 8;

        static readonly string[] SlotNames = { "무기", "투구", "갑옷", "장갑", "신발", "목걸이", "반지", "부적" };
        static readonly string[] TierNames = { "낡은", "쓸만한", "희귀", "영웅", "전설", "신화", "곡령", "대지" };
        static readonly double[] TierPower = { 1, 3, 9, 30, 110, 450, 2000, 10000 };

        static readonly Color[] TierColors =
        {
            new Color(0.72f, 0.72f, 0.70f),
            new Color(0.45f, 0.80f, 0.42f),
            new Color(0.38f, 0.62f, 0.95f),
            new Color(0.68f, 0.45f, 0.92f),
            new Color(0.98f, 0.62f, 0.22f),
            new Color(0.92f, 0.30f, 0.30f),
            new Color(0.28f, 0.85f, 0.80f),
            new Color(1.00f, 0.84f, 0.32f),
        };

        public static string Name(GearSlot slot) => SlotNames[(int)slot];
        public static string Name(GearTier tier) => TierNames[(int)tier];
        public static double Power(GearTier tier) => TierPower[(int)tier];
        public static Color Color(GearTier tier) => TierColors[(int)tier];

        public static bool IsOffense(GearSlot slot)
            => slot == GearSlot.Weapon || slot == GearSlot.Gloves || slot == GearSlot.Ring;

        public static bool IsDefense(GearSlot slot)
            => slot == GearSlot.Helmet || slot == GearSlot.Armor || slot == GearSlot.Boots;
    }
}
