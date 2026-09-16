using System;
using System.Collections.Generic;
using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Gear
{
    public class LampService : MonoBehaviour
    {
        [SerializeField] int lampLevel = 1;
        [SerializeField] int offeringsSincePity;
        [SerializeField] int totalOfferings;

        Wallet wallet;
        PlayerStats stats;
        QuestService quests;
        IGameRandom rng;

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
        public int OfferingsSincePity => offeringsSincePity;

        public void Bind(Wallet boundWallet, PlayerStats boundStats, QuestService boundQuests, IGameRandom boundRandom)
        {
            wallet = boundWallet;
            stats = boundStats;
            quests = boundQuests;
            rng = boundRandom;
        }

        public bool CanOffer(int count) => wallet != null
            && wallet.Grain >= LampTable.OfferCost(count);

        public List<GearItem> Offer(int count)
        {
            if (wallet == null || count <= 0) return null;
            if (!wallet.TrySpend(CurrencyKind.Grain, LampTable.OfferCost(count))) return null;

            var weights = LampTable.Weights(lampLevel);
            var results = new List<GearItem>(count);
            bool pityHit = false;
            int itemLevel = GearLevelStamp.Of(stats);

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
                    tier = LampTable.Roll(weights, rng != null ? rng.Value01() : 0);
                }

                int slotRoll = rng != null ? (int)(rng.Value01() * GearTable.SlotCount) : 0;
                if (slotRoll >= GearTable.SlotCount) slotRoll = GearTable.SlotCount - 1;
                results.Add(GearItem.At((GearSlot)slotRoll, tier, itemLevel));
            }

            quests?.Report(QuestKind.Offer, results.Count);
            Offered?.Invoke(results, pityHit);
            Changed?.Invoke();
            return results;
        }

        public bool TryOfferOne(out GearItem item)
        {
            item = GearItem.Empty;
            var results = Offer(1);
            if (results == null || results.Count == 0) return false;
            item = results[0];
            return true;
        }

        public bool CanUpgrade()
        {
            return wallet != null
                && lampLevel < LampTable.MaxLevel
                && wallet.Wick >= WickCost
                && wallet.Gold >= GoldCost;
        }

        public bool TryUpgrade()
        {
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
    }
}
