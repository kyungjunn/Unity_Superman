using System;
using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Gear
{
    public class Loadout : MonoBehaviour
    {
        public event Action<GearSlot, GearItem> SlotChanged;
        public event Action Changed;

        readonly GearItem[] equipped = new GearItem[GearTable.SlotCount];
        PlayerStats stats;

        public void Bind(PlayerStats boundStats) => stats = boundStats;

        public void PushDerived() => PushBonus();

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

        public double SetMultiplier() => SetMultiplierOf(equipped);

        public static double SetMultiplierOf(GearItem[] set)
        {
            for (int t = GearTable.TierCount - 1; t >= 0; t--)
            {
                int count = 0;
                foreach (var item in set)
                    if (item.owned && item.tier == (GearTier)t) count++;
                if (count >= GearTable.SlotCount) return 1.40;
                if (count >= 4) return 1.15;
            }
            return 1.0;
        }

        public static EquipmentBonus BonusOf(GearItem[] set)
        {
            double offense = 0, defense = 0, utility = 0;
            foreach (var item in set)
            {
                if (!item.owned) continue;
                if (GearTable.IsOffense(item.slot)) offense += item.Power;
                else if (GearTable.IsDefense(item.slot)) defense += item.Power;
                else utility += item.Power;
            }

            double setBonus = SetMultiplierOf(set);
            return new EquipmentBonus
            {
                attackMultiplier = (1.0 + offense * 0.05 + utility * 0.01) * setBonus,
                healthMultiplier = (1.0 + defense * 0.04 + utility * 0.01) * setBonus,
                defenseMultiplier = (1.0 + defense * 0.03) * setBonus,
            };
        }

        public EquipmentBonus BonusWith(GearItem candidate)
        {
            if (!candidate.owned) return CurrentBonus();
            var preview = (GearItem[])equipped.Clone();
            preview[(int)candidate.slot] = candidate;
            return BonusOf(preview);
        }

        public bool Acquire(GearItem item) => Acquire(item, out _);

        public bool Acquire(GearItem item, out GearItem replaced)
        {
            replaced = GearItem.Empty;
            if (!item.owned) return false;
            var slotIndex = (int)item.slot;
            if (!item.StrongerThan(equipped[slotIndex])) return false;

            replaced = equipped[slotIndex];
            equipped[slotIndex] = item;
            SlotChanged?.Invoke(item.slot, item);
            PushBonus();
            Changed?.Invoke();
            return true;
        }

        public GearItem ForceEquip(GearItem item)
        {
            if (!item.owned) return GearItem.Empty;
            int i = (int)item.slot;
            var replaced = equipped[i];
            equipped[i] = item;
            SlotChanged?.Invoke(item.slot, item);
            PushBonus();
            Changed?.Invoke();
            return replaced;
        }

        public bool RaisePlus(GearSlot slot)
        {
            int i = (int)slot;
            if (!equipped[i].owned || EnhanceTable.IsMax(equipped[i].plus)) return false;

            equipped[i].plus++;
            SlotChanged?.Invoke(slot, equipped[i]);
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

        public EquipmentBonus CurrentBonus() => BonusOf(equipped);

        void PushBonus() => stats?.SetEquipmentBonus(CurrentBonus());
    }
}
