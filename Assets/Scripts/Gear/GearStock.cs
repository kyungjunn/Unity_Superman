using System;

namespace GrowNa.Gear
{
    /// <summary>
    /// 보유 장비 재고 + 수확 도감. MonoBehaviour 가 아닌 순수 클래스라 EditMode 에서 그대로 테스트된다.
    ///
    /// 재고는 <b>슬롯×티어 개수</b>만 센다. 개별 인스턴스를 들지 않는 이유:
    /// - 봉헌은 slot/tier 만 뽑는다(<see cref="LampService"/>). 개체를 구분할 정보가 애초에 없다.
    /// - 강화 수치(plus)는 장착품에만 붙는다. 재고에서 꺼낸 장비는 항상 +0.
    /// - 64칸 고정이라 세이브가 무한히 자라지 않는다.
    /// </summary>
    [Serializable]
    public class GearStock
    {
        /// <summary>도감 1칸당 계정 전체 스탯 가산. 64칸 전부 = +32%.</summary>
        public const double CodexBonusPerEntry = 0.005;

        readonly int[] counts = new int[GearTable.StockSize];
        readonly bool[] codex = new bool[GearTable.StockSize];

        public int Count(GearSlot slot, GearTier tier) => counts[GearTable.Index(slot, tier)];

        public bool Registered(GearSlot slot, GearTier tier) => codex[GearTable.Index(slot, tier)];

        public int TotalCount
        {
            get
            {
                int sum = 0;
                foreach (int c in counts) sum += c;
                return sum;
            }
        }

        public int CodexCount
        {
            get
            {
                int sum = 0;
                foreach (bool c in codex) if (c) sum++;
                return sum;
            }
        }

        /// <summary>도감 등록 수에 비례하는 계정 전체 스탯 배수. 등록만으로 붙는다(GDD 4.5).</summary>
        public double CodexMultiplier => 1.0 + CodexBonusPerEntry * CodexCount;

        /// <summary>도감에만 등록한다. 재고는 건드리지 않는다. 반환값 = 최초 등록이면 true.</summary>
        public bool Register(GearSlot slot, GearTier tier)
        {
            int i = GearTable.Index(slot, tier);
            if (codex[i]) return false;
            codex[i] = true;
            return true;
        }

        /// <summary>재고에 넣는다. 도감 등록은 별도(<see cref="Register"/>) — 장착으로 빠진 장비도 도감에는 남아야 하기 때문.</summary>
        public void Add(GearSlot slot, GearTier tier, int count = 1)
        {
            if (count <= 0) return;
            counts[GearTable.Index(slot, tier)] += count;
        }

        public bool TryRemove(GearSlot slot, GearTier tier, int count = 1)
        {
            if (count <= 0) return false;
            int i = GearTable.Index(slot, tier);
            if (counts[i] < count) return false;
            counts[i] -= count;
            return true;
        }

        public double DismantleAll(GearSlot slot, GearTier tier)
        {
            int i = GearTable.Index(slot, tier);
            int count = counts[i];
            if (count <= 0) return 0;
            counts[i] = 0;
            return GearTable.Shard(tier) * count;
        }

        /// <summary>지정 티어 이하 전량 분해. 도감은 그대로 남는다.</summary>
        public double DismantleUpTo(GearTier maxTier)
        {
            double shard = 0;
            for (int s = 0; s < GearTable.SlotCount; s++)
                for (int t = 0; t <= (int)maxTier; t++)
                    shard += DismantleAll((GearSlot)s, (GearTier)t);
            return shard;
        }

        public bool CanFuse(GearSlot slot, GearTier tier)
            => !GearTable.IsMaxTier(tier) && Count(slot, tier) >= GearTable.FusionCount(tier);

        /// <summary>
        /// 동일 티어·슬롯 N개 → 상위 티어 1개 (100% 확정). 강화 수치는 이월되지 않는다 (GDD 4.4).
        /// 결과물은 재고로 들어간다. 장착 판정은 호출자(<see cref="Inventory"/>)가 한다.
        /// </summary>
        public bool TryFuse(GearSlot slot, GearTier tier, int itemLevel, out GearItem result)
        {
            result = GearItem.Empty;
            if (!CanFuse(slot, tier)) return false;
            if (!TryRemove(slot, tier, GearTable.FusionCount(tier))) return false;

            var next = GearTable.NextTier(tier);
            Add(slot, next);
            Register(slot, next);
            result = GearItem.At(slot, next, itemLevel);
            return true;
        }

        public int[] CountsSnapshot() => (int[])counts.Clone();

        public bool[] CodexSnapshot() => (bool[])codex.Clone();

        public void LoadFrom(int[] savedCounts, bool[] savedCodex)
        {
            Array.Clear(counts, 0, counts.Length);
            Array.Clear(codex, 0, codex.Length);
            if (savedCounts != null)
                for (int i = 0; i < counts.Length && i < savedCounts.Length; i++)
                    counts[i] = Math.Max(0, savedCounts[i]);
            if (savedCodex != null)
                for (int i = 0; i < codex.Length && i < savedCodex.Length; i++)
                    codex[i] = savedCodex[i];
        }
    }
}
