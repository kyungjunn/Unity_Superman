using System.IO;
using System.Collections.Generic;
using GrowNa.Battle;
using GrowNa.Core;
using GrowNa.Persistence;
using GrowNa.Gear;
using GrowNa.Monsters;
using GrowNa.Pet;
using GrowNa.Players;
using GrowNa.Skill;
using GrowNa.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace GrowNa.EditorTools
{
    public static class MainSceneBuilder
    {
        const string SpriteDir = "Assets/Art/Sprites";
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string AxelSheetDir = "Assets/Axel - Pixel Character/PNG [Sprite Sheet]";
        const string AxelPortraitDir = "Assets/Axel - Pixel Character/PNG/_portrait";
        const string AnimatedGearDir = "Assets/Art/AnimatedGear";
        const string PlainsDemoPath = "Assets/2D Pixel Art Platformer Biome - Plains/Demo.unity";
        const string PlainsPrefabPath = "Assets/2D Pixel Art Platformer Biome - Plains/Tilemap/Tilemap.prefab";
        const float WalkY = 0f;
        const float AxelScale = 8f;
        const float PlayerScale = 0.7f;

        static readonly Color Cream = new Color(0.96f, 0.93f, 0.86f);
        static readonly Color Gold = new Color(0.96f, 0.80f, 0.30f);

        [MenuItem("GrowNa/Build Main Scene")]
        public static void Build()
        {
            var scene = Construct(NewSceneMode.Single);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GrowNa] Main scene built: {ScenePath}");
        }

        public static UnityEngine.SceneManagement.Scene Construct()
            => Construct(NewSceneMode.Single);

        public static UnityEngine.SceneManagement.Scene Construct(NewSceneMode mode)
        {
            EnsureSpriteImportSettings();
            PlayerSettings.runInBackground = true;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            BuildCamera();
            var systems = new GameObject("Systems");
            var wallet = systems.AddComponent<Wallet>();
            var stats = systems.AddComponent<PlayerStats>();
            var loadout = systems.AddComponent<Loadout>();
            var inventory = systems.AddComponent<Inventory>();
            var enhance = systems.AddComponent<EnhanceService>();
            var autoOffer = systems.AddComponent<AutoOfferService>();
            var upgrades = systems.AddComponent<UpgradeService>();
            var lamp = systems.AddComponent<LampService>();
            var skills = systems.AddComponent<SkillService>();
            var pets = systems.AddComponent<PetService>();
            var quests = systems.AddComponent<QuestService>();
            var chest = systems.AddComponent<IdleChestService>();
            var save = systems.AddComponent<GrowNa.Persistence.SaveService>();
            var factory = systems.AddComponent<MonsterFactory>();
            var fonts = systems.AddComponent<UiFont>();
            var popups = systems.AddComponent<DamagePopupSpawner>();
            var presenter = systems.AddComponent<BattlePresenter>();
            var bootstrap = systems.AddComponent<GrowNa.Composition.SceneRuntimeBootstrap>();

            var world = new GameObject("World").transform;
            BuildBackground(world);
            Transform player = BuildPlayer(world);
            BuildPet(world, player);
            var monsterRoot = new GameObject("Monsters").transform;
            monsterRoot.SetParent(world, false);

            var battle = systems.AddComponent<BattleManager>();
            WireBattle(battle, player, monsterRoot);
            WirePresenter(presenter, player);
            WireBootstrap(bootstrap, wallet, stats, upgrades, quests, loadout, inventory, enhance, lamp, autoOffer, skills, pets, chest, battle, save, factory, presenter, popups, fonts);

            var hud = UiBuilder.Build(out var toastRoot);
            hud.transform.SetParent(null);
            return scene;
        }

        static void EnsureSpriteImportSettings()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteDir, AnimatedGearDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                bool isAnimatedGear = path.StartsWith(AnimatedGearDir, System.StringComparison.Ordinal);
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                float pixelsPerUnit = isAnimatedGear ? 100f : 256f;
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
                {
                    importer.spritePixelsPerUnit = pixelsPerUnit;
                    dirty = true;
                }
                FilterMode filter = isAnimatedGear ? FilterMode.Point : FilterMode.Bilinear;
                if (importer.filterMode != filter)
                {
                    importer.filterMode = filter;
                    dirty = true;
                }

                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                bool isRigLayer = isAnimatedGear || file.StartsWith("gear_") || file.StartsWith("char_");

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

        public static Sprite LoadAxelPortrait(string file = "Stone_Face.png")
            => AssetDatabase.LoadAssetAtPath<Sprite>($"{AxelPortraitDir}/{file}");

        static void BuildCamera()
        {
            var go = new GameObject("Main Camera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = new Color(0.45f, 0.72f, 0.92f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
        }

        static void BuildBackground(Transform parent)
        {
            var a = new GameObject("PlainsA").transform;
            a.SetParent(parent, false);
            if (!ImportDemoWorld(a))
                InstantiatePlainsPrefab(a);
            FlattenGround(a);

            float width = MeasureWidth(a);
            var bGo = Object.Instantiate(a.gameObject, parent);
            bGo.name = "PlainsB";
            bGo.transform.position = a.position + Vector3.right * width;

            parent.gameObject.AddComponent<MapScroller>().Bind(new[] { a, bGo.transform }, width);
        }

        static bool ImportDemoWorld(Transform dest)
        {
            var main = EditorSceneManager.GetActiveScene();
            var demo = EditorSceneManager.OpenScene(PlainsDemoPath, OpenSceneMode.Additive);
            if (!demo.IsValid()) return false;
            EditorSceneManager.SetActiveScene(main);
            var skip = new HashSet<string> { "Main Camera", "Scarecrow", "Pointer", "Spikes" };
            EditorSceneManager.SetActiveScene(demo);
            foreach (var root in demo.GetRootGameObjects())
            {
                if (skip.Contains(root.name)) continue;
                var clone = Object.Instantiate(root);
                clone.name = root.name;
                EditorSceneManager.MoveGameObjectToScene(clone, main);
                clone.transform.SetParent(dest, true);
            }
            EditorSceneManager.SetActiveScene(main);
            EditorSceneManager.CloseScene(demo, true);
            return dest.childCount > 0;
        }

        static void InstantiatePlainsPrefab(Transform dest)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlainsPrefabPath);
            if (prefab == null) return;
            var plains = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            plains.name = "Plains";
            plains.transform.SetParent(dest, false);
        }

        static void FlattenGround(Transform plains)
        {
            Tilemap ground = null;
            foreach (var tilemap in plains.GetComponentsInChildren<Tilemap>())
            {
                if (tilemap.gameObject.name == "Ground")
                {
                    ground = tilemap;
                    break;
                }
            }
            if (ground == null) return;

            ground.CompressBounds();
            var bounds = ground.cellBounds;
            var colMax = new Dictionary<int, int>();
            TileBase topTile = null;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                int maxY = int.MinValue;
                TileBase found = null;
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var tile = ground.GetTile(new Vector3Int(x, y, 0));
                    if (tile == null) continue;
                    if (y >= maxY)
                    {
                        maxY = y;
                        found = tile;
                    }
                }
                if (found == null) continue;
                colMax[x] = maxY;
                topTile = found;
            }
            if (colMax.Count == 0 || topTile == null) return;

            int surfaceY = MostCommon(colMax.Values);
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = surfaceY + 1; y < bounds.yMax; y++)
                    ground.SetTile(new Vector3Int(x, y, 0), null);
                if (ground.GetTile(new Vector3Int(x, surfaceY, 0)) == null)
                    ground.SetTile(new Vector3Int(x, surfaceY, 0), topTile);
            }

            float top = ground.CellToWorld(new Vector3Int(0, surfaceY + 1, 0)).y;
            plains.position += new Vector3(0f, WalkY - top, 0f);
            Debug.Log($"[GrowNa] flat ground cellY={surfaceY} worldTop={top:0.##} -> WalkY={WalkY}");
        }

        static int MostCommon(Dictionary<int, int>.ValueCollection values)
        {
            var counts = new Dictionary<int, int>();
            int best = 0, bestN = -1;
            foreach (int v in values)
            {
                counts.TryGetValue(v, out int n);
                n++;
                counts[v] = n;
                if (n <= bestN) continue;
                best = v;
                bestN = n;
            }
            return best;
        }

        static float FootOffset(Sprite sprite)
        {
            if (sprite == null) return 1.6f;
            return sprite.pivot.y / sprite.pixelsPerUnit * AxelScale;
        }

        static float MeasureWidth(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0) return 40f;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return Mathf.Max(24f, bounds.size.x);
        }

        static Transform BuildPlayer(Transform parent)
        {
            var root = new GameObject("Player").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(-2.1f, WalkY, 0f);
            root.localScale = Vector3.one * PlayerScale;

            var art = new GameObject("Art").AddComponent<SpriteRenderer>();
            art.transform.SetParent(root, false);
            art.sortingOrder = 10;
            Sprite[] idle = LoadAxelSheet("Idle.png");
            Sprite[] run = LoadAxelSheet("Run.png");
            Sprite[] punch = LoadAxelSheet("Right_Punch.png");
            if (idle.Length > 0) art.sprite = idle[0];
            else Debug.LogError("[GrowNa] Axel Idle 시트를 못 찾았다.");
            art.transform.localScale = Vector3.one * AxelScale;
            art.transform.localPosition = new Vector3(0f, FootOffset(art.sprite), 0f);
            var animation = art.gameObject.AddComponent<PlayerAnimation>();
            animation.Bind(art, idle, run, punch);
            var rig = BuildGearLayers(art);
            animation.BindGearRig(rig);
            BuildPlayerHpBar(root);
            return root;
        }

        static Sprite[] LoadAxelSheet(string file)
        {
            var loaded = AssetDatabase.LoadAllAssetsAtPath($"{AxelSheetDir}/{file}");
            var frames = new List<Sprite>();
            foreach (var obj in loaded)
                if (obj is Sprite sprite) frames.Add(sprite);
            frames.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return frames.ToArray();
        }

        static void BuildPlayerHpBar(Transform playerRoot)
        {
            var barRoot = new GameObject("HpBar").transform;
            barRoot.SetParent(playerRoot, false);
            barRoot.localPosition = new Vector3(0f, 3.3f, 0f);

            var back = new GameObject("Back").AddComponent<SpriteRenderer>();
            back.transform.SetParent(barRoot, false);
            back.sprite = Load("bar_bg.png");
            back.color = new Color(0.1f, 0.09f, 0.08f, 0.85f);
            back.sortingOrder = 20;
            back.drawMode = SpriteDrawMode.Sliced;
            back.size = new Vector2(1.5f, 0.2f);

            var fillPivot = new GameObject("FillPivot").transform;
            fillPivot.SetParent(barRoot, false);
            fillPivot.localPosition = new Vector3(-0.75f, 0f, 0f);

            var fill = new GameObject("Fill").AddComponent<SpriteRenderer>();
            fill.transform.SetParent(fillPivot, false);
            fill.sprite = Load("bar_hp.png");
            fill.sortingOrder = 21;
            fill.drawMode = SpriteDrawMode.Sliced;
            fill.size = new Vector2(1.44f, 0.15f);
            fill.transform.localPosition = new Vector3(0.72f, 0f, 0f);

            barRoot.gameObject.AddComponent<PlayerHpBar>().Bind(fillPivot, fill);
        }

        static CharacterRig BuildGearLayers(SpriteRenderer body)
        {
            var layers = new SpriteRenderer[GearTable.SlotCount];
            var sprites = new Sprite[GearTable.SlotCount];
            var animations = new CharacterRig.AnimatedSlot[GearTable.SlotCount];
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
                CharacterRig.FitToBody(layer, body);
                layers[i] = layer;
                animations[i] = new CharacterRig.AnimatedSlot
                {
                    idle = LoadAnimatedGear(slot, "Idle"),
                    run = LoadAnimatedGear(slot, "Run"),
                    attack = LoadAnimatedGear(slot, "Attack"),
                };
            }

            var rig = body.gameObject.AddComponent<CharacterRig>();
            rig.Bind(body, layers, sprites, animations);
            return rig;
        }

        static Sprite[] LoadAnimatedGear(GearSlot slot, string clip)
        {
            string folder = $"{AnimatedGearDir}/{slot}/{clip}";
            var frames = new List<Sprite>();
            if (!AssetDatabase.IsValidFolder(folder)) return frames.ToArray();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                if (sprite != null) frames.Add(sprite);
            }
            frames.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return frames.ToArray();
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
            pet.gameObject.AddComponent<PetView>().Bind(pet);
        }

        static void Assign(SerializedObject so, string name, UnityEngine.Object value)
        {
            var prop = so.FindProperty(name);
            if (prop == null)
                throw new System.InvalidOperationException($"missing serialized field '{name}'");
            prop.objectReferenceValue = value;
        }

        static void WireBootstrap(
            GrowNa.Composition.SceneRuntimeBootstrap bootstrap,
            Wallet wallet, PlayerStats stats, UpgradeService upgrades, QuestService quests,
            Loadout loadout, Inventory inventory, EnhanceService enhance, LampService lamp,
            AutoOfferService autoOffer, SkillService skills, PetService pets,
            IdleChestService chest, BattleManager battle, GrowNa.Persistence.SaveService save,
            MonsterFactory factory, BattlePresenter presenter, DamagePopupSpawner popups, UiFont fonts)
        {
            var so = new SerializedObject(bootstrap);
            Assign(so, "wallet", wallet);
            Assign(so, "stats", stats);
            Assign(so, "upgrades", upgrades);
            Assign(so, "quests", quests);
            Assign(so, "loadout", loadout);
            Assign(so, "inventory", inventory);
            Assign(so, "enhance", enhance);
            Assign(so, "lamp", lamp);
            Assign(so, "autoOffer", autoOffer);
            Assign(so, "skills", skills);
            Assign(so, "pets", pets);
            Assign(so, "chest", chest);
            Assign(so, "battle", battle);
            Assign(so, "save", save);
            Assign(so, "factory", factory);
            Assign(so, "presenter", presenter);
            Assign(so, "popups", popups);
            Assign(so, "fonts", fonts);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WirePresenter(BattlePresenter presenter, Transform player)
        {
            var so = new SerializedObject(presenter);
            Assign(so, "mapScroller", player.parent.GetComponent<MapScroller>());
            Assign(so, "playerAnimation", player.GetComponentInChildren<PlayerAnimation>());
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireBattle(BattleManager battle, Transform player, Transform monsterRoot)
        {
            var so = new SerializedObject(battle);
            Assign(so, "playerTransform", player);
            Assign(so, "monsterRoot", monsterRoot);
            Assign(so, "hpBarSprite", Load("bar_hp.png"));
            var spawnX = so.FindProperty("spawnX");
            if (spawnX == null) throw new System.InvalidOperationException("missing serialized field 'spawnX'");
            spawnX.floatValue = 6.4f;
            var groundY = so.FindProperty("groundY");
            if (groundY == null) throw new System.InvalidOperationException("missing serialized field 'groundY'");
            groundY.floatValue = WalkY;

            var sprites = so.FindProperty("monsterSprites");
            if (sprites == null) throw new System.InvalidOperationException("missing serialized field 'monsterSprites'");
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
