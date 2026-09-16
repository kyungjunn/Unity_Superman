using System;

namespace GrowNa.Core
{
    [Serializable]
    public struct CombatSnapshot
    {
        public string displayName;
        public int level;
        public int evolutionStage;
        public double maxHp;
        public double attack;
        public double attackSpeed;
        public double defense;
        public double critChance;
        public double critMultiplier;

        public double Power =>
            (attack * attackSpeed * (1.0 + critChance * (critMultiplier - 1.0)) * 6.0)
            + (maxHp * 0.35)
            + (defense * 4.0);

        public static CombatSnapshot Default(string name) => new CombatSnapshot
        {
            displayName = name,
            level = 1,
            evolutionStage = 1,
            maxHp = 240,
            attack = 14,
            attackSpeed = 1.25,
            defense = 5,
            critChance = 0.15,
            critMultiplier = 1.8,
        };
    }
}
