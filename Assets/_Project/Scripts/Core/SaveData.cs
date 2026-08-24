using System;
using GrowNa.Gear;

namespace GrowNa.Core
{
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public long savedAtUnixSeconds;

        public double gold;
        public double grain;
        public double wick;
        public double gem;

        public int level = 1;
        public int evolutionStage = 1;
        public int[] upgradeLevels = new int[3];
        public double currentHp;

        public int world = 1;
        public int stage = 1;
        public int kills;

        public int lampLevel = 1;
        public int offeringsSincePity;
        public int totalOfferings;

        public GearItem[] gear = new GearItem[GearTable.SlotCount];

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
