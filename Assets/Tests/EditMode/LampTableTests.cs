using GrowNa.Gear;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class LampTableTests
    {
        [Test]
        public void Weights_always_sum_to_one()
        {
            for (int level = LampTable.MinLevel; level <= LampTable.MaxLevel; level++)
            {
                double sum = 0;
                foreach (double w in LampTable.Weights(level)) sum += w;
                Assert.AreEqual(1.0, sum, 1e-9, $"등불 Lv.{level} 확률 합이 1이 아니다");
            }
        }

        [Test]
        public void Weights_match_gdd_anchor_rows()
        {
            var level1 = LampTable.Weights(1);
            Assert.AreEqual(0.70, level1[(int)GearTier.Worn], 1e-9);
            Assert.AreEqual(0.25, level1[(int)GearTier.Decent], 1e-9);
            Assert.AreEqual(0.05, level1[(int)GearTier.Rare], 1e-9);

            var level200 = LampTable.Weights(LampTable.MaxLevel);
            Assert.AreEqual(0.10, level200[(int)GearTier.Heroic], 1e-9);
            Assert.AreEqual(0.04, level200[(int)GearTier.Earth], 1e-9);
            Assert.AreEqual(0.0, level200[(int)GearTier.Worn], 1e-9);
        }

        [Test]
        public void Legend_appears_at_level_fifteen_and_grows_to_sixty()
        {
            Assert.AreEqual(0.0, LampTable.TierChance(5, GearTier.Legend), 1e-9);
            Assert.AreEqual(0.005, LampTable.TierChance(15, GearTier.Legend), 1e-9);
            Assert.Greater(LampTable.TierChance(60, GearTier.Legend), LampTable.TierChance(30, GearTier.Legend));
        }

        [Test]
        public void Unreleased_tier_stays_at_zero_until_its_anchor_level()
        {
            for (int level = 1; level < 15; level++)
                Assert.AreEqual(0.0, LampTable.TierChance(level, GearTier.Legend), 1e-9,
                    $"Lv.{level} 에서 전설이 미리 새어나왔다");

            for (int level = 1; level < 100; level++)
                Assert.AreEqual(0.0, LampTable.TierChance(level, GearTier.Spirit), 1e-9,
                    $"Lv.{level} 에서 곡령이 미리 새어나왔다");

            Assert.Greater(LampTable.TierChance(15, GearTier.Legend), 0.0);
            Assert.Greater(LampTable.TierChance(100, GearTier.Spirit), 0.0);
        }

        [Test]
        public void Highest_tier_never_regresses_with_level()
        {
            var previous = GearTier.Worn;
            for (int level = LampTable.MinLevel; level <= LampTable.MaxLevel; level++)
            {
                var current = LampTable.HighestTier(level);
                Assert.GreaterOrEqual((int)current, (int)previous, $"Lv.{level} 에서 최고 티어가 내려갔다");
                previous = current;
            }
        }

        [Test]
        public void Roll_zero_returns_first_available_tier()
            => Assert.AreEqual(GearTier.Worn, LampTable.Roll(LampTable.Weights(1), 0.0));

        [Test]
        public void Roll_near_one_returns_highest_available_tier()
            => Assert.AreEqual(GearTier.Rare, LampTable.Roll(LampTable.Weights(1), 0.9999));

        [Test]
        public void Roll_respects_cumulative_boundaries()
        {
            var weights = LampTable.Weights(1);
            Assert.AreEqual(GearTier.Worn, LampTable.Roll(weights, 0.699));
            Assert.AreEqual(GearTier.Decent, LampTable.Roll(weights, 0.701));
            Assert.AreEqual(GearTier.Decent, LampTable.Roll(weights, 0.949));
            Assert.AreEqual(GearTier.Rare, LampTable.Roll(weights, 0.951));
        }

        [Test]
        public void Offer_cost_is_one_grain_per_pull()
        {
            Assert.AreEqual(1, LampTable.OfferCost(1));
            Assert.AreEqual(3, LampTable.OfferCost(3));
            Assert.AreEqual(LampTable.MaxAutoBatch, LampTable.OfferCost(LampTable.MaxAutoBatch));
        }

        [Test]
        public void Offer_cost_never_goes_negative()
        {
            Assert.AreEqual(0, LampTable.OfferCost(0));
            Assert.AreEqual(0, LampTable.OfferCost(-5));
        }

        [TestCase(1, 2)]
        [TestCase(30, 20)]
        [TestCase(31, 30)]
        [TestCase(60, 80)]
        [TestCase(61, 100)]
        [TestCase(100, 250)]
        [TestCase(151, 900)]
        public void Wick_cost_matches_band_endpoints(int level, double expected)
            => Assert.AreEqual(expected, LampTable.WickCost(level));

        [Test]
        public void Max_level_costs_nothing_further()
        {
            Assert.AreEqual(0, LampTable.WickCost(LampTable.MaxLevel));
            Assert.AreEqual(0, LampTable.GoldCost(LampTable.MaxLevel));
        }

        [Test]
        public void Gold_cost_increases_with_level()
            => Assert.Greater(LampTable.GoldCost(80), LampTable.GoldCost(20));
    }
}
