using System;
using GrowNa.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class QuestPanel : MonoBehaviour
    {
        [Serializable]
        public struct Row
        {
            public Text title;
            public Text detail;
            public Image fill;
            public Button claim;
            public Text claimLabel;
        }

        static readonly Color Ready = new Color(0.98f, 0.83f, 0.36f);
        static readonly Color Waiting = new Color(0.55f, 0.53f, 0.50f);

        [SerializeField] GameObject root;
        [SerializeField] Row[] rows = new Row[QuestTable.Count];
        [SerializeField] Text hintLabel;
        [SerializeField] Button claimAllButton;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] GameObject badge;
        [SerializeField] Text badgeLabel;

        QuestService quests;

        public void Bind(GameObject panelRoot, Row[] boundRows, Text hint, Button claimAll,
                         Button open, Button close, GameObject badgeRoot, Text badgeText)
        {
            root = panelRoot;
            rows = boundRows;
            hintLabel = hint;
            claimAllButton = claimAll;
            openButton = open;
            closeButton = close;
            badge = badgeRoot;
            badgeLabel = badgeText;
        }

        public void BindServices(QuestService bound)
        {
            if (quests != null) quests.Changed -= Refresh;
            quests = bound;
            if (quests != null) quests.Changed += Refresh;
            Refresh();
        }

        void Start()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                var kind = (QuestKind)i;
                if (rows[i].claim != null) rows[i].claim.onClick.AddListener(() => Claim(kind));
            }

            if (claimAllButton != null) claimAllButton.onClick.AddListener(ClaimAll);
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (closeButton != null) closeButton.onClick.AddListener(() => SetOpen(false));
            SetOpen(false);
        }

        void OnDestroy()
        {
            if (quests != null) quests.Changed -= Refresh;
        }

        public void Toggle() => SetOpen(root != null && !root.activeSelf);

        public void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void Claim(QuestKind kind)
        {
            if (quests == null) return;
            var def = QuestTable.Get(kind);
            Hint(quests.TryClaim(kind)
                ? $"{def.title} 완료 · 다이아 +{def.gem:0}"
                : $"{def.title} {quests.Remaining(kind)}{def.unit} 남았습니다");
            Refresh();
        }

        void ClaimAll()
        {
            if (quests == null) return;
            int claimed = quests.ClaimAll();
            Hint(claimed > 0 ? $"{claimed}개 수령했습니다" : "수령할 퀘스트가 없습니다");
            Refresh();
        }

        void Hint(string message)
        {
            if (hintLabel != null) hintLabel.text = message;
        }

        void Refresh()
        {
            if (quests == null) return;

            for (int i = 0; i < rows.Length && i < QuestTable.Count; i++)
            {
                var kind = (QuestKind)i;
                var def = QuestTable.Get(kind);
                bool ready = quests.IsReady(kind);

                if (rows[i].title != null) rows[i].title.text = def.Describe(quests.Progress(kind));
                if (rows[i].detail != null)
                    rows[i].detail.text = $"다이아 {def.gem:0} · {quests.Rounds(kind)}회 완료";
                if (rows[i].fill != null) rows[i].fill.fillAmount = (float)quests.Ratio(kind);
                if (rows[i].claim != null) rows[i].claim.interactable = ready;
                if (rows[i].claimLabel != null)
                {
                    rows[i].claimLabel.text = ready ? "수령" : "진행 중";
                    rows[i].claimLabel.color = ready ? Ready : Waiting;
                }
            }

            int readyCount = quests.ReadyCount;
            if (claimAllButton != null) claimAllButton.interactable = readyCount > 0;
            if (badge != null) badge.SetActive(readyCount > 0);
            if (badgeLabel != null) badgeLabel.text = readyCount.ToString();
        }
    }
}
