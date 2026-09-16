using GrowNa.Core;
using GrowNa.Persistence;
using GrowNa.Composition;
using NUnit.Framework;
using UnityEngine;

namespace GrowNa.Tests
{
    public class SceneRuntimeBootstrapTests
    {
        [Test]
        public void Direct_initialize_loads_new_game_and_is_idempotent()
        {
            var host = new GameObject("boot");
            var wallet = host.AddComponent<Wallet>();
            var stats = host.AddComponent<PlayerStats>();
            var quests = host.AddComponent<QuestService>();
            var upgrades = host.AddComponent<UpgradeService>();
            var save = host.AddComponent<SaveService>();
            var boot = host.AddComponent<SceneRuntimeBootstrap>();
            boot.BindCore(wallet, stats, upgrades, quests, save);

            var store = new MemorySaveStore();
            boot.ConfigurePersistence(store, new FixedClock(System.DateTime.UtcNow), new SystemGameRandom(1));
            var first = boot.Initialize();
            Assert.AreEqual(BootstrapPhase.Ready, first.Phase);
            Assert.AreEqual(Wallet.StartingGrain, wallet.Grain, 1e-9);
            Assert.AreEqual(1, boot.ReadCount);

            var second = boot.Initialize();
            Assert.AreEqual(BootstrapPhase.Ready, second.Phase);
            Assert.AreEqual(1, boot.ReadCount);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void Invalid_v8_blocks_writes()
        {
            var host = new GameObject("boot-fail");
            var wallet = host.AddComponent<Wallet>();
            var stats = host.AddComponent<PlayerStats>();
            var save = host.AddComponent<SaveService>();
            var boot = host.AddComponent<SceneRuntimeBootstrap>();
            boot.BindCore(wallet, stats, null, null, save);

            var store = new MemorySaveStore();
            store.Seed(System.Text.Encoding.UTF8.GetBytes("{\"version\":8}"));
            boot.ConfigurePersistence(store, new FixedClock(System.DateTime.UtcNow));
            var result = boot.Initialize();
            Assert.AreEqual(BootstrapPhase.Failed, result.Phase);
            Assert.AreEqual(SaveWriteGate.Blocked, save.Gate);
            Assert.AreEqual(0, store.WriteCount);
            Object.DestroyImmediate(host);
        }
    }
}
