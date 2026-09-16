using System;
using System.Globalization;
using System.Text;
using GrowNa.Battle;
using GrowNa.Core;
using GrowNa.Gear;
using GrowNa.Pet;
using GrowNa.Skill;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GrowNa.Persistence
{
    public static class SaveValidator
    {
        public static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        public static SaveReadResult Read(byte[] bytes)
        {
            if (bytes == null) return SaveReadResult.Invalid("empty", "$", "bytes null");
            string text;
            try
            {
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                    text = Utf8.GetString(bytes, 3, bytes.Length - 3);
                else
                    text = Utf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return SaveReadResult.Invalid("utf8", "$", "invalid utf-8");
            }

            if (string.IsNullOrWhiteSpace(text))
                return SaveReadResult.Invalid("empty", "$", "empty document");

            JObject root;
            try
            {
                using var reader = new JsonTextReader(new System.IO.StringReader(text))
                {
                    DateParseHandling = DateParseHandling.None,
                    FloatParseHandling = FloatParseHandling.Double,
                    MaxDepth = 16,
                };
                var settings = new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    CommentHandling = CommentHandling.Ignore,
                    LineInfoHandling = LineInfoHandling.Load,
                };
                var token = JToken.Load(reader, settings);
                if (reader.Read() && reader.TokenType != JsonToken.None)
                    return SaveReadResult.Invalid("trailing", "$", "second document");
                if (token is not JObject obj)
                    return SaveReadResult.Invalid("root", "$", "root is not object");
                root = obj;
            }
            catch (JsonReaderException e)
            {
                return SaveReadResult.Invalid("syntax", "$", e.Message);
            }
            catch (JsonException e)
            {
                return SaveReadResult.Invalid("json", "$", e.Message);
            }

            if (!TryInteger(root, "version", out int version, out var versionFail))
                return versionFail;

            if (version == 7)
            {
                if (!TryBoolArray(root, "skillOwned", SkillTable.Count, out _, out var legacyFail))
                    return legacyFail;
                if (!TryBoolArray(root, "petOwned", PetTable.Count, out _, out legacyFail))
                    return legacyFail;
                if (!TryInteger(root, "questKillProgress", out int questKillProgress, out legacyFail))
                    return legacyFail;
                if (questKillProgress < 0)
                    return SaveReadResult.Invalid("range", "questKillProgress", "neg");
                if (!TryInteger(root, "questCompleted", out int questCompleted, out legacyFail))
                    return legacyFail;
                if (questCompleted < 0)
                    return SaveReadResult.Invalid("range", "questCompleted", "neg");
                if (!NormalizeLegacyEmptyGear(root, out legacyFail))
                    return legacyFail;

                root.Remove("skillOwned");
                root.Remove("petOwned");
                root.Remove("questKillProgress");
                root.Remove("questCompleted");
                root["version"] = SaveData.CurrentVersion;
                version = SaveData.CurrentVersion;
            }
            else if (version != SaveData.CurrentVersion)
                return SaveReadResult.Unsupported(version, "version");

            var data = new SaveData { version = SaveData.CurrentVersion };

            if (!TryInt64(root, "savedAtUnixSeconds", out data.savedAtUnixSeconds, out var fail)) return fail;
            if (data.savedAtUnixSeconds < 0) return SaveReadResult.Invalid("range", "savedAtUnixSeconds", "negative");

            if (!TryFiniteNonNeg(root, "gold", out data.gold, out fail)) return fail;
            if (!TryFiniteNonNeg(root, "grain", out data.grain, out fail)) return fail;
            if (!TryFiniteNonNeg(root, "wick", out data.wick, out fail)) return fail;
            if (!TryFiniteNonNeg(root, "gem", out data.gem, out fail)) return fail;
            if (!TryFiniteNonNeg(root, "shard", out data.shard, out fail)) return fail;

            if (!TryInteger(root, "level", out data.level, out fail)) return fail;
            if (data.level < 1 || data.level > ExpTable.MaxLevel)
                return SaveReadResult.Invalid("range", "level", data.level.ToString(CultureInfo.InvariantCulture));
            if (!TryInteger(root, "evolutionStage", out data.evolutionStage, out fail)) return fail;
            if (data.evolutionStage < 1) return SaveReadResult.Invalid("range", "evolutionStage", "lt1");
            if (!TryFiniteNonNeg(root, "exp", out data.exp, out fail)) return fail;
            if (!TryIntArray(root, "upgradeLevels", 3, out data.upgradeLevels, out fail)) return fail;
            for (int i = 0; i < data.upgradeLevels.Length; i++)
                if (data.upgradeLevels[i] < 0 || data.upgradeLevels[i] > UpgradeTable.MaxLevel)
                    return SaveReadResult.Invalid("range", $"upgradeLevels[{i}]", "out of range");
            if (!TryFiniteNonNeg(root, "currentHp", out data.currentHp, out fail)) return fail;

            if (!TryIntArray(root, "skillLevel", SkillTable.Count, out data.skillLevel, out fail)) return fail;
            for (int i = 0; i < data.skillLevel.Length; i++)
                if (data.skillLevel[i] < 0 || data.skillLevel[i] > SkillTable.MaxLevel)
                    return SaveReadResult.Invalid("range", $"skillLevel[{i}]", "out of range");
            if (!TryIntArray(root, "skillEquipped", SkillTable.EquipSlots, out data.skillEquipped, out fail)) return fail;
            for (int i = 0; i < data.skillEquipped.Length; i++)
            {
                int id = data.skillEquipped[i];
                if (id == SkillTable.None) continue;
                if (!SkillTable.IsValid(id) || data.skillLevel[id] <= 0)
                    return SaveReadResult.Invalid("equip", $"skillEquipped[{i}]", "unowned or invalid");
            }

            if (!TryIntArray(root, "petLevel", PetTable.Count, out data.petLevel, out fail)) return fail;
            for (int i = 0; i < data.petLevel.Length; i++)
                if (data.petLevel[i] < 0 || data.petLevel[i] > PetTable.MaxLevel)
                    return SaveReadResult.Invalid("range", $"petLevel[{i}]", "out of range");
            if (!TryInteger(root, "petEquipped", out data.petEquipped, out fail)) return fail;
            if (data.petEquipped != PetTable.None)
            {
                if (!PetTable.IsValid(data.petEquipped) || data.petLevel[data.petEquipped] <= 0)
                    return SaveReadResult.Invalid("equip", "petEquipped", "unowned or invalid");
            }

            if (!TryIntArray(root, "questProgress", QuestTable.Count, out data.questProgress, out fail)) return fail;
            if (!TryIntArray(root, "questRounds", QuestTable.Count, out data.questRounds, out fail)) return fail;
            for (int i = 0; i < data.questProgress.Length; i++)
            {
                if (data.questProgress[i] < 0) return SaveReadResult.Invalid("range", $"questProgress[{i}]", "neg");
                if (data.questRounds[i] < 0) return SaveReadResult.Invalid("range", $"questRounds[{i}]", "neg");
            }

            if (!TryFiniteNonNeg(root, "idleBankedSeconds", out data.idleBankedSeconds, out fail)) return fail;

            if (!TryInteger(root, "world", out data.world, out fail)) return fail;
            if (data.world < 1) return SaveReadResult.Invalid("range", "world", "lt1");
            if (!TryInteger(root, "stage", out data.stage, out fail)) return fail;
            if (data.stage < 1 || data.stage > 10) return SaveReadResult.Invalid("range", "stage", "not 1..10");
            if (!TryInteger(root, "kills", out data.kills, out fail)) return fail;
            if (data.kills < 0 || data.kills > StageTable.MonstersPerStage)
                return SaveReadResult.Invalid("range", "kills", "out of stage");

            if (!TryInteger(root, "lampLevel", out data.lampLevel, out fail)) return fail;
            if (data.lampLevel < LampTable.MinLevel || data.lampLevel > LampTable.MaxLevel)
                return SaveReadResult.Invalid("range", "lampLevel", "out of table");
            if (!TryInteger(root, "offeringsSincePity", out data.offeringsSincePity, out fail)) return fail;
            if (data.offeringsSincePity < 0 || data.offeringsSincePity > LampTable.PityThreshold)
                return SaveReadResult.Invalid("range", "offeringsSincePity", "out of pity");
            if (!TryInteger(root, "totalOfferings", out data.totalOfferings, out fail)) return fail;
            if (data.totalOfferings < data.offeringsSincePity)
                return SaveReadResult.Invalid("range", "totalOfferings", "lt sincePity");

            if (!TryIntArray(root, "gearStock", GearTable.StockSize, out data.gearStock, out fail)) return fail;
            if (!TryBoolArray(root, "gearCodex", GearTable.StockSize, out data.gearCodex, out fail)) return fail;
            if (!TryIntArray(root, "enhanceFailStreak", GearTable.SlotCount, out data.enhanceFailStreak, out fail)) return fail;
            for (int i = 0; i < data.gearStock.Length; i++)
                if (data.gearStock[i] < 0) return SaveReadResult.Invalid("range", $"gearStock[{i}]", "neg");
            for (int i = 0; i < data.enhanceFailStreak.Length; i++)
                if (data.enhanceFailStreak[i] < 0) return SaveReadResult.Invalid("range", $"enhanceFailStreak[{i}]", "neg");

            if (!TryGearArray(root, out data.gear, out fail)) return fail;

            foreach (var prop in root.Properties())
            {
                if (!IsKnown(prop.Name))
                    return SaveReadResult.Invalid("unknown", prop.Name, "unknown field");
            }

            return SaveReadResult.Loaded(data);
        }

        public static byte[] WriteBytes(SaveData data)
        {
            var json = ToCanonicalJson(data);
            return Utf8.GetBytes(json);
        }

        public static string ToCanonicalJson(SaveData data)
        {
            var root = new JObject
            {
                ["version"] = data.version,
                ["savedAtUnixSeconds"] = data.savedAtUnixSeconds,
                ["gold"] = data.gold,
                ["grain"] = data.grain,
                ["wick"] = data.wick,
                ["gem"] = data.gem,
                ["shard"] = data.shard,
                ["level"] = data.level,
                ["evolutionStage"] = data.evolutionStage,
                ["exp"] = data.exp,
                ["upgradeLevels"] = IntArray(data.upgradeLevels),
                ["currentHp"] = data.currentHp,
                ["skillLevel"] = IntArray(data.skillLevel),
                ["skillEquipped"] = IntArray(data.skillEquipped),
                ["petLevel"] = IntArray(data.petLevel),
                ["petEquipped"] = data.petEquipped,
                ["questProgress"] = IntArray(data.questProgress),
                ["questRounds"] = IntArray(data.questRounds),
                ["idleBankedSeconds"] = data.idleBankedSeconds,
                ["world"] = data.world,
                ["stage"] = data.stage,
                ["kills"] = data.kills,
                ["lampLevel"] = data.lampLevel,
                ["offeringsSincePity"] = data.offeringsSincePity,
                ["totalOfferings"] = data.totalOfferings,
                ["gear"] = GearArray(data.gear),
                ["gearStock"] = IntArray(data.gearStock),
                ["gearCodex"] = BoolArray(data.gearCodex),
                ["enhanceFailStreak"] = IntArray(data.enhanceFailStreak),
            };
            return root.ToString(Formatting.Indented);
        }

        static JArray IntArray(int[] values)
        {
            var arr = new JArray();
            if (values != null) foreach (int v in values) arr.Add(v);
            return arr;
        }

        static JArray BoolArray(bool[] values)
        {
            var arr = new JArray();
            if (values != null) foreach (bool v in values) arr.Add(v);
            return arr;
        }

        static JArray GearArray(GearItem[] items)
        {
            var arr = new JArray();
            if (items == null) return arr;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i].owned ? items[i] : EmptyGear.Slot(i);
                arr.Add(new JObject
                {
                    ["slot"] = (int)item.slot,
                    ["tier"] = (int)item.tier,
                    ["plus"] = item.plus,
                    ["owned"] = item.owned,
                    ["level"] = item.owned ? item.level : 0,
                });
            }
            return arr;
        }

        static bool IsKnown(string name) => name switch
        {
            "version" or "savedAtUnixSeconds" or "gold" or "grain" or "wick" or "gem" or "shard"
                or "level" or "evolutionStage" or "exp" or "upgradeLevels" or "currentHp"
                or "skillLevel" or "skillEquipped" or "petLevel" or "petEquipped"
                or "questProgress" or "questRounds" or "idleBankedSeconds"
                or "world" or "stage" or "kills"
                or "lampLevel" or "offeringsSincePity" or "totalOfferings"
                or "gear" or "gearStock" or "gearCodex" or "enhanceFailStreak" => true,
            _ => false,
        };

        static bool TryInteger(JObject root, string name, out int value, out SaveReadResult fail)
        {
            value = 0;
            fail = default;
            if (!root.TryGetValue(name, out var token) || token == null || token.Type == JTokenType.Null)
            {
                fail = SaveReadResult.Invalid("missing", name, "missing");
                return false;
            }
            if (token.Type != JTokenType.Integer)
            {
                fail = SaveReadResult.Invalid("type", name, token.Type.ToString());
                return false;
            }
            long raw = token.Value<long>();
            if (raw < int.MinValue || raw > int.MaxValue)
            {
                fail = SaveReadResult.Invalid("overflow", name, raw.ToString(CultureInfo.InvariantCulture));
                return false;
            }
            value = (int)raw;
            return true;
        }

        static bool TryInt64(JObject root, string name, out long value, out SaveReadResult fail)
        {
            value = 0;
            fail = default;
            if (!root.TryGetValue(name, out var token) || token == null || token.Type == JTokenType.Null)
            {
                fail = SaveReadResult.Invalid("missing", name, "missing");
                return false;
            }
            if (token.Type != JTokenType.Integer)
            {
                fail = SaveReadResult.Invalid("type", name, token.Type.ToString());
                return false;
            }
            value = token.Value<long>();
            return true;
        }

        static bool TryFiniteNonNeg(JObject root, string name, out double value, out SaveReadResult fail)
        {
            value = 0;
            fail = default;
            if (!root.TryGetValue(name, out var token) || token == null || token.Type == JTokenType.Null)
            {
                fail = SaveReadResult.Invalid("missing", name, "missing");
                return false;
            }
            if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)
            {
                fail = SaveReadResult.Invalid("type", name, token.Type.ToString());
                return false;
            }
            value = token.Value<double>();
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            {
                fail = SaveReadResult.Invalid("range", name, "not finite nonnegative");
                return false;
            }
            return true;
        }

        static bool TryIntArray(JObject root, string name, int length, out int[] values, out SaveReadResult fail)
        {
            values = null;
            fail = default;
            if (!root.TryGetValue(name, out var token) || token is not JArray arr)
            {
                fail = SaveReadResult.Invalid(token == null ? "missing" : "type", name, "array required");
                return false;
            }
            if (arr.Count != length)
            {
                fail = SaveReadResult.Invalid("shape", name, $"len {arr.Count} != {length}");
                return false;
            }
            values = new int[length];
            for (int i = 0; i < length; i++)
            {
                var item = arr[i];
                if (item == null || item.Type != JTokenType.Integer)
                {
                    fail = SaveReadResult.Invalid("type", $"{name}[{i}]", item?.Type.ToString() ?? "null");
                    return false;
                }
                long raw = item.Value<long>();
                if (raw < int.MinValue || raw > int.MaxValue)
                {
                    fail = SaveReadResult.Invalid("overflow", $"{name}[{i}]", raw.ToString(CultureInfo.InvariantCulture));
                    return false;
                }
                values[i] = (int)raw;
            }
            return true;
        }

        static bool TryBoolArray(JObject root, string name, int length, out bool[] values, out SaveReadResult fail)
        {
            values = null;
            fail = default;
            if (!root.TryGetValue(name, out var token) || token is not JArray arr)
            {
                fail = SaveReadResult.Invalid(token == null ? "missing" : "type", name, "array required");
                return false;
            }
            if (arr.Count != length)
            {
                fail = SaveReadResult.Invalid("shape", name, $"len {arr.Count} != {length}");
                return false;
            }
            values = new bool[length];
            for (int i = 0; i < length; i++)
            {
                var item = arr[i];
                if (item == null || item.Type != JTokenType.Boolean)
                {
                    fail = SaveReadResult.Invalid("type", $"{name}[{i}]", item?.Type.ToString() ?? "null");
                    return false;
                }
                values[i] = item.Value<bool>();
            }
            return true;
        }

        static bool TryGearArray(JObject root, out GearItem[] items, out SaveReadResult fail)
        {
            items = null;
            fail = default;
            if (!root.TryGetValue("gear", out var token) || token is not JArray arr)
            {
                fail = SaveReadResult.Invalid(token == null ? "missing" : "type", "gear", "array required");
                return false;
            }
            if (arr.Count != GearTable.SlotCount)
            {
                fail = SaveReadResult.Invalid("shape", "gear", $"len {arr.Count}");
                return false;
            }
            items = new GearItem[GearTable.SlotCount];
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] is not JObject obj)
                {
                    fail = SaveReadResult.Invalid("type", $"gear[{i}]", "object required");
                    return false;
                }
                if (!TryInteger(obj, "slot", out int slot, out fail)) { fail = Prefix(fail, $"gear[{i}]."); return false; }
                if (!TryInteger(obj, "tier", out int tier, out fail)) { fail = Prefix(fail, $"gear[{i}]."); return false; }
                if (!TryInteger(obj, "plus", out int plus, out fail)) { fail = Prefix(fail, $"gear[{i}]."); return false; }
                if (!obj.TryGetValue("owned", out var ownedToken) || ownedToken.Type != JTokenType.Boolean)
                {
                    fail = SaveReadResult.Invalid("type", $"gear[{i}].owned", "bool required");
                    return false;
                }
                bool owned = ownedToken.Value<bool>();
                if (!TryInteger(obj, "level", out int level, out fail)) { fail = Prefix(fail, $"gear[{i}]."); return false; }
                foreach (var prop in obj.Properties())
                {
                    if (prop.Name is not ("slot" or "tier" or "plus" or "owned" or "level"))
                    {
                        fail = SaveReadResult.Invalid("unknown", $"gear[{i}].{prop.Name}", "unknown field");
                        return false;
                    }
                }
                if (owned)
                {
                    if (slot != i || slot < 0 || slot >= GearTable.SlotCount)
                    {
                        fail = SaveReadResult.Invalid("range", $"gear[{i}].slot", "mismatch");
                        return false;
                    }
                    if (tier < 0 || tier >= GearTable.TierCount)
                    {
                        fail = SaveReadResult.Invalid("range", $"gear[{i}].tier", "out of table");
                        return false;
                    }
                    if (plus < 0 || plus > EnhanceTable.MaxPlus)
                    {
                        fail = SaveReadResult.Invalid("range", $"gear[{i}].plus", "out of table");
                        return false;
                    }
                    if (level < GearTable.MinItemLevel || level > GearTable.MaxItemLevel)
                    {
                        fail = SaveReadResult.Invalid("range", $"gear[{i}].level", "out of table");
                        return false;
                    }
                    items[i] = GearItem.At((GearSlot)slot, (GearTier)tier, level, plus);
                }
                else
                {
                    if (slot != i || tier != 0 || plus != 0 || level != 0)
                    {
                        fail = SaveReadResult.Invalid("empty", $"gear[{i}]", "empty slot must be zeros");
                        return false;
                    }
                    items[i] = EmptyGear.Slot(i);
                }
            }
            return true;
        }

        static bool NormalizeLegacyEmptyGear(JObject root, out SaveReadResult fail)
        {
            fail = default;
            if (!root.TryGetValue("gear", out var token) || token is not JArray items)
            {
                fail = SaveReadResult.Invalid(token == null ? "missing" : "type", "gear", "array required");
                return false;
            }
            if (items.Count != GearTable.SlotCount)
            {
                fail = SaveReadResult.Invalid("shape", "gear", $"len {items.Count}");
                return false;
            }
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is not JObject item
                    || !item.TryGetValue("owned", out var owned)
                    || owned.Type != JTokenType.Boolean)
                {
                    fail = SaveReadResult.Invalid("type", $"gear[{i}]", "owned bool required");
                    return false;
                }
                if (owned.Value<bool>()) continue;
                item["slot"] = i;
                item["tier"] = 0;
                item["plus"] = 0;
                item["level"] = 0;
            }
            return true;
        }

        static SaveReadResult Prefix(SaveReadResult fail, string prefix)
            => SaveReadResult.Invalid(fail.ErrorCode, prefix + fail.JsonPath, fail.Message);
    }
}
