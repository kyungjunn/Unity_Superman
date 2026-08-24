using System;
using System.Collections.Generic;
using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Gear
{
    public class LampService : MonoBehaviour
    {
        public static LampService Instance { get; private set; }

        [SerializeField] int lampLevel = 1;
        [SerializeField] int offeringsSincePity;
        [SerializeField] int totalOfferings;

        public event Action Changed;
        public event Action<List<GearItem>, bool> Offered;

        public int LampLevel => lampLevel;
        public int TotalOfferings => totalOfferings;
        public int OfferingsUntilPity => Math.Max(0, LampTable.PityThreshold - offeringsSincePity);
        public GearTier HighestTier => LampTable.HighestTier(lampLevel);
        public double LegendChance => LampTable.TierChance(lampLevel, GearTier.Legend);
        public double NextLegendChance => LampTable.TierChance(Math.Min(LampTable.MaxLevel, lampLevel + 1), GearTier.Legend);
        public double WickCost => LampTable.WickCost(lampLevel);
        public double GoldCost => LampTable.GoldCost(lampLevel);
        public Color FlameColor => LampTable.FlameColor(lampLevel);

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public bool CanOffer(int count) => Wallet.Instance != null
            && Wallet.Instance.Grain >= LampTable.OfferCost(count);

        public List<GearItem> Offer(int count)
        {
            var wallet = Wallet.Instance;
            var loadout = Loadout.Instance;
            if (wallet == null || count <= 0) return null;
            if (!wallet.TrySpend(CurrencyKind.Grain, LampTable.OfferCost(count))) return null;

            var weights = LampTable.Weights(lampLevel);
            var results = new List<GearItem>(count);
            bool pityHit = false;
            bool tenPull = count >= LampTable.TenOfferCount;
            bool floorMet = false;

            for (int i = 0; i < count; i++)
            {
                offeringsSincePity++;
                totalOfferings++;

                GearTier tier;
                if (offeringsSincePity >= LampTable.PityThreshold)
                {
                    tier = HighestTier;
                    offeringsSincePity = 0;
                    pityHit = true;
                }
                else
                {
                    tier = LampTable.Roll(weights, UnityEngine.Random.value);
                }

                bool isLast = i == count - 1;
                if (tenPull && isLast && !floorMet && tier < LampTable.TenOfferFloor)
                    tier = LampTable.TenOfferFloor;
                if (tier >= LampTable.TenOfferFloor) floorMet = true;

                var slot = (GearSlot)UnityEngine.Random.Range(0, GearTable.SlotCount);
                var item = GearItem.Of(slot, tier);
                results.Add(item);
                loadout?.Acquire(item);
            }

            Offered?.Invoke(results, pityHit);
            Changed?.Invoke();
            return results;
        }

        public bool CanUpgrade()
        {
            var wallet = Wallet.Instance;
            return wallet != null
                && lampLevel < LampTable.MaxLevel
                && wallet.Wick >= WickCost
                && wallet.Gold >= GoldCost;
        }

        public bool TryUpgrade()
        {
            var wallet = Wallet.Instance;
            if (!CanUpgrade() || wallet == null) return false;

            double wick = WickCost;
            double gold = GoldCost;
            if (!wallet.TrySpend(CurrencyKind.Wick, wick)) return false;
            if (!wallet.TrySpend(CurrencyKind.Gold, gold))
            {
                wallet.Add(CurrencyKind.Wick, wick);
                return false;
            }

            lampLevel++;
            Changed?.Invoke();
            return true;
        }

        public void LoadFrom(int level, int sincePity, int total)
        {
            lampLevel = LampTable.ClampLevel(level);
            offeringsSincePity = Math.Max(0, sincePity);
            totalOfferings = Math.Max(0, total);
            Changed?.Invoke();
        }

        public int OfferingsSincePity => offeringsSincePity;
    }
}
