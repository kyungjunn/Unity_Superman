using System.IO;
using System.Linq;
using GrowNa.Composition;
using GrowNa.EditorTools;
using GrowNa.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrowNa.Tests
{
    public class SceneBuilderWiringTests
    {
        const string HostPath = "Assets/Scenes/_gjc_wiring_host.unity";

        [Test]
        public void Construct_serializes_exactly_one_bootstrap_with_required_refs()
        {
            var current = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(current.path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HostPath));
                EditorSceneManager.SaveScene(current, HostPath);
            }

            var scene = MainSceneBuilder.Construct(NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var boots = Object.FindObjectsByType<SceneRuntimeBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(b => b.gameObject.scene == scene)
                    .ToArray();
                Assert.AreEqual(1, boots.Length);
                var so = new SerializedObject(boots[0]);
                foreach (var field in new[]
                {
                    "wallet", "stats", "upgrades", "quests", "loadout", "inventory",
                    "enhance", "lamp", "autoOffer", "skills", "pets", "chest",
                    "battle", "save", "factory", "presenter", "popups", "fonts"
                })
                {
                    var prop = so.FindProperty(field);
                    Assert.IsNotNull(prop, field);
                    Assert.IsNotNull(prop.objectReferenceValue, field);
                }

                var ui = scene.GetRootGameObjects().Single(go => go.name == "UI").transform;
                var hudLayer = ui.Find("HudLayer");
                var popupLayer = ui.Find("PopupLayer");
                Assert.IsNotNull(hudLayer);
                Assert.IsNotNull(popupLayer);
                Assert.Greater(popupLayer.GetSiblingIndex(), hudLayer.GetSiblingIndex());
                Assert.IsNotNull(hudLayer.Find("IdleChestButton"));
                Assert.IsNotNull(hudLayer.Find("QuestButton"));
                Assert.IsNotNull(popupLayer.Find("IdleChestPanel"));
                Assert.IsNotNull(popupLayer.Find("QuestPanel"));

                var result = popupLayer.Find("OfferResultPanel");
                var fresh = result.Find("NewCard").GetComponent<RectTransform>();
                var currentCard = result.Find("CurrentCard").GetComponent<RectTransform>();
                Assert.Greater(fresh.anchoredPosition.y, currentCard.anchoredPosition.y);

                var rig = Object.FindObjectsByType<CharacterRig>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(item => item.gameObject.scene == scene);
                var animated = new SerializedObject(rig).FindProperty("animatedSlots");
                foreach (int slot in new[] { 0, 1, 2, 4 })
                {
                    var entry = animated.GetArrayElementAtIndex(slot);
                    Assert.Greater(entry.FindPropertyRelative("idle").arraySize, 0, $"slot {slot} idle");
                    Assert.Greater(entry.FindPropertyRelative("run").arraySize, 0, $"slot {slot} run");
                    Assert.Greater(entry.FindPropertyRelative("attack").arraySize, 0, $"slot {slot} attack");
                }
            }
            finally
            {
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
                if (File.Exists(HostPath))
                {
                    AssetDatabase.DeleteAsset(HostPath);
                    if (File.Exists(HostPath + ".meta")) AssetDatabase.DeleteAsset(HostPath + ".meta");
                }
            }
        }
    }
}
