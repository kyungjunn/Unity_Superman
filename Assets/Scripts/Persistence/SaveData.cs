using System;
using GrowNa.Battle;
using GrowNa.Core;
using GrowNa.Gear;
using GrowNa.Pet;
using GrowNa.Skill;

namespace GrowNa.Persistence
{
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 8;

        public int version = CurrentVersion;
        public long savedAtUnixSeconds;

        public double gold;
        public double grain;
        public double wick;
        public double gem;
        public double shard;

        public int level = 1;
        public int evolutionStage = 1;
        public double exp;
        public int[] upgradeLevels = new int[3];
        public double currentHp;

        public int[] skillLevel = new int[SkillTable.Count];
        public int[] skillEquipped = EmptySkillLoadout();

        public int[] petLevel = new int[PetTable.Count];
        public int petEquipped = PetTable.None;

        public int[] questProgress = new int[QuestTable.Count];
        public int[] questRounds = new int[QuestTable.Count];

        public double idleBankedSeconds;

        public int world = 1;
        public int stage = 1;
        public int kills;

        public int lampLevel = 1;
        public int offeringsSincePity;
        public int totalOfferings;

        public GearItem[] gear = EmptyGearSlots();
        public int[] gearStock = new int[GearTable.StockSize];
        public bool[] gearCodex = new bool[GearTable.StockSize];
        public int[] enhanceFailStreak = new int[GearTable.SlotCount];

        public static int[] EmptySkillLoadout()
        {
            var slots = new int[SkillTable.EquipSlots];
            for (int i = 0; i < slots.Length; i++) slots[i] = SkillTable.None;
            return slots;
        }

        public static GearItem[] EmptyGearSlots()
        {
            var items = new GearItem[GearTable.SlotCount];
            for (int i = 0; i < items.Length; i++) items[i] = EmptyGear.Slot(i);
            return items;
        }

        public static SaveData NewGame()
        {
            var data = new SaveData
            {
                version = CurrentVersion,
                grain = Wallet.StartingGrain,
                level = 1,
                evolutionStage = 1,
                lampLevel = LampTable.MinLevel,
                world = 1,
                stage = 1,
                skillEquipped = EmptySkillLoadout(),
                petEquipped = PetTable.None,
                gear = EmptyGearSlots(),
            };
            return data;
        }

        public DateTime SavedAtUtc => DateTimeOffset.FromUnixTimeSeconds(savedAtUnixSeconds).UtcDateTime;

        public double ElapsedSecondsSince(DateTime nowUtc)
        {
            if (savedAtUnixSeconds <= 0) return 0;
            double seconds = (nowUtc - SavedAtUtc).TotalSeconds;
            return seconds > 0 ? seconds : 0;
        }

        public void Stamp(DateTime nowUtc)
            => savedAtUnixSeconds = new DateTimeOffset(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc)).ToUnixTimeSeconds();
    }
}
