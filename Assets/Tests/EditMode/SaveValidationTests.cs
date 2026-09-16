using System.Text;
using GrowNa.Core;
using GrowNa.Gear;
using GrowNa.Persistence;
using GrowNa.Pet;
using GrowNa.Skill;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class SaveValidationTests
    {
        static SaveData Sample()
        {
            var data = SaveData.NewGame();
            data.gold = 12;
            data.grain = 3;
            data.level = 4;
            data.upgradeLevels = new[] { 1, 2, 0 };
            data.currentHp = 50;
            data.gear[(int)GearSlot.Weapon] = GearItem.At(GearSlot.Weapon, GearTier.Rare, 4);
            data.skillLevel[0] = 1;
            data.skillEquipped[0] = 0;
            data.Stamp(new System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc));
            return data;
        }

        [Test]
        public void Round_trip_loads_current_v8()
        {
            var original = Sample();
            var result = SaveValidator.Read(SaveValidator.WriteBytes(original));
            Assert.AreEqual(SaveReadStatus.Loaded, result.Status);
            Assert.AreEqual(original.gold, result.Data.gold, 1e-9);
            Assert.AreEqual(GearTier.Rare, result.Data.gear[(int)GearSlot.Weapon].tier);
            Assert.IsFalse(result.Data.gear[(int)GearSlot.Helmet].owned);
        }

        [Test]
        public void Version_only_object_is_invalid_and_does_not_default_fill()
        {
            var bytes = Encoding.UTF8.GetBytes("{\"version\":8}");
            var result = SaveValidator.Read(bytes);
            Assert.AreEqual(SaveReadStatus.InvalidData, result.Status);
            Assert.IsNull(result.Data);
        }

        [Test]
        public void Version_6_is_unsupported()
        {
            var data = Sample();
            data.version = 6;
            var json = SaveValidator.ToCanonicalJson(data);
            var result = SaveValidator.Read(Encoding.UTF8.GetBytes(json));
            Assert.AreEqual(SaveReadStatus.UnsupportedVersion, result.Status);
            Assert.AreEqual(6, result.ParsedVersion);
        }

        [Test]
        public void Missing_required_field_is_invalid()
        {
            var json = SaveValidator.ToCanonicalJson(Sample()).Replace("  \"gold\": 12.0,\r\n", "").Replace("  \"gold\": 12.0,\n", "");
            var result = SaveValidator.Read(Encoding.UTF8.GetBytes(json));
            Assert.AreEqual(SaveReadStatus.InvalidData, result.Status);
        }

        [Test]
        public void Memory_store_preserves_rejected_bytes()
        {
            var store = new MemorySaveStore();
            var raw = Encoding.UTF8.GetBytes("{\"version\":8}");
            store.Seed(raw);
            var service = new UnityEngine.GameObject("save").AddComponent<SaveService>();
            service.Bind(store, new FixedClock(System.DateTime.UtcNow), null, null, null, null, null, null, null, null, null, null, null);
            var read = service.Read();
            Assert.AreEqual(SaveReadStatus.InvalidData, read.Status);
            service.BlockWrites();
            var write = service.Save();
            Assert.AreEqual(SaveWriteStatus.Blocked, write.Status);
            Assert.AreEqual(0, store.WriteCount);
            CollectionAssert.AreEqual(raw, store.Snapshot());
            UnityEngine.Object.DestroyImmediate(service.gameObject);
        }

        [Test]
        public void New_game_factory_round_trips()
        {
            var data = SaveData.NewGame();
            data.Stamp(new System.DateTime(2026, 2, 2, 0, 0, 0, System.DateTimeKind.Utc));
            var result = SaveValidator.Read(SaveValidator.WriteBytes(data));
            Assert.AreEqual(SaveReadStatus.Loaded, result.Status);
            Assert.AreEqual(Wallet.StartingGrain, result.Data.grain, 1e-9);
            Assert.AreEqual(SkillTable.None, result.Data.skillEquipped[0]);
            Assert.AreEqual(PetTable.None, result.Data.petEquipped);
        }
    }
}
