using GrowNa.Core;
using GrowNa.Gear;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class UpgradeTableTests
    {
        [Test]
        public void First_upgrade_costs_base_price()
        {
            Assert.AreEqual(25, UpgradeTable.Cost(StatKind.Attack, 0));
            Assert.AreEqual(30, UpgradeTable.Cost(StatKind.Health, 0));
            Assert.AreEqual(400, UpgradeTable.Cost(StatKind.Crit, 0));
        }

        [Test]
        public void Cost_is_strictly_increasing()
        {
            foreach (var kind in UpgradeTable.All)
            {
                double previous = 0;
                for (int level = 0; level < 60; level++)
                {
                    double cost = UpgradeTable.Cost(kind, level);
                    Assert.Greater(cost, previous, $"{kind} Lv.{level} 비용이 오르지 않았다");
                    previous = cost;
                }
            }
        }

        [Test]
        public void Level_zero_bonus_is_neutral()
        {
            Assert.AreEqual(1.0, UpgradeTable.Bonus(StatKind.Attack, 0), 1e-9);
            Assert.AreEqual(1.0, UpgradeTable.Bonus(StatKind.Health, 0), 1e-9);
            Assert.AreEqual(0.0, UpgradeTable.Bonus(StatKind.Crit, 0), 1e-9);
        }

        [Test]
        public void Attack_bonus_is_linear_twelve_percent_per_level()
            => Assert.AreEqual(2.2, UpgradeTable.Bonus(StatKind.Attack, 10), 1e-9);

        [Test]
        public void Negative_level_is_clamped()
            => Assert.AreEqual(UpgradeTable.Cost(StatKind.Attack, 0), UpgradeTable.Cost(StatKind.Attack, -5));

        [Test]
        public void Gear_power_scales_with_tier_and_plus()
        {
            var plain = GearItem.Of(GearSlot.Weapon, GearTier.Legend);
            var enhanced = GearItem.Of(GearSlot.Weapon, GearTier.Legend, 10);
            Assert.AreEqual(110.0, plain.Power, 1e-9);
            Assert.AreEqual(110.0 * 1.8, enhanced.Power, 1e-9);
            Assert.IsTrue(enhanced.StrongerThan(plain));
        }

        [Test]
        public void Empty_gear_has_no_power()
        {
            Assert.AreEqual(0.0, GearItem.Empty.Power, 1e-9);
            Assert.IsTrue(GearItem.Of(GearSlot.Ring, GearTier.Worn).StrongerThan(GearItem.Empty));
        }
    }
}
