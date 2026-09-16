using GrowNa.Battle;
using GrowNa.Core;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class StageProgressionTests
    {
        [Test]
        public void Five_kills_reach_the_boss()
            => Assert.AreEqual(5, StageTable.MonstersPerStage);

        [Test]
        public void Kills_spread_evenly_over_the_ranks()
        {
            for (int kills = 0; kills < StageTable.MonstersPerStage; kills++)
                Assert.AreEqual(kills * StageTable.MonsterRanks / StageTable.MonstersPerStage,
                                StageTable.RankOf(kills), $"{kills}처치");
        }

        [Test]
        public void Rank_never_goes_backwards()
        {
            for (int kills = 1; kills <= StageTable.MonstersPerStage; kills++)
                Assert.GreaterOrEqual(StageTable.RankOf(kills), StageTable.RankOf(kills - 1));
        }

        [Test]
        public void Remaining_counts_down_to_the_boss()
        {
            Assert.AreEqual(StageTable.MonstersPerStage, StageTable.Remaining(0));
            Assert.AreEqual(1, StageTable.Remaining(StageTable.MonstersPerStage - 1));
            Assert.AreEqual(0, StageTable.Remaining(StageTable.MonstersPerStage));
            Assert.AreEqual(0, StageTable.Remaining(StageTable.MonstersPerStage + 3));
        }

        [Test]
        public void A_wave_brings_two_or_three_monsters()
        {
            Assert.AreEqual(StageTable.MinWaveSize, StageTable.WaveSize(0, 0.0));
            Assert.AreEqual(StageTable.MaxWaveSize, StageTable.WaveSize(0, 0.99));
        }

        [Test]
        public void A_wave_never_overshoots_the_boss()
        {
            for (int kills = 0; kills <= StageTable.MonstersPerStage; kills++)
                for (double roll = 0.0; roll < 1.0; roll += 0.25)
                    Assert.LessOrEqual(StageTable.WaveSize(kills, roll), StageTable.Remaining(kills),
                                       $"{kills}처치 roll={roll}");
        }

        [Test]
        public void No_wave_spawns_once_the_boss_is_due()
            => Assert.AreEqual(0, StageTable.WaveSize(StageTable.MonstersPerStage, 0.5));

        [Test]
        public void Every_wave_before_the_boss_has_at_least_one_monster()
        {
            for (int kills = 0; kills < StageTable.MonstersPerStage; kills++)
                Assert.GreaterOrEqual(StageTable.WaveSize(kills, 0.5), 1, $"{kills}처치");
        }

        [Test]
        public void Waves_of_the_minimum_size_still_reach_the_boss()
        {
            int kills = 0, waves = 0;
            while (StageTable.Remaining(kills) > 0 && waves < 100)
            {
                kills += StageTable.WaveSize(kills, 0.0);
                waves++;
            }
            Assert.AreEqual(StageTable.MonstersPerStage, kills);
            Assert.Less(waves, 100, "무리 진행이 멈추지 않았다");
        }

        [Test]
        public void Rank_is_clamped_past_the_last_monster()
            => Assert.AreEqual(StageTable.MonsterRanks - 1, StageTable.RankOf(StageTable.MonstersPerStage + 5));

        [Test]
        public void Higher_rank_monsters_are_stronger()
        {
            Assert.Greater(StageTable.MonsterHp(1, 1, 4), StageTable.MonsterHp(1, 1, 0));
            Assert.Greater(StageTable.MonsterAttack(1, 1, 4), StageTable.MonsterAttack(1, 1, 0));
        }

        [Test]
        public void Every_normal_kill_pays_gold_and_exp()
        {
            for (int rank = 0; rank < StageTable.MonsterRanks; rank++)
            {
                var reward = StageTable.KillReward(1, 1, rank, 0.99);
                Assert.Greater(reward.gold, 0, $"rank {rank} 골드");
                Assert.Greater(reward.exp, 0, $"rank {rank} 경험치");
            }
        }

        [Test]
        public void Normal_kill_grain_is_a_single_drop_on_a_hit_roll()
        {
            var hit = StageTable.KillReward(1, 1, 0, StageTable.GrainDropChance - 0.001);
            var miss = StageTable.KillReward(1, 1, 0, StageTable.GrainDropChance + 0.001);

            Assert.AreEqual(StageTable.GrainDropPerKill, hit.grain);
            Assert.AreEqual(0, miss.grain);
        }

        [Test]
        public void Boss_always_drops_five_or_six_grain()
        {
            Assert.AreEqual(StageTable.BossGrainMin, StageTable.BossReward(1, 1, 0.1).grain);
            Assert.AreEqual(StageTable.BossGrainMax, StageTable.BossReward(1, 1, 0.9).grain);
        }

        [Test]
        public void Boss_pays_far_more_than_a_normal_kill()
        {
            var boss = StageTable.BossReward(3, 4, 0.5);
            var normal = StageTable.KillReward(3, 4, StageTable.MonsterRanks - 1, 0.5);

            Assert.Greater(boss.gold, normal.gold);
            Assert.Greater(boss.exp, normal.exp);
            Assert.Greater(boss.wick, 0);
        }

        [Test]
        public void First_boss_is_softer_than_the_later_ones()
        {
            Assert.Less(StageTable.BossHpMultiplier(1, 1), StageTable.BossHpMultiplier(1, 5));
            Assert.AreEqual(8.0, StageTable.BossHpMultiplier(1, 5), 1e-9);
            Assert.AreEqual(8.0, StageTable.BossHpMultiplier(2, 3), 1e-9);
        }

        [Test]
        public void Boss_ramp_is_monotonic_over_the_first_five_stages()
        {
            for (int stage = 1; stage < 5; stage++)
                Assert.Less(StageTable.BossHpMultiplier(1, stage), StageTable.BossHpMultiplier(1, stage + 1));
        }

        [Test]
        public void Expected_grain_matches_the_drop_chance()
            => Assert.AreEqual(StageTable.GrainDropChance * StageTable.GrainDropPerKill,
                               StageTable.ExpectedGrainPerKill, 1e-9);

        [Test]
        public void Every_rank_has_a_name()
        {
            for (int rank = 0; rank < StageTable.MonsterRanks; rank++)
                Assert.IsNotEmpty(StageTable.RankName(rank));
        }

        [Test]
        public void Required_exp_grows_with_level()
            => Assert.Greater(ExpTable.Required(10), ExpTable.Required(1));

        [Test]
        public void Max_level_can_never_fill_its_bar()
            => Assert.IsTrue(double.IsInfinity(ExpTable.Required(ExpTable.MaxLevel)));

        [Test]
        public void Exp_ratio_is_clamped()
        {
            Assert.AreEqual(0.0, ExpTable.Ratio(1, -50), 1e-9);
            Assert.AreEqual(1.0, ExpTable.Ratio(1, ExpTable.Required(1) * 5), 1e-9);
            Assert.AreEqual(0.5, ExpTable.Ratio(1, ExpTable.Required(1) * 0.5), 1e-9);
        }

        [Test]
        public void Boss_exp_beats_the_toughest_normal_monster()
            => Assert.Greater(ExpTable.BossExp(1, 4), ExpTable.MonsterExp(1, 4));
    }
}
