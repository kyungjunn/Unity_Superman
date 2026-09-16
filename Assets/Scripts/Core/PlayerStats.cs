using System;
using UnityEngine;

namespace GrowNa.Core
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] string displayName = "액셀";
        [SerializeField] int level = 1;
        [SerializeField] int evolutionStage = 1;
        [SerializeField] double exp;
        [SerializeField] int[] upgradeLevels = new int[3];

        CombatSnapshot baseStats = CombatSnapshot.Default("액셀");
        EquipmentBonus equipment = EquipmentBonus.None;
        EquipmentBonus pet = EquipmentBonus.None;
        double codexMultiplier = 1.0;
        double currentHp;
        bool initialized;
        IGameRandom rng;

        public event Action HpChanged;
        public event Action StatsChanged;
        public event Action ExpChanged;
        public event Action<int> LeveledUp;

        public string DisplayName => displayName;
        public int Level => level;
        public int EvolutionStage => evolutionStage;
        public double Exp => exp;
        public double ExpRequired => ExpTable.Required(level);
        public double ExpRatio => ExpTable.Ratio(level, exp);
        public double CurrentHp => currentHp;
        public bool IsDead => currentHp <= 0;

        public double MaxHp => Snapshot.maxHp;
        public double AttackSpeed => Snapshot.attackSpeed;
        public double Power => Snapshot.Power;
        public double HpRatio => MaxHp <= 0 ? 0 : Mathf.Clamp01((float)(currentHp / MaxHp));

        public int UpgradeLevel(StatKind kind) => upgradeLevels[(int)kind];

        public CombatSnapshot Snapshot => SnapshotWith(equipment);

        public void BindRandom(IGameRandom boundRandom) => rng = boundRandom;

        public void EnsureInitialized()
        {
            if (initialized) return;
            currentHp = MaxHp;
            initialized = true;
        }

        public void ClampHpToMax()
        {
            currentHp = Math.Min(currentHp, MaxHp);
            initialized = true;
            HpChanged?.Invoke();
        }

        public CombatSnapshot SnapshotWith(EquipmentBonus bonus)
        {
            double levelPower = level - 1;
            var snap = baseStats;
            snap.displayName = displayName;
            snap.level = level;
            snap.evolutionStage = evolutionStage;
            snap.attack = baseStats.attack * Math.Pow(1.06, levelPower)
                        * UpgradeTable.Bonus(StatKind.Attack, UpgradeLevel(StatKind.Attack))
                        * bonus.attackMultiplier * codexMultiplier * pet.attackMultiplier;
            snap.maxHp = baseStats.maxHp * Math.Pow(1.05, levelPower)
                       * UpgradeTable.Bonus(StatKind.Health, UpgradeLevel(StatKind.Health))
                       * bonus.healthMultiplier * codexMultiplier * pet.healthMultiplier;
            snap.defense = baseStats.defense * Math.Pow(1.04, levelPower)
                         * bonus.defenseMultiplier * codexMultiplier * pet.defenseMultiplier;
            snap.critChance = Math.Min(UpgradeTable.CritChanceCap,
                baseStats.critChance + UpgradeTable.Bonus(StatKind.Crit, UpgradeLevel(StatKind.Crit)));
            return snap;
        }

        public double RollDamage(out bool critical)
        {
            var snap = Snapshot;
            double roll = rng != null ? rng.Value01() : 0;
            critical = roll < snap.critChance;
            float spread = rng != null ? rng.Range(0.92f, 1.08f) : 1f;
            double raw = snap.attack * spread;
            if (critical) raw *= snap.critMultiplier;
            return raw;
        }

        public void TakeDamage(double amount)
        {
            double reduced = Math.Max(1.0, amount - Snapshot.defense * 0.5);
            currentHp = Math.Max(0, currentHp - reduced);
            HpChanged?.Invoke();
        }

        public void Recover(double amount)
        {
            currentHp = Math.Min(MaxHp, currentHp + amount);
            HpChanged?.Invoke();
        }

        public void ReviveFull()
        {
            currentHp = MaxHp;
            HpChanged?.Invoke();
        }

        public void AddExp(double amount)
        {
            if (amount <= 0 || double.IsNaN(amount)) return;
            exp += amount;

            bool leveled = false;
            while (level < ExpTable.MaxLevel && exp >= ExpTable.Required(level))
            {
                exp -= ExpTable.Required(level);
                level++;
                leveled = true;
            }

            if (leveled) FinishLevelUp();
            ExpChanged?.Invoke();
        }

        public void GrantLevel(int amount = 1)
        {
            if (amount <= 0) return;
            level = Math.Min(ExpTable.MaxLevel, level + amount);
            exp = 0;
            FinishLevelUp();
            ExpChanged?.Invoke();
        }

        void FinishLevelUp()
        {
            currentHp = MaxHp;
            StatsChanged?.Invoke();
            HpChanged?.Invoke();
            LeveledUp?.Invoke(level);
        }

        public void ApplyUpgrade(StatKind kind)
        {
            int index = (int)kind;
            if (upgradeLevels[index] >= UpgradeTable.MaxLevel) return;
            double hpRatioBefore = HpRatio;
            upgradeLevels[index]++;
            currentHp = MaxHp * hpRatioBefore;
            StatsChanged?.Invoke();
            HpChanged?.Invoke();
        }

        public void SetCodexMultiplier(double multiplier)
        {
            double hpRatioBefore = HpRatio;
            codexMultiplier = Math.Max(1.0, multiplier);
            currentHp = MaxHp * hpRatioBefore;
            StatsChanged?.Invoke();
            HpChanged?.Invoke();
        }

        public void SetPetBonus(EquipmentBonus bonus)
        {
            double hpRatioBefore = HpRatio;
            pet = bonus;
            currentHp = MaxHp * hpRatioBefore;
            StatsChanged?.Invoke();
            HpChanged?.Invoke();
        }

        public void SetEquipmentBonus(EquipmentBonus bonus)
        {
            double hpRatioBefore = HpRatio;
            equipment = bonus;
            currentHp = MaxHp * hpRatioBefore;
            StatsChanged?.Invoke();
            HpChanged?.Invoke();
        }

        public void LoadFrom(int savedLevel, int savedEvolution, int[] savedUpgrades, double savedHp, double savedExp)
        {
            level = Math.Clamp(savedLevel, 1, ExpTable.MaxLevel);
            evolutionStage = Math.Max(1, savedEvolution);
            exp = savedExp > 0 ? savedExp : 0;
            if (savedUpgrades != null)
                for (int i = 0; i < upgradeLevels.Length && i < savedUpgrades.Length; i++)
                    upgradeLevels[i] = Math.Max(0, savedUpgrades[i]);
            initialized = true;
            currentHp = savedHp > 0 ? Math.Min(savedHp, MaxHp) : MaxHp;
            StatsChanged?.Invoke();
            HpChanged?.Invoke();
            ExpChanged?.Invoke();
        }

        public int[] UpgradeLevelsCopy() => (int[])upgradeLevels.Clone();
    }
}
