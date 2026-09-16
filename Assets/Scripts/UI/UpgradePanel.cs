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
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;

        Wallet wallet;
        PlayerStats stats;
        UpgradeService upgrades;

        static readonly Color Affordable = new Color(0.98f, 0.83f, 0.36f);
        static readonly Color TooExpensive = new Color(0.55f, 0.52f, 0.48f);

        public void Bind(GameObject panelRoot, Row[] boundRows, Button open, Button close)
        {
            root = panelRoot;
            rows = boundRows;
            openButton = open;
            closeButton = close;
        }

        public void BindServices(Wallet boundWallet, PlayerStats boundStats, UpgradeService boundUpgrades)
        {
            UnbindServices();
            wallet = boundWallet;
            stats = boundStats;
            upgrades = boundUpgrades;
            if (wallet != null) wallet.Changed += Refresh;
            if (stats != null) stats.StatsChanged += Refresh;
            Refresh();
        }

        void Start()
        {
            WireButtons();
            SetOpen(false);
        }

        void WireButtons()
        {
            foreach (var row in rows)
            {
                if (row.button == null) continue;
                var kind = row.kind;
                row.button.onClick.AddListener(() => Upgrade(kind));
            }
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (closeButton != null) closeButton.onClick.AddListener(() => SetOpen(false));
        }

        void OnDestroy() => UnbindServices();

        void UnbindServices()
        {
            if (wallet != null) wallet.Changed -= Refresh;
            if (stats != null) stats.StatsChanged -= Refresh;
        }

        public void Toggle() => SetOpen(root != null && !root.activeSelf);

        public void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void Upgrade(StatKind kind)
        {
            upgrades?.TryUpgrade(kind);
            Refresh();
        }

        void Refresh()
        {
            if (stats == null || upgrades == null) return;

            foreach (var row in rows)
            {
                int level = stats.UpgradeLevel(row.kind);
                double cost = UpgradeTable.Cost(row.kind, level);
                bool affordable = upgrades.CanAfford(row.kind);

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
