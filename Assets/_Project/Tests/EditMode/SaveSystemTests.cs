using System;
using GrowNa.Core;
using GrowNa.Gear;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class SaveSystemTests
    {
        static SaveData Sample()
        {
            var data = new SaveData
            {
                gold = 123456.75,
                grain = 900,
                wick = 12,
                gem = 3,
                level = 7,
                evolutionStage = 2,
                upgradeLevels = new[] { 11, 5, 2 },
                currentHp = 321.5,
                world = 2,
                stage = 4,
                kills = 6,
                lampLevel = 31,
                offeringsSincePity = 47,
                totalOfferings = 247,
            };
            data.gear[(int)GearSlot.Weapon] = GearItem.Of(GearSlot.Weapon, GearTier.Legend, 3);
            data.gear[(int)GearSlot.Boots] = GearItem.Of(GearSlot.Boots, GearTier.Rare);
            return data;
        }

        [Test]
        public void Round_trip_preserves_every_field()
        {
            var original = Sample();
            var restored = SaveSystem.FromJson(SaveSystem.ToJson(original));

            Assert.IsNotNull(restored);
            Assert.AreEqual(original.gold, restored.gold, 1e-6);
            Assert.AreEqual(original.grain, restored.grain, 1e-6);
            Assert.AreEqual(original.level, restored.level);
            Assert.AreEqual(original.upgradeLevels, restored.upgradeLevels);
            Assert.AreEqual(original.world, restored.world);
            Assert.AreEqual(original.stage, restored.stage);
            Assert.AreEqual(original.lampLevel, restored.lampLevel);
            Assert.AreEqual(original.offeringsSincePity, restored.offeringsSincePity);
            Assert.AreEqual(GearTier.Legend, restored.gear[(int)GearSlot.Weapon].tier);
            Assert.AreEqual(3, restored.gear[(int)GearSlot.Weapon].plus);
            Assert.IsTrue(restored.gear[(int)GearSlot.Weapon].owned);
            Assert.IsFalse(restored.gear[(int)GearSlot.Helmet].owned);
        }

        [Test]
        public void Future_version_is_rejected()
        {
            var data = Sample();
            data.version = SaveData.CurrentVersion + 1;
            Assert.IsNull(SaveSystem.FromJson(SaveSystem.ToJson(data)));
        }

        [Test]
        public void Garbage_json_is_rejected()
        {
            Assert.IsNull(SaveSystem.FromJson(null));
            Assert.IsNull(SaveSystem.FromJson("   "));
            Assert.IsNull(SaveSystem.FromJson("{\"version\":0}"));
        }

        [Test]
        public void Corrupt_values_are_normalized()
        {
            var data = Sample();
            data.level = -3;
            data.stage = 99;
            data.upgradeLevels = new[] { 1 };
            var restored = SaveSystem.FromJson(SaveSystem.ToJson(data));

            Assert.AreEqual(1, restored.level);
            Assert.AreEqual(10, restored.stage);
            Assert.AreEqual(3, restored.upgradeLevels.Length);
            Assert.AreEqual(GearTable.SlotCount, restored.gear.Length);
        }

        [Test]
        public void Elapsed_seconds_measures_gap_since_stamp()
        {
            var now = new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);
            var data = Sample();
            data.Stamp(now.AddHours(-2));

            Assert.AreEqual(7200, data.ElapsedSecondsSince(now), 1.0);
            Assert.AreEqual(0, data.ElapsedSecondsSince(now.AddHours(-5)));
        }

        [Test]
        public void Unstamped_save_reports_no_elapsed_time()
            => Assert.AreEqual(0, new SaveData().ElapsedSecondsSince(DateTime.UtcNow));
    }
}
