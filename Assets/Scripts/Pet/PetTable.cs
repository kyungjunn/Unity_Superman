using System;
using UnityEngine;

namespace GrowNa.Pet
{
    public enum PetRarity { Common = 0, Rare = 1, Epic = 2, Legend = 3 }

    public readonly struct PetDef
    {
        public readonly int id;
        public readonly string name;
        public readonly PetRarity rarity;
        public readonly double attackBonus;
        public readonly double healthBonus;

        public PetDef(int id, string name, PetRarity rarity, double attackBonus, double healthBonus)
        {
            this.id = id;
            this.name = name;
            this.rarity = rarity;
            this.attackBonus = attackBonus;
            this.healthBonus = healthBonus;
        }

        public double AttackMultiplier => 1.0 + attackBonus;
        public double HealthMultiplier => 1.0 + healthBonus;
    }

    /// <summary>
    /// 펫 정의와 뽑기 확률. 장착은 <see cref="PetService"/> 가 들고 세이브에 id 로 남는다 —
    /// id 0 이 실존하므로 빈 칸은 반드시 <see cref="None"/>(-1) 이어야 한다(JsonUtility 0-채움 충돌).
    /// </summary>
    public static class PetTable
    {
        public const double DrawCost = 50;
        public const int RarityCount = 4;
        public const int None = -1;

        /// <summary>중복 뽑기가 올리는 레벨 상한. 만렙 이후는 다이아 절반 환급.</summary>
        public const int MaxLevel = 10;

        /// <summary>레벨당 보너스 가산. lv.10 = 1.54배. 스킬과 같은 선형 — 전설 펫이 곱셈이라 지수는 금지.</summary>
        public const double LevelGrowth = 0.06;

        public const double MaxLevelRefund = 0.5;

        static readonly PetDef[] Defs =
        {
            new PetDef(0, "들참새",   PetRarity.Common, 0.05, 0.05),
            new PetDef(1, "논개구리", PetRarity.Common, 0.07, 0.06),
            new PetDef(2, "밭쥐",     PetRarity.Common, 0.06, 0.09),
            new PetDef(3, "까치",     PetRarity.Rare,   0.14, 0.12),
            new PetDef(4, "메뚜기떼", PetRarity.Rare,   0.18, 0.09),
            new PetDef(5, "여우비",   PetRarity.Epic,   0.28, 0.24),
            new PetDef(6, "달토끼",   PetRarity.Epic,   0.22, 0.34),
            new PetDef(7, "곡령 학",  PetRarity.Legend, 0.55, 0.45),
        };

        static readonly double[] RarityWeights = { 0.62, 0.26, 0.10, 0.02 };

        static readonly string[] RarityNames = { "일반", "희귀", "영웅", "전설" };

        static readonly Color[] RarityColors =
        {
            new Color(0.78f, 0.76f, 0.70f),
            new Color(0.42f, 0.66f, 0.95f),
            new Color(0.70f, 0.47f, 0.93f),
            new Color(0.99f, 0.66f, 0.26f),
        };

        public static int Count => Defs.Length;

        public static PetDef Get(int id) => Defs[Math.Clamp(id, 0, Defs.Length - 1)];

        public static bool IsValid(int id) => id >= 0 && id < Defs.Length;

        public static string Name(PetRarity rarity) => RarityNames[(int)rarity];

        public static Color Color(PetRarity rarity) => RarityColors[(int)rarity];

        public static double RarityChance(PetRarity rarity) => RarityWeights[(int)rarity];

        /// <summary>희귀도 우선 정렬 점수. 보너스 합(최대 1.0)보다 등급 가중치가 커서 등급 역전이 없다.</summary>
        public static double Strength(int id)
        {
            if (!IsValid(id)) return double.NegativeInfinity;
            var def = Get(id);
            return (int)def.rarity * 100.0 + def.attackBonus + def.healthBonus;
        }

        /// <summary>등급을 먼저 뽑고 그 등급 안에서 균등 선택. <see cref="Skill.SkillTable.Roll"/> 과 같은 2단 추첨.</summary>
        public static int Roll(double rarityRoll01, double pickRoll01)
        {
            var rarity = RollRarity(rarityRoll01);
            int first = -1, count = 0;
            for (int i = 0; i < Defs.Length; i++)
            {
                if (Defs[i].rarity != rarity) continue;
                if (first < 0) first = i;
                count++;
            }
            if (count <= 0) return 0;

            int offset = (int)(Math.Clamp(pickRoll01, 0.0, 0.999999) * count);
            return first + offset;
        }

        public static PetRarity RollRarity(double roll01)
        {
            double cursor = 0;
            for (int i = 0; i < RarityWeights.Length; i++)
            {
                cursor += RarityWeights[i];
                if (roll01 < cursor) return (PetRarity)i;
            }
            return PetRarity.Legend;
        }

        public static int ClampLevel(int level)
        {
            if (level < 0) return 0;
            return level > MaxLevel ? MaxLevel : level;
        }

        /// <summary>미보유는 0, 첫 획득은 1. 장착 펫의 공/체 보너스에만 곱한다.</summary>
        public static double LevelFactor(int level)
        {
            int lv = ClampLevel(level);
            if (lv <= 0) return 0;
            return 1.0 + LevelGrowth * (lv - 1);
        }

        public static double ScaledAttackMultiplier(int id, int level)
            => 1.0 + Get(id).attackBonus * LevelFactor(level);

        public static double ScaledHealthMultiplier(int id, int level)
            => 1.0 + Get(id).healthBonus * LevelFactor(level);

        /// <summary>현재 레벨에 뽑기 한 장을 적용한다. 만렙이면 그대로 두고 <paramref name="refunded"/>.</summary>
        public static int ApplyPull(int current, out bool refunded)
        {
            int lv = ClampLevel(current);
            if (lv <= 0)
            {
                refunded = false;
                return 1;
            }

            refunded = lv >= MaxLevel;
            return refunded ? MaxLevel : lv + 1;
        }

        /// <summary>보유 목록에서 활성 펫을 고른다. 없으면 <see cref="None"/>.</summary>
        public static int BestOwned(bool[] owned)
        {
            if (owned == null) return None;
            int best = None;
            for (int i = 0; i < Defs.Length && i < owned.Length; i++)
            {
                if (!owned[i]) continue;
                if (best == None || Strength(i) > Strength(best)) best = i;
            }
            return best;
        }
    }
}
