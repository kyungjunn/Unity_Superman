using System;
using GrowNa.Core;
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

        public const int MinItemLevel = 1;
        public const int MaxItemLevel = ExpTable.MaxLevel;

        /// <summary>
        /// 장비 레벨 1당 파워 배수. 희귀도와 직교하는 두 번째 축이다.
        ///
        /// 1.8 은 임의값이 아니라 "일반 5레벨 > 희귀 1레벨" 을 성립시키는 하한에서 잡았다.
        /// 희귀/일반 티어 파워비가 9배(<see cref="TierPower"/>)이므로 1.8^4 = 10.5 > 9 로 넘어선다.
        /// 이 값을 1.73 아래로 내리면 그 관계가 뒤집히므로 함부로 낮추지 말 것.
        /// </summary>
        public const double LevelPowerGrowth = 1.8;

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

        // 분해 파편량. GDD 5.4 파편 교환(T5=3,000 / T6=15,000)을 기준점으로 잡고
        // "같은 티어 10개 분해 ≈ 교환 1회" 가 되도록 티어 파워에 비례시켰다.
        static readonly double[] TierShard = { 1, 3, 10, 60, 300, 1500, 7000, 30000 };

        // 판매 골드. 심지 교체의 골드 비용(Lv.1 = 500)을 기준점으로 잡고
        // "낡은 장비 10개 판매 ≈ 첫 심지 교체 1회" 가 되도록 티어 파워에 50을 곱했다.
        static readonly double[] TierSellGold = { 50, 150, 450, 1500, 5500, 22500, 100000, 500000 };

        /// <summary>합성에 필요한 동일 티어·슬롯 개수. GDD 4.4 — T6(신화) 이상은 5개.</summary>
        public const int FusionCountLow = 4;
        public const int FusionCountHigh = 5;

        public static string Name(GearSlot slot) => SlotNames[(int)slot];
        public static string Name(GearTier tier) => TierNames[(int)tier];
        public static double Power(GearTier tier) => TierPower[(int)tier];
        public static Color Color(GearTier tier) => TierColors[(int)tier];

        public static int ClampItemLevel(int level)
            => level < MinItemLevel ? MinItemLevel : level > MaxItemLevel ? MaxItemLevel : level;

        public static double LevelFactor(int level)
            => Math.Pow(LevelPowerGrowth, ClampItemLevel(level) - MinItemLevel);

        public static bool IsOffense(GearSlot slot)
            => slot == GearSlot.Weapon || slot == GearSlot.Gloves || slot == GearSlot.Ring;

        public static bool IsDefense(GearSlot slot)
            => slot == GearSlot.Helmet || slot == GearSlot.Armor || slot == GearSlot.Boots;

        public static double Shard(GearTier tier) => TierShard[(int)tier];

        public static double SellGold(GearTier tier) => TierSellGold[(int)tier];

        public static bool IsMaxTier(GearTier tier) => (int)tier >= TierCount - 1;

        public static GearTier NextTier(GearTier tier)
            => IsMaxTier(tier) ? tier : (GearTier)((int)tier + 1);

        /// <summary>합성 재료 개수. 신화(T6) 이상은 5개, 그 아래는 4개.</summary>
        public static int FusionCount(GearTier tier)
            => tier >= GearTier.Myth ? FusionCountHigh : FusionCountLow;

        /// <summary>재고 배열 인덱스. 슬롯×티어 64칸을 1차원으로 편다.</summary>
        public static int Index(GearSlot slot, GearTier tier) => (int)slot * TierCount + (int)tier;

        public const int StockSize = SlotCount * TierCount;
    }
}
