using System;
using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Gear
{
    public class Loadout : MonoBehaviour
    {
        public static Loadout Instance { get; private set; }

        public event Action<GearSlot, GearItem> SlotChanged;
        public event Action Changed;

        readonly GearItem[] equipped = new GearItem[GearTable.SlotCount];

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start() => PushBonus();

        public GearItem Get(GearSlot slot) => equipped[(int)slot];

        public int EquippedCount
        {
            get
            {
                int count = 0;
                foreach (var item in equipped) if (item.owned) count++;
                return count;
            }
        }

        public double TotalPower
        {
            get
            {
                double sum = 0;
                foreach (var item in equipped) sum += item.Power;
                return sum;
            }
        }

        public int SameTierCount(GearTier tier)
        {
            int count = 0;
            foreach (var item in equipped) if (item.owned && item.tier == tier) count++;
            return count;
        }

        public double SetMultiplier()
        {
            for (int t = GearTable.TierCount - 1; t >= 0; t--)
            {
                int count = SameTierCount((GearTier)t);
                if (count >= GearTable.SlotCount) return 1.40;
                if (count >= 4) return 1.15;
            }
            return 1.0;
        }

        public bool Acquire(GearItem item)
        {
            if (!item.owned) return false;
            var slotIndex = (int)item.slot;
            if (!item.StrongerThan(equipped[slotIndex])) return false;

            equipped[slotIndex] = item;
            SlotChanged?.Invoke(item.slot, item);
            PushBonus();
            Changed?.Invoke();
            return true;
        }

        public void LoadFrom(GearItem[] saved)
        {
            if (saved == null) return;
            for (int i = 0; i < equipped.Length && i < saved.Length; i++)
            {
                equipped[i] = saved[i];
                if (equipped[i].owned) equipped[i].slot = (GearSlot)i;
                SlotChanged?.Invoke((GearSlot)i, equipped[i]);
            }
            PushBonus();
            Changed?.Invoke();
        }

        public GearItem[] Snapshot() => (GearItem[])equipped.Clone();

        public EquipmentBonus CurrentBonus()
        {
            double offense = 0, defense = 0, utility = 0;
            foreach (var item in equipped)
            {
                if (!item.owned) continue;
                if (GearTable.IsOffense(item.slot)) offense += item.Power;
                else if (GearTable.IsDefense(item.slot)) defense += item.Power;
                else utility += item.Power;
            }

            double set = SetMultiplier();
            return new EquipmentBonus
            {
                attackMultiplier = (1.0 + offense * 0.05 + utility * 0.01) * set,
                healthMultiplier = (1.0 + defense * 0.04 + utility * 0.01) * set,
                defenseMultiplier = (1.0 + defense * 0.03) * set,
            };
        }

        void PushBonus() => PlayerStats.Instance?.SetEquipmentBonus(CurrentBonus());
    }
}
