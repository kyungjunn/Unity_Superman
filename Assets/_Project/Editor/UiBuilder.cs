using GrowNa.Gear;
using GrowNa.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GrowNa.EditorTools
{
    public static class UiBuilder
    {
        static readonly Color Cream = new Color(0.96f, 0.93f, 0.86f);
        static readonly Color Gold = new Color(0.98f, 0.83f, 0.36f);
        static readonly Color Grain = new Color(0.93f, 0.80f, 0.44f);
        static readonly Color PanelTint = new Color(1f, 1f, 1f, 1f);

        public static GameObject Build(out GameObject toastRoot)
        {
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeightConst(0.5f);

            // 프로젝트가 Input System 패키지로 전환돼 있어서 StandaloneInputModule 은 매 프레임 예외를 던진다.
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            Transform root = canvasGo.transform;
            var binder = canvasGo.AddComponent<HudBinder>();

            BuildPlayerPanel(root, out Text nameLabel, out Text levelLabel, out Text hpLabel,
                             out Text powerLabel, out Image hpFill);
            BuildCurrencyBar(root, out Text goldLabel, out Text grainLabel, out Text wickLabel);
            BuildStageBanner(root, out Text stageLabel, out Text killLabel, out Text bossLabel);
            BuildEventButtons(root);
            toastRoot = BuildToast(root, out Text toastLabel);
            BuildInventory(root, out Image[] slotFrames, out Text[] slotLabels);
            Button lampButton = BuildLampRow(root);

            var upgradePanel = PanelBuilder.BuildUpgradePanel(root);
            var lampPanel = PanelBuilder.BuildLampPanel(root);
            lampButton.onClick.AddListener(lampPanel.Toggle);

            BuildNavBar(root, toastRoot, toastLabel, upgradePanel);

            canvasGo.AddComponent<LoadoutView>().Bind(slotFrames, slotLabels);
            canvasGo.AddComponent<OfflineNotice>().Bind(toastRoot, toastLabel);

            binder.Bind(nameLabel, levelLabel, hpLabel, powerLabel, goldLabel, grainLabel,
                        wickLabel, stageLabel, killLabel, bossLabel, hpFill);
            return canvasGo;
        }

        static void matchWidthOrHeightConst(this CanvasScaler scaler, float value)
        {
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = value;
        }

        static void BuildPlayerPanel(Transform root, out Text nameLabel, out Text levelLabel,
                                     out Text hpLabel, out Text powerLabel, out Image hpFill)
        {
            var panel = UiFactory.Sprite("PlayerPanel", root, MainSceneBuilder.Load("ui_frame_dark.png"), PanelTint);
            UiFactory.Anchor(panel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(620f, 250f));

            var portrait = UiFactory.Sprite("Portrait", panel.transform, MainSceneBuilder.Load("ui_portrait.png"), Color.white);
            UiFactory.Anchor(portrait.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(150f, 150f));

            levelLabel = UiFactory.Label("Level", panel.transform, "Lv.1", 30, Gold, TextAnchor.MiddleCenter);
            UiFactory.Anchor(levelLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -172f), new Vector2(150f, 44f));

            nameLabel = UiFactory.Label("Name", panel.transform, "허수아비", 36, Cream);
            UiFactory.Anchor(nameLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(184f, -16f), new Vector2(400f, 50f));
            UiFactory.Shadow(nameLabel.gameObject);

            var hpBg = UiFactory.Sprite("HpBg", panel.transform, MainSceneBuilder.Load("bar_bg.png"), Color.white);
            UiFactory.Anchor(hpBg.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(184f, -74f), new Vector2(410f, 46f));

            hpFill = UiFactory.Sprite("HpFill", hpBg.transform, MainSceneBuilder.Load("bar_hp.png"), Color.white);
            UiFactory.Place(hpFill.gameObject, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillOrigin = 0;
            hpFill.fillAmount = 1f;

            hpLabel = UiFactory.Label("HpText", hpBg.transform, "240 / 240", 24, Cream, TextAnchor.MiddleCenter);
            UiFactory.Place(hpLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var powerIcon = UiFactory.Sprite("PowerIcon", panel.transform, MainSceneBuilder.Load("ic_gem.png"), Color.white);
            UiFactory.Anchor(powerIcon.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(184f, -132f), new Vector2(44f, 44f));

            powerLabel = UiFactory.Label("Power", panel.transform, "0", 32, Gold);
            UiFactory.Anchor(powerLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(238f, -132f), new Vector2(340f, 44f));
            UiFactory.Shadow(powerLabel.gameObject);

            var powerCaption = UiFactory.Label("PowerCaption", panel.transform, "전투력", 22, new Color(0.72f, 0.68f, 0.6f));
            UiFactory.Anchor(powerCaption.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(184f, -186f), new Vector2(200f, 34f));
        }

        static void BuildCurrencyBar(Transform root, out Text gold, out Text grain, out Text wick)
        {
            gold = CurrencyChip(root, "Gold", "ic_gold.png", 0);
            grain = CurrencyChip(root, "Grain", "ic_grain.png", 1);
            wick = CurrencyChip(root, "Wick", "ic_wick.png", 2);
        }

        static Text CurrencyChip(Transform root, string name, string icon, int index)
        {
            var chip = UiFactory.Sprite($"Chip_{name}", root, MainSceneBuilder.Load("ui_frame_dark.png"), PanelTint);
            UiFactory.Anchor(chip.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(18f + index * 210f, -282f), new Vector2(200f, 64f));

            var iconImage = UiFactory.Sprite("Icon", chip.transform, MainSceneBuilder.Load(icon), Color.white);
            UiFactory.Anchor(iconImage.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(52f, 52f));

            var label = UiFactory.Label("Value", chip.transform, "0", 28, Cream, TextAnchor.MiddleRight);
            UiFactory.Place(label.gameObject, Vector2.zero, Vector2.one, new Vector2(64f, 0f), new Vector2(-14f, 0f));
            return label;
        }

        static void BuildStageBanner(Transform root, out Text stage, out Text kills, out Text boss)
        {
            var banner = UiFactory.Sprite("StageBanner", root, MainSceneBuilder.Load("ui_frame_dark.png"), PanelTint);
            UiFactory.Anchor(banner.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -370f), new Vector2(460f, 120f));

            stage = UiFactory.Label("Stage", banner.transform, "밭두렁 1-1", 38, Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(stage.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(440f, 52f));
            UiFactory.Shadow(stage.gameObject);

            kills = UiFactory.Label("Kills", banner.transform, "0 / 10", 28, Grain, TextAnchor.MiddleCenter);
            UiFactory.Anchor(kills.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(440f, 44f));

            boss = UiFactory.Label("BossTimer", root, "보스 30.0초", 34, new Color(1f, 0.5f, 0.4f), TextAnchor.MiddleCenter);
            UiFactory.Anchor(boss.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -500f), new Vector2(460f, 52f));
            UiFactory.Shadow(boss.gameObject);
            boss.gameObject.SetActive(false);
        }

        static void BuildEventButtons(Transform root)
        {
            string[] names = { "출석", "공지", "이벤트", "우편" };
            for (int i = 0; i < names.Length; i++)
            {
                var button = UiFactory.Panel($"Event_{names[i]}", root, MainSceneBuilder.Load("ui_btn_round.png"), Color.white, true);
                UiFactory.Anchor(button, new Vector2(1f, 1f), new Vector2(1f, 1f),
                                 new Vector2(-18f, -18f - i * 158f), new Vector2(146f, 146f));
                var label = UiFactory.Label("Label", button.transform, names[i], 26, new Color(0.28f, 0.20f, 0.10f), TextAnchor.MiddleCenter);
                UiFactory.Place(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
        }

        static void BuildInventory(Transform root, out Image[] slotFrames, out Text[] slotLabels)
        {
            slotFrames = new Image[GearTable.SlotCount];
            slotLabels = new Text[GearTable.SlotCount];
            var panel = UiFactory.Sprite("InventoryPanel", root, MainSceneBuilder.Load("ui_frame_dark.png"), PanelTint);
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 452f), new Vector2(880f, 348f));

            const int columns = 5;
            const int rows = 2;
            const float cell = 146f;
            const float gap = 14f;
            float startX = -((columns - 1) * (cell + gap)) * 0.5f;
            float startY = ((rows - 1) * (cell + gap)) * 0.5f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    int index = r * columns + c;
                    bool locked = index >= 8;
                    var go = UiFactory.Panel($"Slot_{index}", panel.transform,
                        MainSceneBuilder.Load(locked ? "ui_slot_locked.png" : "ui_slot.png"), Color.white, !locked);
                    UiFactory.Anchor(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                     new Vector2(startX + c * (cell + gap), startY - r * (cell + gap)),
                                     new Vector2(cell, cell));
                    if (locked) continue;
                    var slot = (GearSlot)index;
                    var label = UiFactory.Label("Slot", go.transform, GearTable.Name(slot), 20,
                                                new Color(0.62f, 0.58f, 0.70f), TextAnchor.MiddleCenter);
                    UiFactory.Place(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    slotFrames[index] = go.GetComponent<Image>();
                    slotLabels[index] = label;
                }
            }
        }

        static Button BuildLampRow(Transform root)
        {
            var lamp = UiFactory.Panel("LampButton", root, MainSceneBuilder.Load("ui_lamp_big.png"), Color.white, true);
            UiFactory.Anchor(lamp, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 196f), new Vector2(216f, 216f));

            var caption = UiFactory.Label("LampCaption", lamp.transform, "봉헌", 30, new Color(0.30f, 0.20f, 0.08f), TextAnchor.MiddleCenter);
            UiFactory.Anchor(caption.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(160f, 38f));

            string[] side = { "속성", "자동", "전직" };
            float[] xs = { -330f, 330f, 330f };
            float[] ys = { 232f, 288f, 176f };
            for (int i = 0; i < side.Length; i++)
            {
                var button = UiFactory.Panel($"Side_{side[i]}", root, MainSceneBuilder.Load("ui_frame_light.png"), Color.white, true);
                UiFactory.Anchor(button, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(xs[i], ys[i]), new Vector2(140f, 84f));
                var label = UiFactory.Label("Label", button.transform, side[i], 28, new Color(0.25f, 0.18f, 0.08f), TextAnchor.MiddleCenter);
                UiFactory.Place(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            return lamp.GetComponent<Button>();
        }

        static void BuildNavBar(Transform root, GameObject toastRoot, Text toastLabel, UpgradePanel upgradePanel)
        {
            var bar = UiFactory.Sprite("NavBar", root, MainSceneBuilder.Load("ui_frame_nav.png"), PanelTint);
            UiFactory.Place(bar.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 190f));

            (string label, string icon)[] entries =
            {
                ("캐릭터", "nav_char.png"),
                ("펫", "nav_pet.png"),
                ("던전", "nav_dungeon.png"),
                ("상점", "nav_shop.png"),
                ("길드", "nav_guild.png"),
                ("정원", "nav_garden.png"),
            };

            float width = 1080f / entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var slot = UiFactory.Node($"Nav_{entries[i].label}", bar.transform);
                var image = slot.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
                var button = slot.AddComponent<Button>();
                button.targetGraphic = image;
                UiFactory.Anchor(slot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                 new Vector2(width * (i + 0.5f), 0f), new Vector2(width - 6f, 176f));

                var icon = UiFactory.Sprite("Icon", slot.transform, MainSceneBuilder.Load(entries[i].icon), Color.white);
                UiFactory.Anchor(icon.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(96f, 96f));

                var label = UiFactory.Label("Label", slot.transform, entries[i].label, 26, Cream, TextAnchor.MiddleCenter);
                UiFactory.Anchor(label.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(width, 40f));

                if (entries[i].label == "캐릭터")
                {
                    button.onClick.AddListener(upgradePanel.Toggle);
                    continue;
                }

                var stub = slot.AddComponent<MenuStub>();
                stub.Setup(entries[i].label, toastRoot, toastLabel);
                button.onClick.AddListener(stub.Show);
            }
        }

        static GameObject BuildToast(Transform root, out Text label)
        {
            var toast = UiFactory.Sprite("Toast", root, MainSceneBuilder.Load("ui_frame_dark.png"), PanelTint);
            UiFactory.Anchor(toast.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 130f));
            label = UiFactory.Label("Label", toast.transform, "", 34, Cream, TextAnchor.MiddleCenter);
            UiFactory.Place(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            toast.gameObject.SetActive(false);
            return toast.gameObject;
        }
    }
}
