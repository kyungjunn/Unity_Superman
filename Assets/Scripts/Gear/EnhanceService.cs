using System;
using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Gear
{
    public class EnhanceService : MonoBehaviour
    {
        [SerializeField] int[] failStreak = new int[GearTable.SlotCount];

        Wallet wallet;
        Loadout loadout;
        IGameRandom rng;

        public event Action Changed;
        public event Action<GearSlot, bool> Attempted;

        public void Bind(Wallet boundWallet, Loadout boundLoadout, IGameRandom boundRandom)
        {
            wallet = boundWallet;
            loadout = boundLoadout;
            rng = boundRandom;
        }

        public int FailStreak(GearSlot slot) => failStreak[(int)slot];

        public double CostOf(GearSlot slot)
        {
            var item = loadout == null ? GearItem.Empty : loadout.Get(slot);
            return item.owned ? EnhanceTable.Cost(item.tier, item.plus) : 0;
        }

        public double ChanceOf(GearSlot slot)
        {
            var item = loadout == null ? GearItem.Empty : loadout.Get(slot);
            return item.owned ? EnhanceTable.EffectiveChance(item.plus, FailStreak(slot)) : 0;
        }

        public bool CanEnhance(GearSlot slot)
        {
            if (loadout == null || wallet == null) return false;
            var item = loadout.Get(slot);
            return item.owned && !EnhanceTable.IsMax(item.plus) && wallet.Gold >= CostOf(slot);
        }

        public bool TryEnhance(GearSlot slot)
        {
            if (!CanEnhance(slot)) return false;
            if (!wallet.TrySpend(CurrencyKind.Gold, CostOf(slot))) return false;

            int index = (int)slot;
            var item = loadout.Get(slot);
            double roll = rng != null ? rng.Value01() : 0;
            bool success = roll < EnhanceTable.EffectiveChance(item.plus, failStreak[index]);

            if (success)
            {
                failStreak[index] = 0;
                loadout.RaisePlus(slot);
            }
            else
            {
                failStreak[index]++;
            }

            Attempted?.Invoke(slot, success);
            Changed?.Invoke();
            return success;
        }

        public void LoadFrom(int[] savedStreaks)
        {
            if (savedStreaks != null)
                for (int i = 0; i < failStreak.Length && i < savedStreaks.Length; i++)
                    failStreak[i] = Math.Max(0, savedStreaks[i]);
            Changed?.Invoke();
        }

        public int[] Snapshot() => (int[])failStreak.Clone();
    }
}
