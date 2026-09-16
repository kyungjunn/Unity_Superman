using GrowNa.Gear;
using GrowNa.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.EditorTools
{
    public static class OfferPanelBuilder
    {
        static Color Cream => PanelBuilder.Cream;
        static Color Gold => PanelBuilder.Gold;
        static Color Dim => PanelBuilder.Dim;
        static Color Ink => PanelBuilder.Ink;

        static readonly Color SellRed = new Color(0.86f, 0.34f, 0.34f);
        static readonly Color EquipGreen = new Color(0.45f, 0.75f, 0.35f);

        static readonly string[] SlotSpriteFiles =
        {
            "gear_weapon.png", "gear_helmet.png", "gear_armor.png", "gear_gloves.png",
            "gear_boots.png", "gear_amulet.png", "gear_ring.png", "gear_charm.png",
        };

        public static OfferResultPanel BuildResultPanel(Transform root)
        {
            var panel = UiFactory.Sprite("OfferResultPanel", root, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            panel.raycastTarget = true;
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(0f, -20f), new Vector2(940f, 940f));

            Header(panel.transform, "NEW", Gold, -36f);
            var fresh = BuildCard(panel.transform, "NewCard", -200f, true);

            Header(panel.transform, "현재 장비", Dim, -368f);
            var current = BuildCard(panel.transform, "CurrentCard", -530f, false);

            Button sell = ActionButton(panel.transform, "Sell", "판매", SellRed, new Vector2(-215f, 110f), out Text sellLabel);
            Button equip = ActionButton(panel.transform, "Equip", "장비", EquipGreen, new Vector2(215f, 110f), out _);

            var sprites = new Sprite[GearTable.SlotCount];
            for (int i = 0; i < sprites.Length; i++) sprites[i] = MainSceneBuilder.Load(SlotSpriteFiles[i]);

            var component = panel.gameObject.AddComponent<OfferResultPanel>();
            component.Bind(panel.gameObject, current, fresh, sprites, sellLabel, sell, equip);
            return component;
        }

        static void Header(Transform parent, string text, Color color, float y)
        {
            var label = UiFactory.Label($"Header_{text}", parent, text, 28, color);
            UiFactory.Anchor(label.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(40f, y), new Vector2(320f, 44f));
        }

        static OfferResultPanel.Card BuildCard(Transform parent, string name, float y, bool withDeltas)
        {
            var frame = UiFactory.Sprite(name, parent, MainSceneBuilder.Load("ui_frame_light.png"), Color.white);
            UiFactory.Anchor(frame.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, y), new Vector2(860f, 280f));

            var icon = UiFactory.Sprite("Icon", frame.transform, null, Color.white);
            UiFactory.Anchor(icon.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(24f, -24f), new Vector2(130f, 130f));

            var title = UiFactory.Label("Title", frame.transform, "", 34, Ink);
            UiFactory.Anchor(title.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(176f, -18f), new Vector2(660f, 50f));

            var subtitle = UiFactory.Label("Subtitle", frame.transform, "", 24, new Color(0.42f, 0.34f, 0.22f), TextAnchor.MiddleLeft, false);
            UiFactory.Anchor(subtitle.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(176f, -68f), new Vector2(660f, 38f));

            var names = UiFactory.Label("StatNames", frame.transform, "", 26, new Color(0.38f, 0.30f, 0.18f), TextAnchor.UpperLeft);
            UiFactory.Anchor(names.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(176f, -112f), new Vector2(160f, 150f));

            var values = UiFactory.Label("StatValues", frame.transform, "", 26, Ink, TextAnchor.UpperRight);
            UiFactory.Anchor(values.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(336f, -112f), new Vector2(240f, 150f));

            Text deltas = null;
            if (withDeltas)
            {
                deltas = UiFactory.Label("StatDeltas", frame.transform, "", 26, Ink, TextAnchor.UpperLeft);
                UiFactory.Anchor(deltas.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                                 new Vector2(590f, -112f), new Vector2(90f, 150f));
            }

            return new OfferResultPanel.Card
            {
                icon = icon,
                title = title,
                subtitle = subtitle,
                statNames = names,
                statValues = values,
                statDeltas = deltas,
            };
        }

        static Button ActionButton(Transform parent, string name, string text, Color tint, Vector2 position, out Text label)
        {
            var go = UiFactory.Panel(name, parent, MainSceneBuilder.Load("ui_btn_round.png"), tint, true);
            UiFactory.Anchor(go, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, new Vector2(400f, 120f));
            label = UiFactory.Label("Label", go.transform, text, 38, Cream, TextAnchor.MiddleCenter);
            UiFactory.Place(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFactory.Shadow(label.gameObject);
            return go.GetComponent<Button>();
        }

        public static AutoOfferPanel BuildAutoPanel(Transform root, Button opener)
        {
            var panel = UiFactory.Sprite("AutoOfferPanel", root, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            panel.raycastTarget = true;
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(0f, -20f), new Vector2(760f, 760f));

            var title = UiFactory.Label("Title", panel.transform, "자동 점등", 42, Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(600f, 56f));
            UiFactory.Shadow(title.gameObject);

            Button tierDown = null, tierUp = null, batchDown = null, batchUp = null;
            Text tierValue = StepperRow(panel.transform, "Tier", "장비 등급", -160f, ref tierDown, ref tierUp);
            Text batchValue = StepperRow(panel.transform, "Batch", "1회 낟알 수량", -290f, ref batchDown, ref batchUp);

            var notice = UiFactory.Label("Notice", panel.transform, "기준 미만 장비는 자동으로 판매합니다\n기준 이상이 나오면 멈추고 보여줍니다",
                                         22, new Color(0.86f, 0.55f, 0.42f), TextAnchor.MiddleCenter, false);
            UiFactory.Anchor(notice.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(660f, 80f));

            var status = UiFactory.Label("Status", panel.transform, "", 24, Gold, TextAnchor.MiddleCenter);
            UiFactory.Anchor(status.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -490f), new Vector2(680f, 48f));

            Button start = ActionButton(panel.transform, "Start", "시작", new Color(0.78f, 0.62f, 0.30f), new Vector2(0f, 96f), out Text startLabel);

            var component = panel.gameObject.AddComponent<AutoOfferPanel>();
            component.Bind(panel.gameObject, tierValue, batchValue, status, startLabel,
                           tierDown, tierUp, batchDown, batchUp, start, opener,
                           PanelBuilder.BuildCloseButton(panel.transform));
            return component;
        }

        static Text StepperRow(Transform parent, string name, string caption, float y, ref Button minus, ref Button plus)
        {
            var row = UiFactory.Sprite($"Row_{name}", parent, MainSceneBuilder.Load("ui_frame_light.png"), Color.white);
            UiFactory.Anchor(row.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(680f, 104f));

            var label = UiFactory.Label("Caption", row.transform, caption, 28, Ink);
            UiFactory.Anchor(label.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(260f, 56f));

            minus = StepButton(row.transform, $"{name}Down", "＜", new Vector2(-286f, 0f));
            plus = StepButton(row.transform, $"{name}Up", "＞", new Vector2(-24f, 0f));

            var value = UiFactory.Label("Value", row.transform, "", 28, Ink, TextAnchor.MiddleCenter);
            UiFactory.Anchor(value.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-155f, 0f), new Vector2(190f, 56f));
            return value;
        }

        static Button StepButton(Transform parent, string name, string glyph, Vector2 position)
        {
            var go = UiFactory.Panel(name, parent, MainSceneBuilder.Load("ui_btn_round.png"), Color.white, true);
            UiFactory.Anchor(go, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), position, new Vector2(72f, 72f));
            var text = UiFactory.Label("Label", go.transform, glyph, 30, Ink, TextAnchor.MiddleCenter);
            UiFactory.Place(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return go.GetComponent<Button>();
        }
    }
}
