using GrowNa.Battle;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class OfflineRewardsTests
    {
        [Test]
        public void Short_absence_pays_nothing()
        {
            var result = OfflineRewards.Compute(1, 1, 59);
            Assert.IsFalse(result.Any);
            Assert.AreEqual(0, result.gold);
        }

        [Test]
        public void One_hour_pays_something()
        {
            var result = OfflineRewards.Compute(1, 1, 3600);
            Assert.IsTrue(result.Any);
            Assert.Greater(result.gold, 0);
            Assert.Greater(result.grain, 0);
            Assert.IsFalse(result.capped);
        }

        [Test]
        public void Reward_is_capped_at_eight_hours()
        {
            var eight = OfflineRewards.Compute(1, 1, OfflineRewards.MaxSeconds);
            var twoDays = OfflineRewards.Compute(1, 1, 48 * 3600);
            Assert.AreEqual(eight.gold, twoDays.gold);
            Assert.AreEqual(OfflineRewards.MaxSeconds, twoDays.seconds);
            Assert.IsTrue(twoDays.capped);
            Assert.IsFalse(eight.capped);
        }

        [Test]
        public void Later_stages_pay_more()
        {
            var early = OfflineRewards.Compute(1, 1, 3600);
            var late = OfflineRewards.Compute(3, 5, 3600);
            Assert.Greater(late.gold, early.gold);
        }

        [Test]
        public void Nan_elapsed_is_ignored()
            => Assert.IsFalse(OfflineRewards.Compute(1, 1, double.NaN).Any);
    }
}
