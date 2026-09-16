using GrowNa.Core;
using GrowNa.Gear;
using GrowNa.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.EditorTools
{
    public static class PanelBuilder
    {
        internal static readonly Color Cream = new Color(0.96f, 0.93f, 0.86f);
        internal static readonly Color Gold = new Color(0.98f, 0.83f, 0.36f);
        internal static readonly Color Dim = new Color(0.72f, 0.68f, 0.60f);
        internal static readonly Color Ink = new Color(0.26f, 0.18f, 0.08f);

        public static UpgradePanel BuildUpgradePanel(Transform root, Button opener)
        {
            var panel = UiFactory.Sprite("UpgradePanel", root, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            panel.raycastTarget = true;
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(0f, -40f), new Vector2(900f, 620f));

            var title = UiFactory.Label("Title", panel.transform, "액셀 단련", 44, Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(600f, 60f));
            UiFactory.Shadow(title.gameObject);

            var component = panel.gameObject.AddComponent<UpgradePanel>();
            var rows = new UpgradePanel.Row[UpgradeTable.All.Length];
            for (int i = 0; i < UpgradeTable.All.Length; i++)
                rows[i] = BuildUpgradeRow(panel.transform, UpgradeTable.All[i], i);

            component.Bind(panel.gameObject, rows, opener, BuildCloseButton(panel.transform));
            return component;
        }

        static UpgradePanel.Row BuildUpgradeRow(Transform parent, StatKind kind, int index)
        {
            var rowGo = UiFactory.Sprite($"Row_{kind}", parent, MainSceneBuilder.Load("ui_frame_light.png"), Color.white);
            UiFactory.Anchor(rowGo.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, -150f - index * 140f), new Vector2(820f, 124f));

            var title = UiFactory.Label("Title", rowGo.transform, UpgradeTable.DisplayName(kind), 34, Ink);
            UiFactory.Anchor(title.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -14f), new Vector2(420f, 48f));

            var detail = UiFactory.Label("Detail", rowGo.transform, "", 26, new Color(0.42f, 0.34f, 0.22f));
            UiFactory.Anchor(detail.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -66f), new Vector2(420f, 40f));

            var buttonGo = UiFactory.Panel("Buy", rowGo.transform, MainSceneBuilder.Load("ui_btn_round.png"), Color.white, true);
            UiFactory.Anchor(buttonGo, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(240f, 92f));

            var icon = UiFactory.Sprite("Icon", buttonGo.transform, MainSceneBuilder.Load("ic_gold.png"), Color.white);
            UiFactory.Anchor(icon.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(48f, 48f));

            var cost = UiFactory.Label("Cost", buttonGo.transform, "0", 30, Gold, TextAnchor.MiddleRight);
            UiFactory.Place(cost.gameObject, Vector2.zero, Vector2.one, new Vector2(68f, 0f), new Vector2(-16f, 0f));

            return new UpgradePanel.Row
            {
                kind = kind,
                title = title,
                detail = detail,
                cost = cost,
                button = buttonGo.GetComponent<Button>(),
                buttonImage = buttonGo.GetComponent<Image>(),
            };
        }

        public static GearPanel BuildGearPanel(Transform root, Button[] openers)
        {
            var panel = UiFactory.Sprite("GearPanel", root, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            panel.raycastTarget = true;
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(0f, -20f), new Vector2(960f, 1420f));

            var title = UiFactory.Label("Title", panel.transform, "장비", 44, Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 58f));
            UiFactory.Shadow(title.gameObject);

            var stash = UiFactory.Label("Stash", panel.transform, "", 24, Gold, TextAnchor.MiddleCenter);
            UiFactory.Anchor(stash.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(880f, 40f));

            var rows = new GearPanel.Row[GearTable.SlotCount];
            for (int i = 0; i < GearTable.SlotCount; i++)
                rows[i] = BuildGearRow(panel.transform, (GearSlot)i, i);

            var result = UiFactory.Label("Result", panel.transform, "", 28, Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(result.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(900f, 46f));
            UiFactory.Shadow(result.gameObject);

            Button dismantle = ActionButton(panel.transform, "Dismantle", "여분 분해", new Vector2(-230f, 80f));
            Button fuse = ActionButton(panel.transform, "Fuse", "합성", new Vector2(230f, 80f));

            var component = panel.gameObject.AddComponent<GearPanel>();
            component.Bind(panel.gameObject, rows, stash, result, dismantle, fuse,
                           openers, BuildCloseButton(panel.transform));
            return component;
        }

        static GearPanel.Row BuildGearRow(Transform parent, GearSlot slot, int index)
        {
            var rowGo = UiFactory.Sprite($"Gear_{slot}", parent, MainSceneBuilder.Load("ui_frame_light.png"), Color.white);
            UiFactory.Anchor(rowGo.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, -160f - index * 128f), new Vector2(880f, 114f));

            var title = UiFactory.Label("Title", rowGo.transform, GearTable.Name(slot), 30, Ink);
            UiFactory.Anchor(title.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -10f), new Vector2(220f, 44f));

            var detail = UiFactory.Label("Detail", rowGo.transform, "", 24, new Color(0.42f, 0.34f, 0.22f));
            UiFactory.Anchor(detail.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -58f), new Vector2(520f, 40f));

            var buttonGo = UiFactory.Panel("Enhance", rowGo.transform, MainSceneBuilder.Load("ui_btn_round.png"), Color.white, true);
            UiFactory.Anchor(buttonGo, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(280f, 86f));

            var cost = UiFactory.Label("Cost", buttonGo.transform, "", 24, Gold, TextAnchor.MiddleCenter);
            UiFactory.Place(cost.gameObject, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));

            return new GearPanel.Row
            {
                slot = slot,
                title = title,
                detail = detail,
                cost = cost,
                button = buttonGo.GetComponent<Button>(),
                buttonImage = buttonGo.GetComponent<Image>(),
            };
        }

        static Button ActionButton(Transform parent, string name, string label, Vector2 position)
        {
            var go = UiFactory.Panel(name, parent, MainSceneBuilder.Load("ui_btn_round.png"), Color.white, true);
            UiFactory.Anchor(go, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, new Vector2(360f, 96f));
            var text = UiFactory.Label("Label", go.transform, label, 30, new Color(0.28f, 0.20f, 0.10f), TextAnchor.MiddleCenter);
            UiFactory.Place(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return go.GetComponent<Button>();
        }

        // 봉헌은 등불 버튼이 바로 처리하고 자동 점등은 "자동" 버튼이 연다.
        // 이 패널에 남는 조작은 심지 교체 하나뿐이다.
        public static LampPanel BuildLampPanel(Transform root, Button opener)
        {
            var panel = UiFactory.Sprite("LampPanel", root, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            panel.raycastTarget = true;
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(0f, -20f), new Vector2(940f, 1180f));

            var headline = UiFactory.Label("Headline", panel.transform, "소원의 등불 Lv.1", 46, Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(headline.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(760f, 62f));
            UiFactory.Shadow(headline.gameObject);

            var flame = UiFactory.Sprite("Flame", panel.transform, MainSceneBuilder.Load("ui_lamp_big.png"), Color.white);
            UiFactory.Anchor(flame.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(260f, 260f));

            var rates = UiFactory.Label("Rates", panel.transform, "", 28, Cream, TextAnchor.UpperLeft);
            UiFactory.Anchor(rates.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -420f), new Vector2(760f, 300f));

            var pity = UiFactory.Label("Pity", panel.transform, "", 28, Gold, TextAnchor.MiddleCenter);
            UiFactory.Anchor(pity.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -742f), new Vector2(760f, 44f));

            var result = UiFactory.Label("Result", panel.transform, "", 32, Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(result.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -800f), new Vector2(860f, 52f));
            UiFactory.Shadow(result.gameObject);

            Button upgradeButton = OfferButton(panel.transform, "Upgrade", "심지 교체", new Vector2(0f, 220f));

            var cost = UiFactory.Label("UpgradeCost", panel.transform, "", 26, Dim, TextAnchor.MiddleCenter);
            UiFactory.Anchor(cost.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(760f, 40f));

            var component = panel.gameObject.AddComponent<LampPanel>();
            component.Bind(panel.gameObject, headline, rates, pity, result, cost, flame, upgradeButton,
                           opener, BuildCloseButton(panel.transform));
            return component;
        }

        static Button OfferButton(Transform parent, string name, string label, Vector2 position)
        {
            var go = UiFactory.Panel(name, parent, MainSceneBuilder.Load("ui_frame_light.png"), Color.white, true);
            UiFactory.Anchor(go, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, new Vector2(360f, 96f));
            var text = UiFactory.Label("Label", go.transform, label, 30, Ink, TextAnchor.MiddleCenter);
            UiFactory.Place(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return go.GetComponent<Button>();
        }

        internal static Button BuildCloseButton(Transform parent)
        {
            var go = UiFactory.Panel("Close", parent, MainSceneBuilder.Load("ui_btn_round.png"), Color.white, true);
            UiFactory.Anchor(go, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(88f, 88f));
            var text = UiFactory.Label("Label", go.transform, "X", 36, Ink, TextAnchor.MiddleCenter);
            UiFactory.Place(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return go.GetComponent<Button>();
        }
    }
}
