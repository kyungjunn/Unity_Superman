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
        static readonly Color ExpGreen = new Color(0.62f, 0.89f, 0.53f);
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
            Transform hudRoot = UiFactory.Node("HudLayer", root).transform;
            UiFactory.Place(hudRoot.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Transform popupRoot = UiFactory.Node("PopupLayer", root).transform;
            UiFactory.Place(popupRoot.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            BuildPlayerPanel(hudRoot, out Text nameLabel, out Text levelLabel,
                             out Text powerLabel, out Image expFill, out Text expLabel);
            BuildCurrencyBar(hudRoot, out Text goldLabel, out Text grainLabel, out Text wickLabel, out Text gemLabel);
            BuildStageBanner(hudRoot, out Text stageLabel, out Text killLabel, out Text bossLabel);
            BuildEventButtons(hudRoot);
            toastRoot = BuildToast(popupRoot, out Text toastLabel);
            BuildInventory(hudRoot, out Image[] slotFrames, out Text[] slotLabels, out Button[] slotButtons);
            Button lampButton = BuildLampRow(hudRoot, out Button statsButton, out Button autoButton, out Button jobButton);

            Button characterNavButton = BuildNavBar(hudRoot, toastRoot, toastLabel, out Button petNavButton);

            PanelBuilder.BuildUpgradePanel(popupRoot, statsButton);
            PanelBuilder.BuildGearPanel(popupRoot, slotButtons);

            // 등불 버튼은 패널을 열지 않고 낟알 1개로 바로 뽑는다. 결과 패널이 그 결과를 받으므로 먼저 만든다.
            var resultPanel = OfferPanelBuilder.BuildResultPanel(popupRoot);
            canvasGo.AddComponent<QuickOfferButton>().Bind(lampButton, resultPanel, toastRoot, toastLabel);

            PanelBuilder.BuildLampPanel(popupRoot, jobButton);
            OfferPanelBuilder.BuildAutoPanel(popupRoot, autoButton);

            var skillSlots = SkillPanelBuilder.BuildSkillBar(hudRoot, out Button skillGachaButton, out Button petGachaButton);
            var gachaPanel = SkillPanelBuilder.BuildGachaPanel(popupRoot);
            canvasGo.AddComponent<SkillBarView>().Bind(skillSlots, skillGachaButton, gachaPanel);

            SkillPanelBuilder.BuildSkillEquipPanel(popupRoot, characterNavButton);
            SkillPanelBuilder.BuildPetEquipPanel(popupRoot, petNavButton);
            SkillPanelBuilder.BuildPetGachaPanel(popupRoot, petGachaButton);
            SkillPanelBuilder.BuildIdleChest(hudRoot, popupRoot);
            QuestPanelBuilder.BuildQuestPanel(hudRoot, popupRoot);
            var levelUp = SkillPanelBuilder.BuildLevelUpPopup(popupRoot);

            canvasGo.AddComponent<LoadoutView>().Bind(slotFrames, slotLabels);
            canvasGo.AddComponent<OfflineNotice>().Bind(toastRoot, toastLabel);

            // 겹침 순서는 형제 순서가 전부다. 결과 < 레벨업 < 토스트 순으로 위에 오도록 마지막에 못박는다.
            resultPanel.transform.SetAsLastSibling();
            levelUp.transform.SetAsLastSibling();
            toastRoot.transform.SetAsLastSibling();

            binder.Bind(nameLabel, levelLabel, powerLabel, goldLabel, grainLabel,
                        wickLabel, gemLabel, stageLabel, killLabel, bossLabel);
            binder.BindProgress(expFill, expLabel);
            return canvasGo;
        }

        static void matchWidthOrHeightConst(this CanvasScaler scaler, float value)
        {
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = value;
        }

        // 체력 바는 플레이어 머리 위 월드 바로 옮겼다(MainSceneBuilder.BuildPlayerHpBar).
        // 비워진 그 자리를 경험치 바가 그대로 물려받는다.
        static void BuildPlayerPanel(Transform root, out Text nameLabel, out Text levelLabel,
                                     out Text powerLabel, out Image expFill, out Text expLabel)
        {
            var panel = UiFactory.Sprite("PlayerPanel", root, MainSceneBuilder.Load("ui_frame_dark.png"), PanelTint);
            UiFactory.Anchor(panel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(620f, 200f));

            var portrait = UiFactory.Sprite("Portrait", panel.transform, MainSceneBuilder.LoadAxelPortrait(), Color.white);
            UiFactory.Anchor(portrait.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -12f), new Vector2(140f, 140f));

            levelLabel = UiFactory.Label("Level", panel.transform, "Lv.1", 28, Gold, TextAnchor.MiddleCenter);
            UiFactory.Anchor(levelLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -156f), new Vector2(140f, 36f));

            nameLabel = UiFactory.Label("Name", panel.transform, "액셀", 34, Cream);
            UiFactory.Anchor(nameLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(172f, -16f), new Vector2(196f, 48f));
            UiFactory.Shadow(nameLabel.gameObject);

            var separator = UiFactory.Label("Separator", panel.transform, "|", 34, new Color(0.55f, 0.51f, 0.44f), TextAnchor.MiddleCenter);
            UiFactory.Anchor(separator.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(372f, -16f), new Vector2(24f, 48f));

            powerLabel = UiFactory.Label("Power", panel.transform, "0", 34, Gold);
            UiFactory.Anchor(powerLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(400f, -16f), new Vector2(206f, 48f));
            UiFactory.Shadow(powerLabel.gameObject);

            var expBg = UiFactory.Sprite("ExpBg", panel.transform, MainSceneBuilder.Load("bar_bg.png"), Color.white);
            UiFactory.Anchor(expBg.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(172f, -76f), new Vector2(430f, 46f));

            expFill = UiFactory.Sprite("ExpFill", expBg.transform, MainSceneBuilder.Load("bar_exp.png"), ExpGreen);
            UiFactory.Place(expFill.gameObject, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            expFill.type = Image.Type.Filled;
            expFill.fillMethod = Image.FillMethod.Horizontal;
            expFill.fillOrigin = 0;
            expFill.fillAmount = 0f;

            expLabel = UiFactory.Label("ExpText", expBg.transform, "EXP 0 / 50", 22, new Color(0.16f, 0.22f, 0.12f), TextAnchor.MiddleCenter);
            UiFactory.Place(expLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        static void BuildCurrencyBar(Transform root, out Text gold, out Text grain, out Text wick, out Text gem)
        {
            gold = CurrencyChip(root, "Gold", "ic_gold.png", 0);
            grain = CurrencyChip(root, "Grain", "ic_grain.png", 1);
            wick = CurrencyChip(root, "Wick", "ic_wick.png", 2);
            gem = CurrencyChip(root, "Gem", "ic_gem.png", 3);
        }

        static Text CurrencyChip(Transform root, string name, string icon, int index)
        {
            var chip = UiFactory.Sprite($"Chip_{name}", root, MainSceneBuilder.Load("ui_frame_dark.png"), PanelTint);
            UiFactory.Anchor(chip.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(18f + index * 210f, -232f), new Vector2(200f, 64f));

            var iconImage = UiFactory.Sprite("Icon", chip.transform, MainSceneBuilder.Load(icon), Color.white);
            UiFactory.Anchor(iconImage.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(52f, 52f));

            var label = UiFactory.Label("Value", chip.transform, "0", 28, Cream, TextAnchor.MiddleRight);
            UiFactory.Place(label.gameObject, Vector2.zero, Vector2.one, new Vector2(64f, 0f), new Vector2(-14f, 0f));
            return label;
        }

        static void BuildStageBanner(Transform root, out Text stage, out Text kills, out Text boss)
        {
            var banner = UiFactory.Sprite("StageBanner", root, MainSceneBuilder.Load("ui_frame_dark.png"), PanelTint);
            UiFactory.Anchor(banner.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(460f, 120f));

            stage = UiFactory.Label("Stage", banner.transform, "밭두렁 1-1", 38, Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(stage.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(440f, 52f));
            UiFactory.Shadow(stage.gameObject);

            kills = UiFactory.Label("Kills", banner.transform, "0 / 10", 28, Grain, TextAnchor.MiddleCenter);
            UiFactory.Anchor(kills.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(440f, 44f));

            boss = UiFactory.Label("BossTimer", root, "보스 30.0초", 34, new Color(1f, 0.5f, 0.4f), TextAnchor.MiddleCenter);
            UiFactory.Anchor(boss.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -462f), new Vector2(460f, 52f));
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

        static void BuildInventory(Transform root, out Image[] slotFrames, out Text[] slotLabels, out Button[] slotButtons)
        {
            slotFrames = new Image[GearTable.SlotCount];
            slotLabels = new Text[GearTable.SlotCount];
            slotButtons = new Button[GearTable.SlotCount];
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
                    slotButtons[index] = go.GetComponent<Button>();
                }
            }
        }

        static Button BuildLampRow(Transform root, out Button statsButton, out Button autoButton, out Button jobButton)
        {
            statsButton = null;
            autoButton = null;
            jobButton = null;
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

                // 등불 버튼이 즉시 봉헌으로 바뀌면서 패널들의 입구를 사이드 버튼 셋이 나눠 받는다.
                // 속성 = 액셀 단련, 자동 = 자동 점등, 전직 = 등불(심지 교체).
                if (side[i] == "속성") statsButton = button.GetComponent<Button>();
                if (side[i] == "자동") autoButton = button.GetComponent<Button>();
                if (side[i] == "전직") jobButton = button.GetComponent<Button>();
            }

            return lamp.GetComponent<Button>();
        }

        static Button BuildNavBar(Transform root, GameObject toastRoot, Text toastLabel, out Button petButton)
        {
            Button characterButton = null;
            petButton = null;
            var bar = UiFactory.Sprite("NavBar", root, MainSceneBuilder.Load("ui_frame_nav.png"), PanelTint);
            UiFactory.Place(bar.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 190f));

            (string label, Sprite icon)[] entries =
            {
                ("캐릭터", MainSceneBuilder.LoadAxelPortrait()),
                ("펫", MainSceneBuilder.Load("nav_pet.png")),
                ("던전", MainSceneBuilder.Load("nav_dungeon.png")),
                ("상점", MainSceneBuilder.Load("nav_shop.png")),
                ("길드", MainSceneBuilder.Load("nav_guild.png")),
                ("정원", MainSceneBuilder.Load("nav_garden.png")),
            };

            float width = 1080f / entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var slot = UiFactory.Panel($"Nav_{entries[i].label}", bar.transform, null, new Color(1f, 1f, 1f, 0f), true);
                UiFactory.Anchor(slot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                 new Vector2(width * (i + 0.5f), 0f), new Vector2(width - 6f, 176f));
                var button = slot.GetComponent<Button>();

                var icon = UiFactory.Sprite("Icon", slot.transform, entries[i].icon, Color.white);
                UiFactory.Anchor(icon.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(96f, 96f));

                var label = UiFactory.Label("Label", slot.transform, entries[i].label, 26, Cream, TextAnchor.MiddleCenter);
                UiFactory.Anchor(label.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(width, 40f));

                if (entries[i].label == "캐릭터")
                {
                    characterButton = button;
                    continue;
                }

                if (entries[i].label == "펫")
                {
                    petButton = button;
                    continue;
                }

                // MenuStub 은 같은 GameObject 의 Button 을 런타임에 직접 잡는다.
                var stub = slot.AddComponent<MenuStub>();
                stub.Setup(entries[i].label, toastRoot, toastLabel);
            }

            return characterButton;
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
