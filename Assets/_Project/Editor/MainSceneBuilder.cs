using System.IO;
using GrowNa.Battle;
using GrowNa.Core;
using GrowNa.Gear;
using GrowNa.UI;
using GrowNa.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GrowNa.EditorTools
{
    public static class MainSceneBuilder
    {
        const string SpriteDir = "Assets/_Project/Art/Sprites";
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        static readonly Color Cream = new Color(0.96f, 0.93f, 0.86f);
        static readonly Color Gold = new Color(0.96f, 0.80f, 0.30f);

        [MenuItem("GrowNa/Build Main Scene")]
        public static void Build()
        {
            EnsureSpriteImportSettings();
            PlayerSettings.runInBackground = true;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildCamera();
            var systems = new GameObject("Systems");
            var wallet = systems.AddComponent<Wallet>();
            var stats = systems.AddComponent<PlayerStats>();
            systems.AddComponent<Loadout>();
            systems.AddComponent<UpgradeService>();
            systems.AddComponent<LampService>();
            systems.AddComponent<SaveService>();

            var world = new GameObject("World").transform;
            BuildBackground(world);
            Transform player = BuildPlayer(world);
            BuildPet(world, player);
            var monsterRoot = new GameObject("Monsters").transform;
            monsterRoot.SetParent(world, false);

            var battle = systems.AddComponent<BattleManager>();
            WireBattle(battle, player, monsterRoot);

            var hud = UiBuilder.Build(out var toastRoot);
            hud.transform.SetParent(null);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GrowNa] Main scene built: {ScenePath} (wallet={wallet != null}, stats={stats != null}, toast={toastRoot != null})");
        }

        static void EnsureSpriteImportSettings()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, 256f))
                {
                    importer.spritePixelsPerUnit = 256f;
                    dirty = true;
                }
                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    dirty = true;
                }

                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                bool isRigLayer = file.StartsWith("gear_") || file.StartsWith("char_");

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                // 리그 레이어는 Multiple(자동 슬라이스)이면 알파 경계로 잘려서 슬롯마다 rect/pivot 이 달라진다.
                // Single + FullRect + Center 로 고정해야 512x512 캔버스가 그대로 유지되어 레이어가 겹친다.
                if (isRigLayer && (importer.spriteImportMode != SpriteImportMode.Single
                                   || settings.spriteMeshType != SpriteMeshType.FullRect
                                   || settings.spriteAlignment != (int)SpriteAlignment.Center))
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    settings.spriteMode = (int)SpriteImportMode.Single;
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    importer.SetTextureSettings(settings);
                    dirty = true;
                }

                if (dirty) importer.SaveAndReimport();
            }
        }

        public static Sprite Load(string file)
            => AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/{file}");

        static void BuildCamera()
        {
            var go = new GameObject("Main Camera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.backgroundColor = new Color(0.53f, 0.74f, 0.88f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0.6f, -10f);
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
        }

        static void BuildBackground(Transform parent)
        {
            var bg = new GameObject("Background").AddComponent<SpriteRenderer>();
            bg.transform.SetParent(parent, false);
            bg.sprite = Load("bg_field.png");
            bg.sortingOrder = -20;
            bg.transform.position = new Vector3(0f, 2.1f, 0f);
            bg.transform.localScale = Vector3.one * 2.9f;

            var ground = new GameObject("Ground").AddComponent<SpriteRenderer>();
            ground.transform.SetParent(parent, false);
            ground.sprite = Load("ground_strip.png");
            ground.sortingOrder = -10;
            ground.transform.position = new Vector3(0f, -0.95f, 0f);
            ground.transform.localScale = new Vector3(3.2f, 1.6f, 1f);
        }

        static Transform BuildPlayer(Transform parent)
        {
            var root = new GameObject("Player").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(-2.1f, -0.25f, 0f);

            var art = new GameObject("Art").AddComponent<SpriteRenderer>();
            art.transform.SetParent(root, false);
            art.sprite = Load("char_scarecrow_base.png");
            art.sortingOrder = 10;
            art.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            art.transform.localScale = Vector3.one * 1.35f;
            art.gameObject.AddComponent<IdleBob>();
            BuildGearLayers(art);
            return root;
        }

        static void BuildGearLayers(SpriteRenderer body)
        {
            var layers = new SpriteRenderer[GearTable.SlotCount];
            var sprites = new Sprite[GearTable.SlotCount];
            string[] files = { "gear_weapon.png", "gear_helmet.png", "gear_armor.png", "gear_gloves.png",
                               "gear_boots.png", "gear_amulet.png", "gear_ring.png", "gear_charm.png" };

            for (int i = 0; i < GearTable.SlotCount; i++)
            {
                var slot = (GearSlot)i;
                var layer = new GameObject($"Layer_{slot}").AddComponent<SpriteRenderer>();
                layer.transform.SetParent(body.transform, false);
                layer.sortingOrder = CharacterRig.SortingOrderFor(slot);
                layer.enabled = false;
                sprites[i] = Load(files[i]);
                layer.sprite = sprites[i];
                layers[i] = layer;
            }

            body.gameObject.AddComponent<CharacterRig>().Bind(body, layers, sprites);
        }

        static void BuildPet(Transform parent, Transform player)
        {
            var pet = new GameObject("Pet_Sparrow").AddComponent<SpriteRenderer>();
            pet.transform.SetParent(parent, false);
            pet.sprite = Load("pet_sparrow.png");
            pet.sortingOrder = 11;
            pet.transform.localScale = Vector3.one * 0.85f;
            var orbit = pet.gameObject.AddComponent<PetOrbit>();
            orbit.Bind(player);
        }

        static void WireBattle(BattleManager battle, Transform player, Transform monsterRoot)
        {
            var so = new SerializedObject(battle);
            so.FindProperty("playerTransform").objectReferenceValue = player;
            so.FindProperty("monsterRoot").objectReferenceValue = monsterRoot;
            so.FindProperty("hpBarSprite").objectReferenceValue = Load("bar_hp.png");
            so.FindProperty("groundY").floatValue = -0.25f;

            var sprites = so.FindProperty("monsterSprites");
            string[] mobs = { "mob_fieldmouse.png", "mob_locust.png", "mob_crow.png" };
            sprites.arraySize = mobs.Length;
            for (int i = 0; i < mobs.Length; i++)
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = Load(mobs[i]);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
                if (s.path == path) return;
            var list = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(list, 0);
            list[scenes.Length] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = list;
        }
    }
}
