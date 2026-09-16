using System.Reflection;
using GrowNa.Battle;
using GrowNa.Core;
using GrowNa.Monsters;
using GrowNa.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace GrowNa.Tests
{
    public class RuntimeLifecycleTests
    {
        [Test]
        public void Blocked_bootstrap_does_not_open_on_second_initialize()
        {
            var host = new GameObject("life");
            var wallet = host.AddComponent<Wallet>();
            var stats = host.AddComponent<PlayerStats>();
            var save = host.AddComponent<SaveService>();
            var boot = host.AddComponent<GrowNa.Composition.SceneRuntimeBootstrap>();
            boot.BindCore(wallet, stats, null, null, save);
            var store = new MemorySaveStore();
            store.Seed(System.Text.Encoding.UTF8.GetBytes("{\"version\":8}"));
            boot.ConfigurePersistence(store, new FixedClock(System.DateTime.UtcNow));
            Assert.AreEqual(BootstrapPhase.Failed, boot.Initialize().Phase);
            Assert.AreEqual(SaveWriteGate.Blocked, save.Gate);
            Assert.AreEqual(BootstrapPhase.Failed, boot.Initialize().Phase);
            Assert.AreEqual(SaveWriteGate.Blocked, save.Gate);
            Assert.AreEqual(0, store.WriteCount);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void DamagePopup_and_UiFont_have_no_static_spawn_or_cache()
        {
            Assert.IsNull(typeof(DamagePopup).GetMethod("Spawn", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(typeof(UiFont).GetMethod("Get", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(typeof(MonsterFactory).GetMethod("Create", BindingFlags.Public | BindingFlags.Static));
        }
    }
}
