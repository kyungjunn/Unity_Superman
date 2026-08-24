using System;
using System.IO;
using UnityEngine;

namespace GrowNa.Core
{
    public static class SaveSystem
    {
        public const string FileName = "grow_na_save.json";

        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, true);

        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (ArgumentException)
            {
                return null;
            }
            if (data == null || data.version <= 0 || data.version > SaveData.CurrentVersion) return null;
            return Normalize(data);
        }

        static SaveData Normalize(SaveData data)
        {
            data.level = Mathf.Max(1, data.level);
            data.evolutionStage = Mathf.Max(1, data.evolutionStage);
            data.world = Mathf.Max(1, data.world);
            data.stage = Mathf.Clamp(data.stage, 1, 10);
            data.kills = Mathf.Max(0, data.kills);
            data.upgradeLevels ??= new int[3];
            if (data.upgradeLevels.Length < 3) Array.Resize(ref data.upgradeLevels, 3);
            data.gear ??= new Gear.GearItem[Gear.GearTable.SlotCount];
            if (data.gear.Length < Gear.GearTable.SlotCount)
                Array.Resize(ref data.gear, Gear.GearTable.SlotCount);
            return data;
        }

        public static bool Write(SaveData data)
        {
            try
            {
                File.WriteAllText(Path, ToJson(data));
                return true;
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[GrowNa] 세이브 실패: {e.Message}");
                return false;
            }
        }

        public static SaveData Read()
        {
            try
            {
                return File.Exists(Path) ? FromJson(File.ReadAllText(Path)) : null;
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[GrowNa] 세이브 읽기 실패: {e.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            if (File.Exists(Path)) File.Delete(Path);
        }
    }
}
