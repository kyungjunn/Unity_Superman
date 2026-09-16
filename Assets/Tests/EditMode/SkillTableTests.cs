using GrowNa.Skill;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class SkillTableTests
    {
        [Test]
        public void Rarity_weights_sum_to_one()
        {
            double sum = 0;
            for (int i = 0; i < SkillTable.RarityCount; i++) sum += SkillTable.RarityChance((SkillRarity)i);
            Assert.AreEqual(1.0, sum, 1e-9);
        }

        [Test]
        public void Rarer_skills_are_less_likely()
        {
            for (int i = 1; i < SkillTable.RarityCount; i++)
                Assert.Less(SkillTable.RarityChance((SkillRarity)i), SkillTable.RarityChance((SkillRarity)(i - 1)));
        }

        [Test]
        public void The_lowest_roll_gives_the_commonest_rarity()
            => Assert.AreEqual(SkillRarity.Common, SkillTable.RollRarity(0.0));

        [Test]
        public void The_highest_roll_gives_legend()
            => Assert.AreEqual(SkillRarity.Legend, SkillTable.RollRarity(0.9999));

        [Test]
        public void Every_roll_lands_on_a_real_skill()
        {
            for (int i = 0; i <= 100; i++)
            {
                int id = SkillTable.Roll(i / 100.0, i / 100.0);
                Assert.IsTrue(SkillTable.IsValid(id), $"roll {i / 100.0} -> {id}");
            }
        }

        [Test]
        public void A_roll_lands_inside_the_rarity_it_picked()
        {
            for (int i = 0; i <= 100; i++)
            {
                double roll = i / 100.0;
                var expected = SkillTable.RollRarity(roll);
                Assert.AreEqual(expected, SkillTable.Get(SkillTable.Roll(roll, 0.5)).rarity);
            }
        }

        [Test]
        public void Every_rarity_has_at_least_one_skill()
        {
            for (int r = 0; r < SkillTable.RarityCount; r++)
            {
                bool found = false;
                for (int i = 0; i < SkillTable.Count && !found; i++)
                    found = SkillTable.Get(i).rarity == (SkillRarity)r;
                Assert.IsTrue(found, $"등급 {r} 에 스킬이 없다");
            }
        }

        [Test]
        public void Rarer_skills_hit_harder_per_second()
        {
            double weakestLegend = double.MaxValue;
            double strongestCommon = 0;

            for (int i = 0; i < SkillTable.Count; i++)
            {
                var def = SkillTable.Get(i);
                double perSecond = def.damageMultiplier / def.cooldown;
                if (def.rarity == SkillRarity.Legend) weakestLegend = System.Math.Min(weakestLegend, perSecond);
                if (def.rarity == SkillRarity.Common) strongestCommon = System.Math.Max(strongestCommon, perSecond);
            }

            Assert.Greater(weakestLegend, strongestCommon);
        }

        [Test]
        public void Every_skill_has_a_name_and_a_positive_cooldown()
        {
            for (int i = 0; i < SkillTable.Count; i++)
            {
                var def = SkillTable.Get(i);
                Assert.IsNotEmpty(def.name);
                Assert.Greater(def.cooldown, 0f);
                Assert.Greater(def.damageMultiplier, 0.0);
            }
        }

        [Test]
        public void The_empty_slot_marker_is_not_a_skill_id()
            => Assert.IsFalse(SkillTable.IsValid(SkillTable.None));

        [Test]
        public void There_are_six_equip_slots()
            => Assert.AreEqual(6, SkillTable.EquipSlots);

        [Test]
        public void Rarity_beats_raw_multiplier_when_ranking_for_auto_equip()
        {
            for (int high = 0; high < SkillTable.Count; high++)
                for (int low = 0; low < SkillTable.Count; low++)
                {
                    if (SkillTable.Get(high).rarity <= SkillTable.Get(low).rarity) continue;
                    Assert.Greater(SkillTable.Strength(high), SkillTable.Strength(low),
                                   $"{high} 가 {low} 보다 높은 등급인데 점수가 낮다");
                }
        }

        [Test]
        public void Within_one_rarity_the_bigger_multiplier_ranks_higher()
        {
            for (int a = 0; a < SkillTable.Count; a++)
                for (int b = 0; b < SkillTable.Count; b++)
                {
                    if (SkillTable.Get(a).rarity != SkillTable.Get(b).rarity) continue;
                    if (SkillTable.Get(a).damageMultiplier <= SkillTable.Get(b).damageMultiplier) continue;
                    Assert.Greater(SkillTable.Strength(a), SkillTable.Strength(b));
                }
        }

        [Test]
        public void An_invalid_id_ranks_below_everything()
        {
            for (int i = 0; i < SkillTable.Count; i++)
                Assert.Greater(SkillTable.Strength(i), SkillTable.Strength(SkillTable.None));
        }

        [Test]
        public void Level_one_does_not_change_damage()
            => Assert.AreEqual(1.0, SkillTable.LevelFactor(1), 1e-9);

        [Test]
        public void Max_level_is_linear_not_exponential()
            => Assert.AreEqual(1.0 + SkillTable.LevelGrowth * (SkillTable.MaxLevel - 1),
                               SkillTable.LevelFactor(SkillTable.MaxLevel), 1e-9);

        [Test]
        public void Unowned_and_overcap_levels_are_clamped()
        {
            Assert.AreEqual(0, SkillTable.ClampLevel(-3));
            Assert.AreEqual(0.0, SkillTable.LevelFactor(0), 1e-9);
            Assert.AreEqual(SkillTable.MaxLevel, SkillTable.ClampLevel(99));
            Assert.AreEqual(SkillTable.LevelFactor(SkillTable.MaxLevel), SkillTable.LevelFactor(99), 1e-9);
        }

        [Test]
        public void ApplyPull_turns_an_unowned_skill_into_level_one()
        {
            int next = SkillTable.ApplyPull(0, out bool refunded);
            Assert.AreEqual(1, next);
            Assert.IsFalse(refunded);
        }

        [Test]
        public void ApplyPull_raises_level_until_the_cap()
        {
            Assert.AreEqual(2, SkillTable.ApplyPull(1, out bool refunded));
            Assert.IsFalse(refunded);
            Assert.AreEqual(SkillTable.MaxLevel, SkillTable.ApplyPull(SkillTable.MaxLevel - 1, out refunded));
            Assert.IsFalse(refunded);
        }

        [Test]
        public void A_maxed_pull_refunds_and_stays_capped()
        {
            int next = SkillTable.ApplyPull(SkillTable.MaxLevel, out bool refunded);
            Assert.AreEqual(SkillTable.MaxLevel, next);
            Assert.IsTrue(refunded);
        }

        [Test]
        public void Scaled_damage_is_base_times_level_factor()
        {
            var def = SkillTable.Get(0);
            Assert.AreEqual(def.damageMultiplier, SkillTable.ScaledDamage(0, 1), 1e-9);
            Assert.AreEqual(def.damageMultiplier * SkillTable.LevelFactor(5), SkillTable.ScaledDamage(0, 5), 1e-9);
        }
    }
}
