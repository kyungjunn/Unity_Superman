using GrowNa.Gear;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class GearLevelTests
    {
        [Test]
        public void A_plain_item_starts_at_the_minimum_level()
            => Assert.AreEqual(GearTable.MinItemLevel, GearItem.Of(GearSlot.Ring, GearTier.Rare).Level);

        [Test]
        public void Item_level_is_stamped_on_creation()
            => Assert.AreEqual(42, GearItem.At(GearSlot.Ring, GearTier.Rare, 42).Level);

        [Test]
        public void A_zero_level_from_an_old_save_reads_as_level_one()
        {
            var legacy = new GearItem { slot = GearSlot.Ring, tier = GearTier.Rare, owned = true, level = 0 };
            Assert.AreEqual(GearTable.MinItemLevel, legacy.Level);
            Assert.AreEqual(GearItem.Of(GearSlot.Ring, GearTier.Rare).Power, legacy.Power, 1e-9);
        }

        [Test]
        public void Same_level_means_rarity_decides()
        {
            for (int t = 1; t < GearTable.TierCount; t++)
                Assert.Greater(GearItem.At(GearSlot.Ring, (GearTier)t, 7).Power,
                               GearItem.At(GearSlot.Ring, (GearTier)(t - 1), 7).Power,
                               $"티어 {t} 가 {t - 1} 보다 강해야 한다");
        }

        [Test]
        public void A_level_five_common_beats_a_level_one_rare()
        {
            var common = GearItem.At(GearSlot.Ring, GearTier.Worn, 5);
            var rare = GearItem.At(GearSlot.Ring, GearTier.Rare, 1);
            Assert.Greater(common.Power, rare.Power);
        }

        [Test]
        public void Level_one_common_still_loses_to_level_one_rare()
        {
            var common = GearItem.At(GearSlot.Ring, GearTier.Worn, 1);
            var rare = GearItem.At(GearSlot.Ring, GearTier.Rare, 1);
            Assert.Less(common.Power, rare.Power);
        }

        [Test]
        public void Level_factor_starts_at_one_and_grows()
        {
            Assert.AreEqual(1.0, GearTable.LevelFactor(GearTable.MinItemLevel), 1e-9);
            Assert.AreEqual(GearTable.LevelPowerGrowth, GearTable.LevelFactor(2), 1e-9);
            Assert.Greater(GearTable.LevelFactor(10), GearTable.LevelFactor(9));
        }

        [Test]
        public void Level_growth_is_steep_enough_to_outrun_one_rarity_step()
        {
            double rarityGap = GearTable.Power(GearTier.Rare) / GearTable.Power(GearTier.Worn);
            Assert.Greater(GearTable.LevelFactor(5), rarityGap);
        }

        [Test]
        public void Item_level_is_clamped_to_the_valid_range()
        {
            Assert.AreEqual(GearTable.MinItemLevel, GearItem.At(GearSlot.Ring, GearTier.Rare, -8).Level);
            Assert.AreEqual(GearTable.MaxItemLevel, GearItem.At(GearSlot.Ring, GearTier.Rare, int.MaxValue).Level);
        }

        [Test]
        public void Higher_level_sells_for_more()
            => Assert.Greater(GearItem.At(GearSlot.Ring, GearTier.Rare, 4).SellGold,
                              GearItem.At(GearSlot.Ring, GearTier.Rare, 1).SellGold);

        [Test]
        public void An_empty_item_has_no_power_at_any_level()
            => Assert.AreEqual(0.0, GearItem.Empty.Power, 1e-9);

        [Test]
        public void Enhancement_still_stacks_on_top_of_level()
        {
            var plain = GearItem.At(GearSlot.Ring, GearTier.Rare, 6);
            var plus5 = GearItem.At(GearSlot.Ring, GearTier.Rare, 6, 5);
            Assert.AreEqual(plain.Power * 1.4, plus5.Power, 1e-6);
        }
    }
}
