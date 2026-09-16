using GrowNa.Core;
using GrowNa.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.EditorTools
{
    public static class QuestPanelBuilder
    {
        const float ButtonX = -390f;
        const float ButtonY = 1078f;

        public static QuestPanel BuildQuestPanel(Transform hudRoot, Transform popupRoot)
        {
            Button opener = OpenerButton(hudRoot, out GameObject badge, out Text badgeLabel);

            var panel = UiFactory.Sprite("QuestPanel", popupRoot, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            panel.raycastTarget = true;
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(0f, -20f), new Vector2(960f, 1080f));

            var title = UiFactory.Label("Title", panel.transform, "반복 퀘스트", 44, PanelBuilder.Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(700f, 60f));
            UiFactory.Shadow(title.gameObject);

            var notice = UiFactory.Label("Notice", panel.transform, "수령하면 같은 퀘스트가 다시 시작됩니다", 24,
                                         new Color(0.86f, 0.55f, 0.42f), TextAnchor.MiddleCenter);
            UiFactory.Anchor(notice.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(860f, 40f));

            var rows = new QuestPanel.Row[QuestTable.Count];
            const float rowH = 132f;
            const float rowGap = 14f;
            for (int i = 0; i < rows.Length; i++)
                rows[i] = BuildRow(panel.transform, i, -180f - i * (rowH + rowGap), rowH);

            var hint = UiFactory.Label("Hint", panel.transform, "", 26, PanelBuilder.Gold, TextAnchor.MiddleCenter);
            UiFactory.Anchor(hint.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(880f, 44f));

            Button claimAll = ActionButton(panel.transform, "ClaimAll", "전부 수령", new Vector2(0f, 96f));

            var component = panel.gameObject.AddComponent<QuestPanel>();
            component.Bind(panel.gameObject, rows, hint, claimAll, opener,
                           PanelBuilder.BuildCloseButton(panel.transform), badge, badgeLabel);
            return component;
        }

        static QuestPanel.Row BuildRow(Transform parent, int index, float y, float height)
        {
            var def = QuestTable.Get(index);
            var row = UiFactory.Sprite($"Quest_{index}", parent, MainSceneBuilder.Load("ui_frame_light.png"), Color.white);
            UiFactory.Anchor(row.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(880f, height));

            var title = UiFactory.Label("Title", row.transform, def.title, 28, PanelBuilder.Ink);
            UiFactory.Anchor(title.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(520f, 42f));

            var detail = UiFactory.Label("Detail", row.transform, "", 22, new Color(0.42f, 0.34f, 0.22f));
            UiFactory.Anchor(detail.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -58f), new Vector2(520f, 34f));

            var barBg = UiFactory.Sprite("BarBg", row.transform, MainSceneBuilder.Load("bar_bg.png"), Color.white);
            UiFactory.Anchor(barBg.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 16f), new Vector2(520f, 26f));

            var fill = UiFactory.Sprite("BarFill", barBg.transform, MainSceneBuilder.Load("bar_exp.png"), new Color(0.62f, 0.89f, 0.53f));
            UiFactory.Place(fill.gameObject, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;

            var claim = UiFactory.Panel($"Claim_{index}", row.transform, MainSceneBuilder.Load("ui_btn_round.png"), Color.white, true);
            UiFactory.Anchor(claim, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(260f, 92f));
            var claimLabel = UiFactory.Label("Label", claim.transform, "진행 중", 28, PanelBuilder.Ink, TextAnchor.MiddleCenter);
            UiFactory.Place(claimLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return new QuestPanel.Row
            {
                title = title,
                detail = detail,
                fill = fill,
                claim = claim.GetComponent<Button>(),
                claimLabel = claimLabel,
            };
        }

        static Button OpenerButton(Transform root, out GameObject badge, out Text badgeLabel)
        {
            var go = UiFactory.Panel("QuestButton", root, MainSceneBuilder.Load("ui_frame_light.png"), Color.white, true);
            UiFactory.Anchor(go, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                             new Vector2(ButtonX, ButtonY), new Vector2(150f, 88f));
            var label = UiFactory.Label("Label", go.transform, "반복\n퀘스트", 24, PanelBuilder.Ink, TextAnchor.MiddleCenter);
            UiFactory.Place(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            badge = UiFactory.Panel("Badge", go.transform, MainSceneBuilder.Load("ui_btn_round.png"),
                                    new Color(0.86f, 0.32f, 0.28f), false);
            UiFactory.Anchor(badge, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(48f, 48f));
            badgeLabel = UiFactory.Label("Count", badge.transform, "0", 26, PanelBuilder.Cream, TextAnchor.MiddleCenter);
            UiFactory.Place(badgeLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            badge.SetActive(false);

            return go.GetComponent<Button>();
        }

        static Button ActionButton(Transform parent, string name, string text, Vector2 position)
        {
            var go = UiFactory.Panel(name, parent, MainSceneBuilder.Load("ui_btn_round.png"),
                                     new Color(0.78f, 0.62f, 0.30f), true);
            UiFactory.Anchor(go, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, new Vector2(400f, 110f));
            var label = UiFactory.Label("Label", go.transform, text, 34, PanelBuilder.Cream, TextAnchor.MiddleCenter);
            UiFactory.Place(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFactory.Shadow(label.gameObject);
            return go.GetComponent<Button>();
        }
    }
}
