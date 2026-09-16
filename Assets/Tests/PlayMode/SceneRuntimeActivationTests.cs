using System.Collections;
using GrowNa.Composition;
using GrowNa.Core;
using GrowNa.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GrowNa.Tests
{
    public class SceneRuntimeActivationTests
    {
        [UnityTest]
        public IEnumerator Start_makes_ready_without_direct_initialize()
        {
            var host = new GameObject("play-boot");
            host.SetActive(false);
            var wallet = host.AddComponent<Wallet>();
            var stats = host.AddComponent<PlayerStats>();
            var save = host.AddComponent<SaveService>();
            var boot = host.AddComponent<SceneRuntimeBootstrap>();
            boot.BindCore(wallet, stats, null, null, save);
            var store = new MemorySaveStore();
            boot.ConfigurePersistence(store, new FixedClock(System.DateTime.UtcNow), new SystemGameRandom(2));
            Assert.AreEqual(BootstrapPhase.Cold, boot.Phase);
            Assert.AreEqual(0, boot.ReadCount);

            host.SetActive(true);
            yield return null;
            yield return null;

            Assert.AreEqual(BootstrapPhase.Ready, boot.Phase);
            Assert.AreEqual(1, boot.ReadCount);
            Object.Destroy(host);
            yield return null;
        }
    }
}
