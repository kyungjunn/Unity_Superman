using System.Text;
using GrowNa.Persistence;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class SaveMigrationV3Tests
    {
        [Test]
        public void Legacy_v2_json_is_unsupported_not_migrated()
        {
            var json = "{\"version\":2,\"gold\":1}";
            var result = SaveValidator.Read(Encoding.UTF8.GetBytes(json));
            Assert.AreEqual(SaveReadStatus.UnsupportedVersion, result.Status);
            Assert.AreEqual(2, result.ParsedVersion);
            Assert.IsNull(result.Data);
        }

        [Test]
        public void Legacy_v7_json_is_migrated_to_v8()
        {
            var json = LegacyV7()
                .Replace("\"gold\": 0.0", "\"gold\": 1234.0")
                .Replace("\"level\": 1", "\"level\": 22");

            var result = SaveValidator.Read(Encoding.UTF8.GetBytes(json));

            Assert.AreEqual(SaveReadStatus.Loaded, result.Status);
            Assert.AreEqual(SaveData.CurrentVersion, result.Data.version);
            Assert.AreEqual(1234.0, result.Data.gold);
            Assert.AreEqual(22, result.Data.level);
        }

        [Test]
        public void Legacy_v7_with_invalid_legacy_shape_is_rejected()
        {
            var json = LegacyV7().Replace(
                "\"skillOwned\": [false,false,false,false,false,false,false,false,false,false,false,false]",
                "\"skillOwned\": [true]");

            var result = SaveValidator.Read(Encoding.UTF8.GetBytes(json));

            Assert.AreEqual(SaveReadStatus.InvalidData, result.Status);
            Assert.AreEqual("shape", result.ErrorCode);
            Assert.AreEqual("skillOwned", result.JsonPath);
        }

        [Test]
        public void Legacy_v7_empty_gear_uses_array_index_as_slot()
        {
            var json = LegacyV7().Replace("\"slot\": 2", "\"slot\": 0");

            var result = SaveValidator.Read(Encoding.UTF8.GetBytes(json));

            Assert.AreEqual(SaveReadStatus.Loaded, result.Status);
            Assert.AreEqual(GrowNa.Gear.GearSlot.Armor, result.Data.gear[2].slot);
            Assert.IsFalse(result.Data.gear[2].owned);
        }

        [Test]
        public void Fresh_v8_has_empty_skill_and_pet_slots()
        {
            var data = SaveData.NewGame();
            foreach (int id in data.skillEquipped) Assert.AreEqual(GrowNa.Skill.SkillTable.None, id);
            Assert.AreEqual(GrowNa.Pet.PetTable.None, data.petEquipped);
        }

        static string LegacyV7()
        {
            string json = SaveValidator.ToCanonicalJson(SaveData.NewGame())
                .Replace("\"version\": 8", "\"version\": 7");
            int end = json.LastIndexOf('}');
            return json.Insert(end,
                ",\n  \"skillOwned\": [false,false,false,false,false,false,false,false,false,false,false,false]" +
                ",\n  \"petOwned\": [false,false,false,false,false,false,false,false]" +
                ",\n  \"questKillProgress\": 0" +
                ",\n  \"questCompleted\": 0\n");
        }
    }
}
