using GrowNa.Core;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class QuestTableTests
    {
        [Test]
        public void Every_quest_has_a_title_target_and_reward()
        {
            for (int i = 0; i < QuestTable.Count; i++)
            {
                var def = QuestTable.Get(i);
                Assert.IsNotEmpty(def.title);
                Assert.IsNotEmpty(def.unit);
                Assert.Greater(def.target, 0);
                Assert.Greater(def.gem, 0.0);
            }
        }

        [Test]
        public void Kinds_and_definitions_line_up()
        {
            for (int i = 0; i < QuestTable.Count; i++)
                Assert.AreEqual((QuestKind)i, QuestTable.Get(i).kind);
        }

        [Test]
        public void A_quest_is_ready_only_at_its_target()
        {
            int target = QuestTable.Target(QuestKind.Kill);
            Assert.IsFalse(QuestTable.IsReady(QuestKind.Kill, target - 1));
            Assert.IsTrue(QuestTable.IsReady(QuestKind.Kill, target));
            Assert.IsTrue(QuestTable.IsReady(QuestKind.Kill, target + 5));
        }

        [Test]
        public void Remaining_never_goes_negative()
        {
            Assert.AreEqual(0, QuestTable.Remaining(QuestKind.Offer, QuestTable.Target(QuestKind.Offer) + 3));
            Assert.AreEqual(QuestTable.Target(QuestKind.Offer), QuestTable.Remaining(QuestKind.Offer, 0));
        }

        [Test]
        public void Ratio_stays_inside_the_bar()
        {
            Assert.AreEqual(0.0, QuestTable.Ratio(QuestKind.PetDraw, 0), 1e-6);
            Assert.AreEqual(1.0, QuestTable.Ratio(QuestKind.PetDraw, QuestTable.Target(QuestKind.PetDraw) * 3), 1e-6);
        }

        [Test]
        public void Drawing_quests_pay_back_less_than_they_cost()
        {
            Assert.Less(QuestTable.Gem(QuestKind.SkillDraw),
                        QuestTable.Target(QuestKind.SkillDraw) * Skill.SkillTable.DrawCost);
            Assert.Less(QuestTable.Gem(QuestKind.PetDraw),
                        QuestTable.Target(QuestKind.PetDraw) * Pet.PetTable.DrawCost);
        }
    }
}
