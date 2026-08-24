using GrowNa.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class UpgradePanel : MonoBehaviour
    {
        [System.Serializable]
        public struct Row
        {
            public StatKind kind;
            public Text title;
            public Text detail;
            public Text cost;
            public Button button;
            public Image buttonImage;
        }

        [SerializeField] Row[] rows = new Row[3];
        [SerializeField] GameObject root;

        static readonly Color Affordable = new Color(0.98f, 0.83f, 0.36f);
        static readonly Color TooExpensive = new Color(0.55f, 0.52f, 0.48f);

        public void Bind(GameObject panelRoot, Row[] boundRows)
        {
            root = panelRoot;
            rows = boundRows;
            foreach (var row in rows)
            {
                var kind = row.kind;
                row.button.onClick.AddListener(() => Upgrade(kind));
            }
        }

        void Start()
        {
            if (Wallet.Instance != null) Wallet.Instance.Changed += Refresh;
            if (PlayerStats.Instance != null) PlayerStats.Instance.StatsChanged += Refresh;
            Refresh();
            SetOpen(false);
        }

        void OnDestroy()
        {
            if (Wallet.Instance != null) Wallet.Instance.Changed -= Refresh;
            if (PlayerStats.Instance != null) PlayerStats.Instance.StatsChanged -= Refresh;
        }

        public void Toggle() => SetOpen(root != null && !root.activeSelf);

        public void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void Upgrade(StatKind kind)
        {
            UpgradeService.Instance?.TryUpgrade(kind);
            Refresh();
        }

        void Refresh()
        {
            var stats = PlayerStats.Instance;
            var service = UpgradeService.Instance;
            if (stats == null || service == null) return;

            foreach (var row in rows)
            {
                int level = stats.UpgradeLevel(row.kind);
                double cost = UpgradeTable.Cost(row.kind, level);
                bool affordable = service.CanAfford(row.kind);

                if (row.title != null) row.title.text = $"{UpgradeTable.DisplayName(row.kind)} Lv.{level}";
                if (row.detail != null) row.detail.text = UpgradeTable.BonusText(row.kind, level);
                if (row.cost != null)
                {
                    row.cost.text = BigNum.Format(cost);
                    row.cost.color = affordable ? Affordable : TooExpensive;
                }
                if (row.buttonImage != null)
                    row.buttonImage.color = affordable ? Color.white : new Color(0.62f, 0.6f, 0.58f);
            }
        }
    }
}
