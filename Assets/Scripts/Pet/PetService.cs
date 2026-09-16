using System;
using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Pet
{
    public class PetService : MonoBehaviour
    {
        [SerializeField] int[] levels = new int[PetTable.Count];
        [SerializeField] int equipped = PetTable.None;

        Wallet wallet;
        PlayerStats stats;
        QuestService quests;
        IGameRandom rng;

        public event Action Changed;
        public event Action<PetDef, bool> Drawn;

        public int ActiveId => Owns(equipped) ? equipped : PetTable.None;
        public bool HasPet => PetTable.IsValid(ActiveId);
        public double DrawCost => PetTable.DrawCost;
        public int EquippedId => equipped;

        public bool Owns(int id) => Level(id) > 0;
        public int Level(int id) => PetTable.IsValid(id) ? levels[id] : 0;

        public int OwnedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < levels.Length; i++) if (levels[i] > 0) count++;
                return count;
            }
        }

        public void Bind(Wallet boundWallet, PlayerStats boundStats, QuestService boundQuests, IGameRandom boundRandom)
        {
            wallet = boundWallet;
            stats = boundStats;
            quests = boundQuests;
            rng = boundRandom;
        }

        public void PushDerived() => PushBonus();

        public bool CanDraw() => wallet != null && wallet.Gem >= PetTable.DrawCost;

        public bool TryDraw(out PetDef drawn, out bool duplicate, out bool refunded)
        {
            drawn = default;
            duplicate = false;
            refunded = false;

            if (wallet == null || !wallet.TrySpend(CurrencyKind.Gem, PetTable.DrawCost)) return false;

            double rollA = rng != null ? rng.Value01() : 0;
            double rollB = rng != null ? rng.Value01() : 0;
            int id = PetTable.Roll(rollA, rollB);
            drawn = PetTable.Get(id);
            int before = levels[id];
            duplicate = before > 0;
            levels[id] = PetTable.ApplyPull(before, out refunded);

            if (refunded) wallet.Add(CurrencyKind.Gem, PetTable.DrawCost * PetTable.MaxLevelRefund);
            else if (before <= 0 && !HasPet) equipped = id;

            PushBonus();
            quests?.Report(QuestKind.PetDraw);
            Drawn?.Invoke(drawn, duplicate);
            Changed?.Invoke();
            return true;
        }

        public EquipmentBonus CurrentBonus()
        {
            int id = ActiveId;
            if (!PetTable.IsValid(id)) return EquipmentBonus.None;

            return new EquipmentBonus
            {
                attackMultiplier = PetTable.ScaledAttackMultiplier(id, levels[id]),
                healthMultiplier = PetTable.ScaledHealthMultiplier(id, levels[id]),
                defenseMultiplier = 1.0,
            };
        }

        public bool TryEquip(int id)
        {
            if (!Owns(id) || equipped == id) return false;
            equipped = id;
            PushBonus();
            Changed?.Invoke();
            return true;
        }

        public bool Unequip()
        {
            if (!PetTable.IsValid(equipped)) return false;
            equipped = PetTable.None;
            PushBonus();
            Changed?.Invoke();
            return true;
        }

        public bool EquipBest() => TryEquip(PetTable.BestOwned(OwnedSnapshot()));

        public void LoadFrom(bool[] savedOwned, int savedEquipped)
            => LoadFrom(savedOwned, null, savedEquipped);

        public void LoadFrom(bool[] savedOwned, int[] savedLevels, int savedEquipped)
        {
            Array.Clear(levels, 0, levels.Length);
            if (savedLevels != null)
                for (int i = 0; i < levels.Length && i < savedLevels.Length; i++)
                    levels[i] = PetTable.ClampLevel(savedLevels[i]);
            if (savedOwned != null)
                for (int i = 0; i < levels.Length && i < savedOwned.Length; i++)
                    if (savedOwned[i] && levels[i] <= 0) levels[i] = 1;

            equipped = Owns(savedEquipped) ? savedEquipped : PetTable.None;

            PushBonus();
            Changed?.Invoke();
        }

        public bool[] OwnedSnapshot()
        {
            var owned = new bool[PetTable.Count];
            for (int i = 0; i < owned.Length; i++) owned[i] = levels[i] > 0;
            return owned;
        }

        public int[] LevelSnapshot() => (int[])levels.Clone();

        void PushBonus() => stats?.SetPetBonus(CurrentBonus());
    }
}
