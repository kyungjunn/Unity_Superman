using GrowNa.Core;
using NUnit.Framework;
using UnityEngine;

namespace GrowNa.Tests
{
    public class QuestServiceTests
    {
        GameObject host;
        QuestService quests;
        Wallet wallet;
        double paidGems;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Quests");
            quests = host.AddComponent<QuestService>();
            wallet = host.AddComponent<Wallet>();
            quests.Bind(wallet);
            paidGems = 0;
            quests.GemRewarded += gem => paidGems += gem;
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
        }

        static void Fill(QuestService service, QuestKind kind)
            => service.Report(kind, QuestTable.Target(kind));

        [Test]
        public void A_fresh_service_has_nothing_to_claim()
        {
            Assert.AreEqual(0, quests.ReadyCount);
            Assert.IsFalse(quests.TryClaim(QuestKind.Kill));
        }

        [Test]
        public void Progress_makes_a_quest_claimable()
        {
            Fill(quests, QuestKind.Offer);
            Assert.IsTrue(quests.IsReady(QuestKind.Offer));
            Assert.AreEqual(1, quests.ReadyCount);
        }

        [Test]
        public void Claiming_pays_gems_and_counts_a_round()
        {
            Fill(quests, QuestKind.Offer);
            Assert.IsTrue(quests.TryClaim(QuestKind.Offer));

            Assert.AreEqual(QuestTable.Gem(QuestKind.Offer), paidGems, 1e-6);
            Assert.AreEqual(QuestTable.Gem(QuestKind.Offer), wallet.Gem, 1e-6);
            Assert.AreEqual(1, quests.Rounds(QuestKind.Offer));
            Assert.IsFalse(quests.IsReady(QuestKind.Offer));
        }

        [Test]
        public void Overflow_carries_into_the_next_round()
        {
            quests.Report(QuestKind.Offer, QuestTable.Target(QuestKind.Offer) + 3);
            Assert.IsTrue(quests.TryClaim(QuestKind.Offer));
            Assert.AreEqual(3, quests.Progress(QuestKind.Offer));
        }

        [Test]
        public void A_kill_feeds_the_kill_quest_only()
        {
            quests.ReportKill(false);
            Assert.AreEqual(1, quests.Progress(QuestKind.Kill));
            Assert.AreEqual(0, quests.Progress(QuestKind.StageClear));
        }

        [Test]
        public void A_boss_kill_also_clears_a_stage()
        {
            quests.ReportKill(true);
            Assert.AreEqual(1, quests.Progress(QuestKind.Kill));
            Assert.AreEqual(1, quests.Progress(QuestKind.StageClear));
        }

        [Test]
        public void Claim_all_takes_every_ready_quest()
        {
            Fill(quests, QuestKind.Offer);
            Fill(quests, QuestKind.PetDraw);

            Assert.AreEqual(2, quests.ClaimAll());
            Assert.AreEqual(QuestTable.Gem(QuestKind.Offer) + QuestTable.Gem(QuestKind.PetDraw), paidGems, 1e-6);
            Assert.AreEqual(0, quests.ReadyCount);
        }

        [Test]
        public void Zero_or_negative_reports_do_nothing()
        {
            quests.Report(QuestKind.Kill, 0);
            quests.Report(QuestKind.Kill, -5);
            Assert.AreEqual(0, quests.Progress(QuestKind.Kill));
        }

        [Test]
        public void Loading_restores_progress_and_rounds()
        {
            var progress = new int[QuestTable.Count];
            var rounds = new int[QuestTable.Count];
            progress[(int)QuestKind.SkillDraw] = 4;
            rounds[(int)QuestKind.SkillDraw] = 7;

            quests.LoadFrom(progress, rounds);

            Assert.AreEqual(4, quests.Progress(QuestKind.SkillDraw));
            Assert.AreEqual(7, quests.Rounds(QuestKind.SkillDraw));
        }

        [Test]
        public void Loading_a_null_or_broken_save_is_safe()
        {
            Fill(quests, QuestKind.Kill);
            quests.LoadFrom(null, null);
            Assert.AreEqual(0, quests.Progress(QuestKind.Kill));

            quests.LoadFrom(new[] { -3 }, new[] { -9 });
            Assert.AreEqual(0, quests.Progress(QuestKind.Kill));
            Assert.AreEqual(0, quests.Rounds(QuestKind.Kill));
        }

        [Test]
        public void Snapshots_round_trip_through_load()
        {
            quests.Report(QuestKind.StageClear, 2);
            Fill(quests, QuestKind.Kill);
            quests.TryClaim(QuestKind.Kill);

            var restoredHost = new GameObject("Restored");
            var restored = restoredHost.AddComponent<QuestService>();
            restored.Bind(restoredHost.AddComponent<Wallet>());
            restored.LoadFrom(quests.ProgressSnapshot(), quests.RoundsSnapshot());

            Assert.AreEqual(2, restored.Progress(QuestKind.StageClear));
            Assert.AreEqual(1, restored.Rounds(QuestKind.Kill));
            Object.DestroyImmediate(restoredHost);
        }
    }
}
