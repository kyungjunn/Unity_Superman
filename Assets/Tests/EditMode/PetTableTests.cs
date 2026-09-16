using GrowNa.Pet;
using GrowNa.Skill;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class PetTableTests
    {
        static bool[] NoPets() => new bool[PetTable.Count];

        [Test]
        public void Rarity_weights_sum_to_one()
        {
            double sum = 0;
            for (int i = 0; i < PetTable.RarityCount; i++) sum += PetTable.RarityChance((PetRarity)i);
            Assert.AreEqual(1.0, sum, 1e-9);
        }

        [Test]
        public void Rarer_pets_are_less_likely()
        {
            for (int i = 1; i < PetTable.RarityCount; i++)
                Assert.Less(PetTable.RarityChance((PetRarity)i), PetTable.RarityChance((PetRarity)(i - 1)));
        }

        [Test]
        public void Every_roll_lands_on_a_real_pet()
        {
            for (int i = 0; i <= 100; i++)
                Assert.IsTrue(PetTable.IsValid(PetTable.Roll(i / 100.0, i / 100.0)), $"roll {i / 100.0}");
        }

        [Test]
        public void A_roll_lands_inside_the_rarity_it_picked()
        {
            for (int i = 0; i <= 100; i++)
            {
                double roll = i / 100.0;
                Assert.AreEqual(PetTable.RollRarity(roll), PetTable.Get(PetTable.Roll(roll, 0.5)).rarity);
            }
        }

        [Test]
        public void Every_rarity_has_at_least_one_pet()
        {
            for (int r = 0; r < PetTable.RarityCount; r++)
            {
                bool found = false;
                for (int i = 0; i < PetTable.Count && !found; i++)
                    found = PetTable.Get(i).rarity == (PetRarity)r;
                Assert.IsTrue(found, $"등급 {r} 에 펫이 없다");
            }
        }

        [Test]
        public void Rarer_pets_give_a_bigger_total_bonus()
        {
            double weakestLegend = double.MaxValue;
            double strongestCommon = 0;

            for (int i = 0; i < PetTable.Count; i++)
            {
                var def = PetTable.Get(i);
                double total = def.attackBonus + def.healthBonus;
                if (def.rarity == PetRarity.Legend) weakestLegend = System.Math.Min(weakestLegend, total);
                if (def.rarity == PetRarity.Common) strongestCommon = System.Math.Max(strongestCommon, total);
            }

            Assert.Greater(weakestLegend, strongestCommon);
        }

        [Test]
        public void No_pets_means_no_active_pet()
            => Assert.AreEqual(PetTable.None, PetTable.BestOwned(NoPets()));

        [Test]
        public void A_null_roster_is_safe()
            => Assert.AreEqual(PetTable.None, PetTable.BestOwned(null));

        [Test]
        public void The_active_pet_is_the_rarest_owned()
        {
            var owned = NoPets();
            owned[0] = true;
            Assert.AreEqual(0, PetTable.BestOwned(owned));

            owned[PetTable.Count - 1] = true;
            Assert.AreEqual(PetTable.Count - 1, PetTable.BestOwned(owned));
        }

        [Test]
        public void Rarity_beats_raw_bonus_when_picking_the_active_pet()
        {
            for (int high = 0; high < PetTable.Count; high++)
                for (int low = 0; low < PetTable.Count; low++)
                {
                    if (PetTable.Get(high).rarity <= PetTable.Get(low).rarity) continue;
                    Assert.Greater(PetTable.Strength(high), PetTable.Strength(low),
                                   $"{high} 가 {low} 보다 높은 등급인데 점수가 낮다");
                }
        }

        [Test]
        public void Every_pet_has_a_name_and_a_positive_bonus()
        {
            for (int i = 0; i < PetTable.Count; i++)
            {
                var def = PetTable.Get(i);
                Assert.IsNotEmpty(def.name);
                Assert.Greater(def.attackBonus, 0.0);
                Assert.Greater(def.healthBonus, 0.0);
                Assert.Greater(def.AttackMultiplier, 1.0);
                Assert.Greater(def.HealthMultiplier, 1.0);
            }
        }

        [Test]
        public void Pets_cost_more_diamonds_than_skills()
            => Assert.Greater(PetTable.DrawCost, SkillTable.DrawCost);

        [Test]
        public void Level_one_does_not_change_the_bonus()
            => Assert.AreEqual(1.0, PetTable.LevelFactor(1), 1e-9);

        [Test]
        public void Max_level_matches_the_skill_curve()
            => Assert.AreEqual(SkillTable.LevelFactor(SkillTable.MaxLevel),
                               PetTable.LevelFactor(PetTable.MaxLevel), 1e-9);

        [Test]
        public void ApplyPull_turns_an_unowned_pet_into_level_one()
        {
            Assert.AreEqual(1, PetTable.ApplyPull(0, out bool refunded));
            Assert.IsFalse(refunded);
        }

        [Test]
        public void A_maxed_pet_pull_refunds_and_stays_capped()
        {
            Assert.AreEqual(PetTable.MaxLevel, PetTable.ApplyPull(PetTable.MaxLevel, out bool refunded));
            Assert.IsTrue(refunded);
        }

        [Test]
        public void Scaled_bonus_is_base_times_level_factor()
        {
            var def = PetTable.Get(0);
            Assert.AreEqual(def.AttackMultiplier, PetTable.ScaledAttackMultiplier(0, 1), 1e-9);
            Assert.AreEqual(1.0 + def.attackBonus * PetTable.LevelFactor(4),
                            PetTable.ScaledAttackMultiplier(0, 4), 1e-9);
        }
    }
}
