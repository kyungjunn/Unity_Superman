using System;
using UnityEngine;

namespace GrowNa.Core
{
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [SerializeField] string displayName = "허수아비";
        [SerializeField] int level = 1;
        [SerializeField] int evolutionStage = 1;
        [SerializeField] int[] upgradeLevels = new int[3];

        CombatSnapshot baseStats = CombatSnapshot.Default("허수아비");
        EquipmentBonus equipment = EquipmentBonus.None;
        double currentHp;
        bool initialized;

        public event Action HpChanged;
        public event Action StatsChanged;

        public string DisplayName => displayName;
        public int Level => level;
        public int EvolutionStage => evolutionStage;
        public double CurrentHp => currentHp;
        public bool IsDead => currentHp <= 0;

        public double MaxHp => Snapshot.maxHp;
        public double AttackSpeed => Snapshot.attackSpeed;
        public double Power => Snapshot.Power;
        public double HpRatio => MaxHp <= 0 ? 0 : Mathf.Clamp01((float)(currentHp / MaxHp));

        public int UpgradeLevel(StatKind kind) => upgradeLevels[(int)kind];

        public CombatSnapshot Snapshot
        {
            get
            {
                double levelPower = level - 1;
                var snap = baseStats;
                snap.displayName = displayName;
                snap.level = level;
                snap.evolutionStage = evolutionStage;
                snap.attack = baseStats.attack * Math.Pow(1.06, levelPower)
                            * UpgradeTable.Bonus(StatKind.Attack, UpgradeLevel(StatKind.Attack))
                            * equipment.attackMultiplier;
                snap.maxHp = baseStats.maxHp * Math.Pow(1.05, levelPower)
                           * UpgradeTable.Bonus(StatKind.Health, UpgradeLevel(StatKind.Health))
                           * equipment.healthMultiplier;
                snap.defense = baseStats.defense * Math.Pow(1.04, levelPower) * equipment.defenseMultiplier;
                snap.critChance = Math.Min(UpgradeTable.CritChanceCap,
                    baseStats.critChance + UpgradeTable.Bonus(StatKind.Crit, UpgradeLevel(StatKind.Crit)));
                return snap;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (!initialized)
            {
                currentHp = MaxHp;
                initialized = true;
            }
        }

        public double RollDamage(out bool critical)
        {
            var snap = Snapshot;
            critical = UnityEngine.Random.value < snap.critChance;
            double raw = snap.attack * UnityEngine.Random.Range(0.92f, 1.08f);
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

        public void GrantLevel(int amount = 1)
        {
            level += amount;
            currentHp = MaxHp;
            StatsChanged?.Invoke();
            HpChanged?.Invoke();
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

        public void SetEquipmentBonus(EquipmentBonus bonus)
        {
            double hpRatioBefore = HpRatio;
            equipment = bonus;
            currentHp = MaxHp * hpRatioBefore;
            StatsChanged?.Invoke();
            HpChanged?.Invoke();
        }

        public void LoadFrom(int savedLevel, int savedEvolution, int[] savedUpgrades, double savedHp)
        {
            level = Math.Max(1, savedLevel);
            evolutionStage = Math.Max(1, savedEvolution);
            if (savedUpgrades != null)
                for (int i = 0; i < upgradeLevels.Length && i < savedUpgrades.Length; i++)
                    upgradeLevels[i] = Math.Max(0, savedUpgrades[i]);
            initialized = true;
            currentHp = savedHp > 0 ? Math.Min(savedHp, MaxHp) : MaxHp;
            StatsChanged?.Invoke();
            HpChanged?.Invoke();
        }

        public int[] UpgradeLevelsCopy() => (int[])upgradeLevels.Clone();
    }
}
