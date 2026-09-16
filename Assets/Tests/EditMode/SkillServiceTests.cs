using GrowNa.Skill;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace GrowNa.Tests
{
    public class SkillServiceTests
    {
        SkillService service;

        [SetUp]
        public void SetUp()
        {
            service = new GameObject("SkillService").AddComponent<SkillService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (service != null) Object.DestroyImmediate(service.gameObject);
        }

        static bool[] Roster(params int[] ids)
        {
            var owned = new bool[SkillTable.Count];
            foreach (int id in ids) owned[id] = true;
            return owned;
        }

        [Test]
        public void Loading_owned_without_levels_starts_at_level_one()
        {
            var equipped = new int[SkillTable.EquipSlots];
            for (int i = 0; i < equipped.Length; i++) equipped[i] = SkillTable.None;
            equipped[0] = 2;
            service.LoadFrom(Roster(2), equipped);
            Assert.AreEqual(1, service.Level(2));
            Assert.IsTrue(service.Owns(2));
        }

        [Test]
        public void Loading_levels_keeps_the_stored_value()
        {
            var levels = new int[SkillTable.Count];
            levels[2] = 6;
            var equipped = new int[SkillTable.EquipSlots];
            for (int i = 0; i < equipped.Length; i++) equipped[i] = SkillTable.None;
            equipped[0] = 2;
            service.LoadFrom(Roster(2), levels, equipped);
            Assert.AreEqual(6, service.Level(2));
        }

        [Test]
        public void Unowned_skills_are_stripped_from_the_loadout()
        {
            var equipped = new int[SkillTable.EquipSlots];
            for (int i = 0; i < equipped.Length; i++) equipped[i] = SkillTable.None;
            equipped[0] = 5;
            service.LoadFrom(Roster(2), equipped);
            Assert.AreEqual(SkillTable.None, service.EquippedId(0));
        }

        [Test]
        public void Cooldown_keeps_running_without_a_live_target()
        {
            var levels = new int[SkillTable.Count];
            levels[2] = 1;
            var equipped = new int[SkillTable.EquipSlots];
            for (int i = 0; i < equipped.Length; i++) equipped[i] = SkillTable.None;
            equipped[0] = 2;
            service.LoadFrom(null, levels, equipped);

            var field = typeof(SkillService).GetField("cooldowns", BindingFlags.Instance | BindingFlags.NonPublic);
            var cooldowns = (float[])field.GetValue(service);
            cooldowns[0] = 5f;

            service.Tick(1.5f);

            Assert.AreEqual(3.5f, service.Cooldown(0), 0.0001f);
        }
    }
}
