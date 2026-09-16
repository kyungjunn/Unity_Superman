using System;
using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Gear
{
    public class Inventory : MonoBehaviour
    {
        readonly GearStock stock = new GearStock();
        Wallet wallet;
        PlayerStats stats;
        Loadout loadout;
        IGameRandom rng;

        public event Action Changed;
        public event Action<GearSlot, GearTier> CodexRegistered;

        public GearStock Stock => stock;
        public int CodexCount => stock.CodexCount;
        public double CodexMultiplier => stock.CodexMultiplier;

        public void Bind(Wallet boundWallet, PlayerStats boundStats, Loadout boundLoadout, IGameRandom boundRandom)
        {
            wallet = boundWallet;
            stats = boundStats;
            loadout = boundLoadout;
            rng = boundRandom;
        }

        public void PushDerived() => PushCodex();

        public void Equip(GearItem item)
        {
            if (!item.owned) return;
            Register(item);

            if (loadout == null) stock.Add(item.slot, item.tier);
            else
            {
                var replaced = loadout.ForceEquip(item);
                if (replaced.owned) stock.Add(replaced.slot, replaced.tier);
            }

            Changed?.Invoke();
        }

        public double Sell(GearItem item)
        {
            if (!item.owned) return 0;
            Register(item);
            double gold = item.SellGold;
            wallet?.Add(CurrencyKind.Gold, gold);
            Changed?.Invoke();
            return gold;
        }

        public double SellFromStock(GearSlot slot, GearTier tier, int count = 1)
        {
            if (!stock.TryRemove(slot, tier, count)) return 0;
            double gold = GearTable.SellGold(tier) * count;
            wallet?.Add(CurrencyKind.Gold, gold);
            Changed?.Invoke();
            return gold;
        }

        bool Register(GearItem item)
        {
            if (!stock.Register(item.slot, item.tier)) return false;
            CodexRegistered?.Invoke(item.slot, item.tier);
            PushCodex();
            return true;
        }

        public void Acquire(GearItem item)
        {
            if (!item.owned) return;
            Register(item);

            if (loadout != null && loadout.Acquire(item, out var replaced))
            {
                if (replaced.owned) stock.Add(replaced.slot, replaced.tier);
            }
            else
            {
                stock.Add(item.slot, item.tier);
            }

            Changed?.Invoke();
        }

        public double Dismantle(GearSlot slot, GearTier tier, int count = 1)
        {
            if (!stock.TryRemove(slot, tier, count)) return 0;
            return Payout(GearTable.Shard(tier) * count);
        }

        public double DismantleUpTo(GearTier maxTier) => Payout(stock.DismantleUpTo(maxTier));

        public bool CanFuse(GearSlot slot, GearTier tier) => stock.CanFuse(slot, tier);

        public bool Fuse(GearSlot slot, GearTier tier)
        {
            if (!stock.TryFuse(slot, tier, GearLevelStamp.Of(stats), out var result)) return false;

            if (loadout != null && loadout.Acquire(result, out var replaced))
            {
                stock.TryRemove(result.slot, result.tier);
                if (replaced.owned) stock.Add(replaced.slot, replaced.tier);
            }

            PushCodex();
            Changed?.Invoke();
            return true;
        }

        public bool CanExchange(GearTier tier)
        {
            double cost = LampTable.ShardExchangeCost(tier);
            return cost > 0 && wallet != null && wallet.Shard >= cost;
        }

        public bool ExchangeShard(GearTier tier)
        {
            double cost = LampTable.ShardExchangeCost(tier);
            if (cost <= 0 || wallet == null) return false;
            if (!wallet.TrySpend(CurrencyKind.Shard, cost)) return false;

            int slot = rng != null ? (int)(rng.Value01() * GearTable.SlotCount) : 0;
            if (slot >= GearTable.SlotCount) slot = GearTable.SlotCount - 1;
            Acquire(GearItem.At((GearSlot)slot, tier, GearLevelStamp.Of(stats)));
            return true;
        }

        public void LoadFrom(int[] savedCounts, bool[] savedCodex)
        {
            stock.LoadFrom(savedCounts, savedCodex);
            PushCodex();
            Changed?.Invoke();
        }

        public int[] CountsSnapshot() => stock.CountsSnapshot();

        public bool[] CodexSnapshot() => stock.CodexSnapshot();

        double Payout(double shard)
        {
            if (shard <= 0) return 0;
            wallet?.Add(CurrencyKind.Shard, shard);
            Changed?.Invoke();
            return shard;
        }

        void PushCodex() => stats?.SetCodexMultiplier(stock.CodexMultiplier);
    }
}
