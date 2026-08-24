using System;
using UnityEngine;

namespace GrowNa.Core
{
    public class UpgradeService : MonoBehaviour
    {
        public static UpgradeService Instance { get; private set; }

        public event Action Changed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public double CostOf(StatKind kind)
        {
            var stats = PlayerStats.Instance;
            return UpgradeTable.Cost(kind, stats == null ? 0 : stats.UpgradeLevel(kind));
        }

        public bool CanAfford(StatKind kind)
        {
            var wallet = Wallet.Instance;
            return wallet != null && wallet.Gold >= CostOf(kind);
        }

        public bool TryUpgrade(StatKind kind)
        {
            var wallet = Wallet.Instance;
            var stats = PlayerStats.Instance;
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
