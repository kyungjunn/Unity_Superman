using GrowNa.Core;
using GrowNa.Pet;
using NUnit.Framework;
using UnityEngine;

namespace GrowNa.Tests
{
    public class PetServiceTests
    {
        PetService service;

        [SetUp]
        public void SetUp()
        {
            service = new GameObject("PetService").AddComponent<PetService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (service != null) Object.DestroyImmediate(service.gameObject);
        }

        static bool[] Roster(params int[] ids)
        {
            var owned = new bool[PetTable.Count];
            foreach (int id in ids) owned[id] = true;
            return owned;
        }

        [Test]
        public void A_fresh_service_has_no_active_pet()
        {
            Assert.IsFalse(service.HasPet);
            Assert.AreEqual(PetTable.None, service.ActiveId);
        }

        [Test]
        public void Loading_an_owned_pet_keeps_it_equipped()
        {
            service.LoadFrom(Roster(3), 3);
            Assert.IsTrue(service.HasPet);
            Assert.AreEqual(3, service.ActiveId);
        }

        [Test]
        public void Loading_an_unowned_equip_slot_clears_it()
        {
            service.LoadFrom(Roster(1), 3);
            Assert.IsFalse(service.HasPet);
            Assert.AreEqual(PetTable.None, service.ActiveId);
        }

        [Test]
        public void Equip_fails_when_the_pet_is_not_owned()
        {
            service.LoadFrom(Roster(1), PetTable.None);
            Assert.IsFalse(service.TryEquip(2));
            Assert.AreEqual(PetTable.None, service.ActiveId);
        }

        [Test]
        public void Equip_swaps_to_another_owned_pet()
        {
            service.LoadFrom(Roster(1, 4), 1);
            Assert.IsTrue(service.TryEquip(4));
            Assert.AreEqual(4, service.ActiveId);
        }

        [Test]
        public void Equip_is_idempotent()
        {
            service.LoadFrom(Roster(1), 1);
            Assert.IsFalse(service.TryEquip(1));
            Assert.AreEqual(1, service.ActiveId);
        }

        [Test]
        public void Unequip_clears_the_active_pet()
        {
            service.LoadFrom(Roster(2), 2);
            Assert.IsTrue(service.Unequip());
            Assert.IsFalse(service.HasPet);
            Assert.IsFalse(service.Unequip());
        }

        [Test]
        public void EquipBest_picks_the_rarest_owned_pet()
        {
            int common = 0;
            int rarest = PetTable.Count - 1;
            service.LoadFrom(Roster(common, rarest), common);
            Assert.IsTrue(service.EquipBest());
            Assert.AreEqual(rarest, service.ActiveId);
            Assert.IsFalse(service.EquipBest());
        }

        [Test]
        public void Bonus_is_none_when_unequipped()
        {
            service.LoadFrom(Roster(0), PetTable.None);
            var bonus = service.CurrentBonus();
            Assert.AreEqual(1.0, bonus.attackMultiplier);
            Assert.AreEqual(1.0, bonus.healthMultiplier);
        }

        [Test]
        public void Bonus_matches_the_equipped_pet()
        {
            service.LoadFrom(Roster(3), 3);
            var def = PetTable.Get(3);
            var bonus = service.CurrentBonus();
            Assert.AreEqual(def.AttackMultiplier, bonus.attackMultiplier);
            Assert.AreEqual(def.HealthMultiplier, bonus.healthMultiplier);
        }

        [Test]
        public void Loading_owned_without_levels_starts_at_level_one()
        {
            service.LoadFrom(Roster(3), 3);
            Assert.AreEqual(1, service.Level(3));
        }

        [Test]
        public void Bonus_grows_with_level()
        {
            var levels = new int[PetTable.Count];
            levels[3] = 5;
            service.LoadFrom(Roster(3), levels, 3);
            Assert.AreEqual(5, service.Level(3));
            Assert.AreEqual(PetTable.ScaledAttackMultiplier(3, 5), service.CurrentBonus().attackMultiplier, 1e-9);
            Assert.AreEqual(PetTable.ScaledHealthMultiplier(3, 5), service.CurrentBonus().healthMultiplier, 1e-9);
        }
    }
}
