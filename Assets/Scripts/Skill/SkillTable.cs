using System;
using UnityEngine;

namespace GrowNa.Skill
{
    public enum SkillRarity { Common = 0, Rare = 1, Epic = 2, Legend = 3 }

    public readonly struct SkillDef
    {
        public readonly int id;
        public readonly string name;
        public readonly SkillRarity rarity;
        public readonly float cooldown;
        public readonly double damageMultiplier;

        public SkillDef(int id, string name, SkillRarity rarity, float cooldown, double damageMultiplier)
        {
            this.id = id;
            this.name = name;
            this.rarity = rarity;
            this.cooldown = cooldown;
            this.damageMultiplier = damageMultiplier;
        }
    }

    /// <summary>
    /// 스킬 정의와 뽑기 확률. 순수 static 이라 EditMode 에서 그대로 검증한다.
    ///
    /// 쿨타임과 배수는 "초당 기대 배수"가 등급마다 단조 증가하도록 짝지었다
    /// (일반 1.2/6s = 0.20, 전설 9.0/16s = 0.56). 배수만 보고 상향/하향하면 이 관계가 깨진다.
    /// </summary>
    public static class SkillTable
    {
        public const int EquipSlots = 6;
        public const int RarityCount = 4;

        /// <summary>스킬 1회 뽑기에 드는 다이아.</summary>
        public const double DrawCost = 30;

        /// <summary>중복 뽑기가 올리는 레벨 상한. 만렙 이후는 다이아 절반 환급.</summary>
        public const int MaxLevel = 10;

        /// <summary>레벨당 피해 가산. lv.10 = 1.54배. 지수로 올리면 전설 6칸이 평타를 삼킨다.</summary>
        public const double LevelGrowth = 0.06;

        public const double MaxLevelRefund = 0.5;

        public const int None = -1;

        static readonly SkillDef[] Defs =
        {
            new SkillDef(0, "맨주먹", SkillRarity.Common, 6f, 1.2),
            new SkillDef(1, "참새 쪼기", SkillRarity.Common, 7f, 1.4),
            new SkillDef(2, "짚단 휘두르기", SkillRarity.Common, 8f, 1.7),
            new SkillDef(3, "낫질", SkillRarity.Rare, 9f, 2.4),
            new SkillDef(4, "겨 폭풍", SkillRarity.Rare, 10f, 2.8),
            new SkillDef(5, "허깨비 분신", SkillRarity.Rare, 11f, 3.2),
            new SkillDef(6, "탈곡 회전베기", SkillRarity.Epic, 12f, 4.5),
            new SkillDef(7, "볏짚 화염", SkillRarity.Epic, 13f, 5.2),
            new SkillDef(8, "까마귀 군무", SkillRarity.Epic, 14f, 5.8),
            new SkillDef(9, "풍년의 벼락", SkillRarity.Legend, 15f, 8.0),
            new SkillDef(10, "곡령 강림", SkillRarity.Legend, 16f, 9.0),
            new SkillDef(11, "대지의 낟알비", SkillRarity.Legend, 18f, 11.0),
        };

        static readonly double[] RarityWeights = { 0.60, 0.28, 0.10, 0.02 };

        static readonly string[] RarityNames = { "일반", "희귀", "영웅", "전설" };

        static readonly Color[] RarityColors =
        {
            new Color(0.72f, 0.72f, 0.70f),
            new Color(0.38f, 0.62f, 0.95f),
            new Color(0.68f, 0.45f, 0.92f),
            new Color(0.98f, 0.62f, 0.22f),
        };

        public static int Count => Defs.Length;

        public static SkillDef Get(int id) => Defs[Math.Clamp(id, 0, Defs.Length - 1)];

        public static bool IsValid(int id) => id >= 0 && id < Defs.Length;

        public static string Name(SkillRarity rarity) => RarityNames[(int)rarity];

        public static Color Color(SkillRarity rarity) => RarityColors[(int)rarity];

        public static double RarityChance(SkillRarity rarity) => RarityWeights[(int)rarity];

        /// <summary>
        /// 등급을 먼저 뽑고 그 등급 안에서 균등하게 스킬을 고른다.
        /// 등급별 스킬 수가 달라도 등급 확률이 흔들리지 않게 하려는 2단 추첨이다.
        /// </summary>
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

        /// <summary>
        /// 자동 장착 정렬 기준. <b>희귀도가 먼저</b>고 같은 등급 안에서만 배수로 가린다.
        /// 배수 상한(11)보다 등급 가중치(100)가 훨씬 커서 등급 역전이 일어나지 않는다.
        /// </summary>
        public static double Strength(int id)
        {
            if (!IsValid(id)) return double.NegativeInfinity;
            var def = Get(id);
            return (int)def.rarity * 100.0 + def.damageMultiplier;
        }

        public static SkillRarity RollRarity(double roll01)
        {
            double cursor = 0;
            for (int i = 0; i < RarityWeights.Length; i++)
            {
                cursor += RarityWeights[i];
                if (roll01 < cursor) return (SkillRarity)i;
            }
            return SkillRarity.Legend;
        }

        public static int ClampLevel(int level)
        {
            if (level < 0) return 0;
            return level > MaxLevel ? MaxLevel : level;
        }

        /// <summary>미보유는 0, 첫 획득은 1. 장착 스킬의 피해에만 곱한다.</summary>
        public static double LevelFactor(int level)
        {
            int lv = ClampLevel(level);
            if (lv <= 0) return 0;
            return 1.0 + LevelGrowth * (lv - 1);
        }

        public static double ScaledDamage(int id, int level)
            => Get(id).damageMultiplier * LevelFactor(level);

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
    }
}
