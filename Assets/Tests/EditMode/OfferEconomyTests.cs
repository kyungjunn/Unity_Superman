using GrowNa.Core;
using GrowNa.Gear;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class OfferEconomyTests
    {
        static GearItem[] EmptySet() => new GearItem[GearTable.SlotCount];

        [Test]
        public void A_new_account_can_afford_its_first_offering()
            => Assert.GreaterOrEqual(Wallet.StartingGrain, LampTable.OfferCost(1));

        [Test]
        public void The_starting_grain_covers_more_than_one_pull()
            => Assert.GreaterOrEqual(Wallet.StartingGrain, LampTable.OfferCost(2));

        [Test]
        public void Sell_gold_tracks_tier_power()
        {
            for (int t = 0; t < GearTable.TierCount; t++)
                Assert.AreEqual(GearTable.Power((GearTier)t) * 50.0, GearTable.SellGold((GearTier)t), 1e-9);
        }

        [Test]
        public void Ten_worn_sales_cover_the_first_wick_swap()
            => Assert.GreaterOrEqual(GearTable.SellGold(GearTier.Worn) * 10.0, LampTable.GoldCost(1));

        [Test]
        public void Enhanced_item_sells_higher()
        {
            var plain = GearItem.Of(GearSlot.Ring, GearTier.Rare);
            var plus5 = GearItem.Of(GearSlot.Ring, GearTier.Rare, 5);
            Assert.AreEqual(plain.SellGold * 1.4, plus5.SellGold, 1e-9);
        }

        [Test]
        public void Empty_item_sells_for_nothing()
            => Assert.AreEqual(0.0, GearItem.Empty.SellGold, 1e-9);

        [Test]
        public void Empty_loadout_gives_neutral_bonus()
        {
            var bonus = Loadout.BonusOf(EmptySet());
            Assert.AreEqual(1.0, bonus.attackMultiplier, 1e-9);
            Assert.AreEqual(1.0, bonus.healthMultiplier, 1e-9);
            Assert.AreEqual(1.0, bonus.defenseMultiplier, 1e-9);
        }

        [Test]
        public void Offense_slot_raises_attack_only()
        {
            var set = EmptySet();
            set[(int)GearSlot.Weapon] = GearItem.Of(GearSlot.Weapon, GearTier.Rare);
            var bonus = Loadout.BonusOf(set);

            Assert.Greater(bonus.attackMultiplier, 1.0);
            Assert.AreEqual(1.0, bonus.healthMultiplier, 1e-9);
            Assert.AreEqual(1.0, bonus.defenseMultiplier, 1e-9);
        }

        [Test]
        public void Defense_slot_raises_health_and_defense()
        {
            var set = EmptySet();
            set[(int)GearSlot.Armor] = GearItem.Of(GearSlot.Armor, GearTier.Rare);
            var bonus = Loadout.BonusOf(set);

            Assert.AreEqual(1.0, bonus.attackMultiplier, 1e-9);
            Assert.Greater(bonus.healthMultiplier, 1.0);
            Assert.Greater(bonus.defenseMultiplier, 1.0);
        }

        [Test]
        public void Higher_tier_in_the_same_slot_beats_the_lower_one()
        {
            var rare = EmptySet();
            rare[(int)GearSlot.Weapon] = GearItem.Of(GearSlot.Weapon, GearTier.Rare);
            var legend = EmptySet();
            legend[(int)GearSlot.Weapon] = GearItem.Of(GearSlot.Weapon, GearTier.Legend);

            Assert.Greater(Loadout.BonusOf(legend).attackMultiplier, Loadout.BonusOf(rare).attackMultiplier);
        }

        [Test]
        public void Set_multiplier_steps_at_four_and_eight()
        {
            var set = EmptySet();
            Assert.AreEqual(1.0, Loadout.SetMultiplierOf(set), 1e-9);

            for (int i = 0; i < 4; i++) set[i] = GearItem.Of((GearSlot)i, GearTier.Legend);
            Assert.AreEqual(1.15, Loadout.SetMultiplierOf(set), 1e-9);

            for (int i = 4; i < GearTable.SlotCount; i++) set[i] = GearItem.Of((GearSlot)i, GearTier.Legend);
            Assert.AreEqual(1.40, Loadout.SetMultiplierOf(set), 1e-9);
        }

        [Test]
        public void Mixed_tiers_do_not_trigger_the_set_bonus()
        {
            var set = EmptySet();
            for (int i = 0; i < GearTable.SlotCount; i++)
                set[i] = GearItem.Of((GearSlot)i, (GearTier)(i % GearTable.TierCount));

            Assert.AreEqual(1.0, Loadout.SetMultiplierOf(set), 1e-9);
        }
    }
}
