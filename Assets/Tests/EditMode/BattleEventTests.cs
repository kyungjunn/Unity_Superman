using System.Collections.Generic;
using System.Reflection;
using GrowNa.Battle;
using GrowNa.Core;
using GrowNa.Monsters;
using NUnit.Framework;
using UnityEngine;

namespace GrowNa.Tests
{
    public class BattleEventTests
    {
        [Test]
        public void Skill_strike_applies_without_presenter()
        {
            var host = new GameObject("evt");
            var stats = host.AddComponent<PlayerStats>();
            var wallet = host.AddComponent<Wallet>();
            var quests = host.AddComponent<QuestService>();
            var battle = host.AddComponent<BattleManager>();
            var factory = host.AddComponent<MonsterFactory>();
            var playerGo = new GameObject("player");
            var player = playerGo.transform;
            player.position = Vector3.zero;
            Set(battle, "playerTransform", player);
            battle.Bind(stats, wallet, quests, new SystemGameRandom(1), factory);
            stats.EnsureInitialized();

            var monster = factory.Create(host.transform, null);
            monster.transform.position = new Vector3(0.4f, 0f, 0f);
            monster.Spawn(null, 1000, 1, false, 0, player);
            Set(battle, "wave", new List<Monster> { monster });

            Assert.IsTrue(battle.TrySkillStrike(1.0));
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(playerGo);
        }

        static void Set(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, name);
            field.SetValue(target, value);
        }
    }
}
