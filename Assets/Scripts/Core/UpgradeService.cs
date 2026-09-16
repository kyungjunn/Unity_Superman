using System;
using UnityEngine;

namespace GrowNa.Core
{
    public class UpgradeService : MonoBehaviour
    {
        public event Action Changed;

        Wallet wallet;
        PlayerStats stats;

        public void Bind(Wallet boundWallet, PlayerStats boundStats)
        {
            wallet = boundWallet;
            stats = boundStats;
        }

        public double CostOf(StatKind kind)
            => UpgradeTable.Cost(kind, stats == null ? 0 : stats.UpgradeLevel(kind));

        public bool CanAfford(StatKind kind)
            => wallet != null && wallet.Gold >= CostOf(kind);

        public bool TryUpgrade(StatKind kind)
        {
            if (wallet == null || stats == null) return false;
            if (!wallet.TrySpend(CurrencyKind.Gold, CostOf(kind))) return false;

            stats.ApplyUpgrade(kind);
            Changed?.Invoke();
            return true;
        }

        public int TryUpgradeMax(StatKind kind, int limit = 50)
        {
            int applied = 0;
            while (applied < limit && TryUpgrade(kind)) applied++;
            return applied;
        }
    }
}
