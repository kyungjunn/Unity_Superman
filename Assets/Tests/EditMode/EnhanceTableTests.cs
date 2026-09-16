using GrowNa.Gear;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class EnhanceTableTests
    {
        [TestCase(0, 1.00)]
        [TestCase(4, 1.00)]
        [TestCase(5, 0.70)]
        [TestCase(9, 0.70)]
        [TestCase(10, 0.40)]
        [TestCase(14, 0.40)]
        [TestCase(15, 0.15)]
        [TestCase(19, 0.15)]
        public void Success_chance_follows_gdd_bands(int plus, double expected)
            => Assert.AreEqual(expected, EnhanceTable.SuccessChance(plus), 1e-9);

        [Test]
        public void Max_plus_cannot_be_enhanced()
        {
            Assert.IsTrue(EnhanceTable.IsMax(EnhanceTable.MaxPlus));
            Assert.AreEqual(0, EnhanceTable.SuccessChance(EnhanceTable.MaxPlus), 1e-9);
            Assert.AreEqual(0, EnhanceTable.Cost(GearTier.Legend, EnhanceTable.MaxPlus), 1e-9);
            Assert.AreEqual(0, EnhanceTable.EffectiveChance(EnhanceTable.MaxPlus, 99), 1e-9);
        }

        [Test]
        public void Three_failures_guarantee_the_next_attempt()
        {
            Assert.AreEqual(0.15, EnhanceTable.EffectiveChance(15, 2), 1e-9);
            Assert.AreEqual(1.0, EnhanceTable.EffectiveChance(15, EnhanceTable.FailStreakForGuarantee), 1e-9);
            Assert.AreEqual(1.0, EnhanceTable.EffectiveChance(15, 10), 1e-9);
        }

        [Test]
        public void Cost_rises_with_plus_within_a_band()
            => Assert.Greater(EnhanceTable.Cost(GearTier.Rare, 3), EnhanceTable.Cost(GearTier.Rare, 0));

        [Test]
        public void Cost_jumps_at_band_boundary()
        {
            double last = EnhanceTable.Cost(GearTier.Rare, 4);
            double first = EnhanceTable.Cost(GearTier.Rare, 5);
            Assert.Greater(first, last * 3);
        }

        [Test]
        public void Cost_scales_with_tier_power()
            => Assert.Greater(EnhanceTable.Cost(GearTier.Legend, 0), EnhanceTable.Cost(GearTier.Worn, 0));

        [Test]
        public void Power_multiplier_is_eight_percent_per_plus()
        {
            Assert.AreEqual(1.0, EnhanceTable.PowerMultiplier(0), 1e-9);
            Assert.AreEqual(1.8, EnhanceTable.PowerMultiplier(10), 1e-9);
        }

        [Test]
        public void Enhanced_item_power_matches_the_table()
        {
            var item = GearItem.Of(GearSlot.Weapon, GearTier.Legend, 5);
            Assert.AreEqual(GearTable.Power(GearTier.Legend) * EnhanceTable.PowerMultiplier(5), item.Power, 1e-9);
        }

        [Test]
        public void Negative_plus_is_treated_as_zero()
        {
            Assert.AreEqual(1.0, EnhanceTable.SuccessChance(-3), 1e-9);
            Assert.AreEqual(EnhanceTable.Cost(GearTier.Rare, 0), EnhanceTable.Cost(GearTier.Rare, -3), 1e-9);
        }
    }
}
