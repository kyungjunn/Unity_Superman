using GrowNa.Core;
using GrowNa.Gear;
using GrowNa.Persistence;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class SaveSystemTests
    {
        static SaveData Sample()
        {
            var data = SaveData.NewGame();
            data.gold = 123456.75;
            data.grain = 900;
            data.wick = 12;
            data.gem = 3;
            data.level = 7;
            data.evolutionStage = 2;
            data.upgradeLevels = new[] { 11, 5, 2 };
            data.currentHp = 321.5;
            data.world = 2;
            data.stage = 4;
            data.kills = 4;
            data.lampLevel = 31;
            data.offeringsSincePity = 47;
            data.totalOfferings = 247;
            data.gear[(int)GearSlot.Weapon] = GearItem.At(GearSlot.Weapon, GearTier.Legend, 7, 3);
            data.gear[(int)GearSlot.Boots] = GearItem.At(GearSlot.Boots, GearTier.Rare, 7);
            data.Stamp(new System.DateTime(2026, 8, 24, 12, 0, 0, System.DateTimeKind.Utc));
            return data;
        }

        [Test]
        public void Round_trip_preserves_every_field()
        {
            var original = Sample();
            var restored = SaveValidator.Read(SaveValidator.WriteBytes(original));

            Assert.AreEqual(SaveReadStatus.Loaded, restored.Status);
            Assert.AreEqual(original.gold, restored.Data.gold, 1e-6);
            Assert.AreEqual(original.grain, restored.Data.grain, 1e-6);
            Assert.AreEqual(original.level, restored.Data.level);
            Assert.AreEqual(original.upgradeLevels, restored.Data.upgradeLevels);
            Assert.AreEqual(original.world, restored.Data.world);
            Assert.AreEqual(original.stage, restored.Data.stage);
            Assert.AreEqual(original.lampLevel, restored.Data.lampLevel);
            Assert.AreEqual(original.offeringsSincePity, restored.Data.offeringsSincePity);
            Assert.AreEqual(GearTier.Legend, restored.Data.gear[(int)GearSlot.Weapon].tier);
            Assert.AreEqual(3, restored.Data.gear[(int)GearSlot.Weapon].plus);
            Assert.IsTrue(restored.Data.gear[(int)GearSlot.Weapon].owned);
            Assert.IsFalse(restored.Data.gear[(int)GearSlot.Helmet].owned);
        }

        [Test]
        public void Future_version_is_rejected()
        {
            var json = SaveValidator.ToCanonicalJson(Sample()).Replace("\"version\": 8", "\"version\": 9");
            var result = SaveValidator.Read(SaveValidator.Utf8.GetBytes(json));
            Assert.AreEqual(SaveReadStatus.UnsupportedVersion, result.Status);
        }

        [Test]
        public void Garbage_json_is_rejected()
        {
            Assert.AreEqual(SaveReadStatus.InvalidData, SaveValidator.Read(null).Status);
            Assert.AreEqual(SaveReadStatus.InvalidData, SaveValidator.Read(SaveValidator.Utf8.GetBytes("   ")).Status);
            Assert.AreEqual(SaveReadStatus.UnsupportedVersion, SaveValidator.Read(SaveValidator.Utf8.GetBytes("{\"version\":0}")).Status);
        }

        [Test]
        public void Negative_values_are_rejected_not_clamped()
        {
            var json = SaveValidator.ToCanonicalJson(Sample()).Replace("\"shard\": 0.0", "\"shard\": -10.0");
            var result = SaveValidator.Read(SaveValidator.Utf8.GetBytes(json));
            Assert.AreEqual(SaveReadStatus.InvalidData, result.Status);
        }

        [Test]
        public void Elapsed_seconds_measures_gap_since_stamp()
        {
            var now = new System.DateTime(2026, 8, 24, 12, 0, 0, System.DateTimeKind.Utc);
            var data = Sample();
            data.Stamp(now.AddHours(-2));
            Assert.AreEqual(7200, data.ElapsedSecondsSince(now), 1.0);
            Assert.AreEqual(0, data.ElapsedSecondsSince(now.AddHours(-5)));
        }

        [Test]
        public void Unstamped_save_reports_no_elapsed_time()
            => Assert.AreEqual(0, new SaveData().ElapsedSecondsSince(System.DateTime.UtcNow));
    }
}
