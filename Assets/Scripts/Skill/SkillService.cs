using System;
using System.Collections.Generic;
using GrowNa.Battle;
using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Skill
{
    public class SkillService : MonoBehaviour
    {
        [SerializeField] int[] levels = new int[SkillTable.Count];
        [SerializeField] int[] equipped = NewEmptyLoadout();
        [SerializeField] float[] cooldowns = new float[SkillTable.EquipSlots];

        Wallet wallet;
        QuestService quests;
        BattleManager battle;
        IGameRandom rng;

        public event Action Changed;
        public event Action<int, SkillDef> Fired;
        public event Action<SkillDef, bool> Drawn;

        public void Bind(Wallet boundWallet, QuestService boundQuests, BattleManager boundBattle, IGameRandom boundRandom)
        {
            wallet = boundWallet;
            quests = boundQuests;
            battle = boundBattle;
            rng = boundRandom;
        }

        static int[] NewEmptyLoadout()
        {
            var slots = new int[SkillTable.EquipSlots];
            for (int i = 0; i < slots.Length; i++) slots[i] = SkillTable.None;
            return slots;
        }

        public int EquippedId(int slot) => InRange(slot) ? equipped[slot] : SkillTable.None;
        public bool HasSkill(int slot) => SkillTable.IsValid(EquippedId(slot));
        public bool Owns(int id) => Level(id) > 0;
        public int Level(int id) => SkillTable.IsValid(id) ? levels[id] : 0;
        public float Cooldown(int slot) => InRange(slot) ? cooldowns[slot] : 0f;
        public double DrawCost => SkillTable.DrawCost;
        public bool CanDraw() => wallet != null && wallet.Gem >= SkillTable.DrawCost;

        public float CooldownRatio(int slot)
        {
            if (!HasSkill(slot)) return 0f;
            float full = SkillTable.Get(equipped[slot]).cooldown;
            return full <= 0f ? 0f : Mathf.Clamp01(cooldowns[slot] / full);
        }

        public bool TryDraw(out SkillDef drawn, out bool duplicate, out bool refunded)
        {
            drawn = default;
            duplicate = false;
            refunded = false;

            if (wallet == null || !wallet.TrySpend(CurrencyKind.Gem, SkillTable.DrawCost)) return false;

            double rollA = rng != null ? rng.Value01() : 0;
            double rollB = rng != null ? rng.Value01() : 0;
            int id = SkillTable.Roll(rollA, rollB);
            drawn = SkillTable.Get(id);
            int before = levels[id];
            duplicate = before > 0;
            levels[id] = SkillTable.ApplyPull(before, out refunded);

            if (refunded) wallet.Add(CurrencyKind.Gem, SkillTable.DrawCost * SkillTable.MaxLevelRefund);
            else if (before <= 0) AutoEquip(id);

            quests?.Report(QuestKind.SkillDraw);
            Drawn?.Invoke(drawn, duplicate);
            Changed?.Invoke();
            return true;
        }

        void AutoEquip(int id)
        {
            for (int i = 0; i < equipped.Length; i++)
            {
                if (SkillTable.IsValid(equipped[i])) continue;
                equipped[i] = id;
                cooldowns[i] = 0f;
                return;
            }

            int weakest = 0;
            for (int i = 1; i < equipped.Length; i++)
                if (SkillTable.Get(equipped[i]).damageMultiplier < SkillTable.Get(equipped[weakest]).damageMultiplier)
                    weakest = i;

            if (SkillTable.Get(id).damageMultiplier <= SkillTable.Get(equipped[weakest]).damageMultiplier) return;
            equipped[weakest] = id;
            cooldowns[weakest] = 0f;
        }

        public bool AutoEquipBest()
        {
            var ranked = new List<int>();
            for (int id = 0; id < SkillTable.Count; id++)
                if (Owns(id)) ranked.Add(id);

            ranked.Sort((a, b) => SkillTable.Strength(b).CompareTo(SkillTable.Strength(a)));

            var next = NewEmptyLoadout();
            for (int i = 0; i < next.Length && i < ranked.Count; i++) next[i] = ranked[i];

            bool changed = false;
            for (int i = 0; i < equipped.Length; i++)
            {
                if (equipped[i] == next[i]) continue;
                equipped[i] = next[i];
                cooldowns[i] = 0f;
                changed = true;
            }

            if (changed) Changed?.Invoke();
            return changed;
        }

        public bool TryClear(int slot)
        {
            if (!InRange(slot) || !SkillTable.IsValid(equipped[slot])) return false;
            equipped[slot] = SkillTable.None;
            cooldowns[slot] = 0f;
            Changed?.Invoke();
            return true;
        }

        public bool TryEquip(int slot, int id)
        {
            if (!InRange(slot) || !Owns(id)) return false;
            for (int i = 0; i < equipped.Length; i++)
                if (i != slot && equipped[i] == id) equipped[i] = SkillTable.None;

            equipped[slot] = id;
            cooldowns[slot] = 0f;
            Changed?.Invoke();
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            for (int i = 0; i < equipped.Length; i++)
            {
                if (!HasSkill(i)) continue;
                if (cooldowns[i] > 0f)
                {
                    cooldowns[i] = Mathf.Max(0f, cooldowns[i] - deltaTime);
                    continue;
                }

                if (battle == null || !battle.HasLiveTarget) continue;
                int id = equipped[i];
                var def = SkillTable.Get(id);
                if (!battle.TrySkillStrike(SkillTable.ScaledDamage(id, levels[id]))) continue;

                cooldowns[i] = def.cooldown;
                Fired?.Invoke(i, def);
            }
        }

        static bool InRange(int slot) => slot >= 0 && slot < SkillTable.EquipSlots;

        public void LoadFrom(bool[] savedOwned, int[] savedEquipped)
            => LoadFrom(savedOwned, null, savedEquipped);

        public void LoadFrom(bool[] savedOwned, int[] savedLevels, int[] savedEquipped)
        {
            Array.Clear(levels, 0, levels.Length);
            if (savedLevels != null)
                for (int i = 0; i < levels.Length && i < savedLevels.Length; i++)
                    levels[i] = SkillTable.ClampLevel(savedLevels[i]);
            if (savedOwned != null)
                for (int i = 0; i < levels.Length && i < savedOwned.Length; i++)
                    if (savedOwned[i] && levels[i] <= 0) levels[i] = 1;

            equipped = NewEmptyLoadout();
            if (savedEquipped != null)
                for (int i = 0; i < equipped.Length && i < savedEquipped.Length; i++)
                    equipped[i] = Owns(savedEquipped[i]) ? savedEquipped[i] : SkillTable.None;

            cooldowns = new float[SkillTable.EquipSlots];
            Changed?.Invoke();
        }

        public bool[] OwnedSnapshot()
        {
            var owned = new bool[SkillTable.Count];
            for (int i = 0; i < owned.Length; i++) owned[i] = levels[i] > 0;
            return owned;
        }

        public int[] LevelSnapshot() => (int[])levels.Clone();
        public int[] EquippedSnapshot() => (int[])equipped.Clone();
    }
}
