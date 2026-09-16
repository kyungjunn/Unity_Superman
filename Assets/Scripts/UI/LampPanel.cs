using GrowNa.Core;
using GrowNa.Gear;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class LampPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text headline;
        [SerializeField] Text rateTable;
        [SerializeField] Text pityLabel;
        [SerializeField] Text resultLabel;
        [SerializeField] Text upgradeCost;
        [SerializeField] Image flame;
        [SerializeField] Button upgrade;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;

        Wallet wallet;
        LampService lamp;

        public void Bind(GameObject panelRoot, Text head, Text rates, Text pity, Text result,
                         Text cost, Image flameImage, Button upgradeButton, Button open, Button close)
        {
            root = panelRoot;
            headline = head;
            rateTable = rates;
            pityLabel = pity;
            resultLabel = result;
            upgradeCost = cost;
            flame = flameImage;
            upgrade = upgradeButton;
            openButton = open;
            closeButton = close;
        }

        public void BindServices(Wallet boundWallet, LampService boundLamp)
        {
            UnbindServices();
            wallet = boundWallet;
            lamp = boundLamp;
            if (wallet != null) wallet.Changed += Refresh;
            if (lamp != null) lamp.Changed += Refresh;
            Refresh();
        }

        void WireButtons()
        {
            if (upgrade != null) upgrade.onClick.AddListener(UpgradeLamp);
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (closeButton != null) closeButton.onClick.AddListener(() => SetOpen(false));
        }

        void Start()
        {
            WireButtons();
            SetOpen(false);
        }

        void OnDestroy() => UnbindServices();

        void UnbindServices()
        {
            if (wallet != null) wallet.Changed -= Refresh;
            if (lamp != null) lamp.Changed -= Refresh;
        }

        public void Toggle() => SetOpen(root != null && !root.activeSelf);

        public void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void UpgradeLamp()
        {
            if (lamp == null) return;
            double before = lamp.LegendChance;
            if (!lamp.TryUpgrade())
            {
                if (resultLabel != null) resultLabel.text = "심지 또는 골드가 모자란다";
                return;
            }
            if (resultLabel != null)
            {
                resultLabel.text = $"심지 교체! 전설 {BigNum.Percent(before)} → {BigNum.Percent(lamp.LegendChance)}";
                resultLabel.color = LampTable.FlameColor(lamp.LampLevel);
            }
            Refresh();
        }

        void Refresh()
        {
            if (lamp == null) return;

            if (headline != null)
                headline.text = $"소원의 등불 Lv.{lamp.LampLevel}";

            if (rateTable != null)
            {
                var sb = new System.Text.StringBuilder();
                var weights = LampTable.Weights(lamp.LampLevel);
                for (int i = weights.Length - 1; i >= 0; i--)
                {
                    if (weights[i] <= 0) continue;
                    sb.AppendLine($"{GearTable.Name((GearTier)i)}  {BigNum.Percent(weights[i])}");
                }
                rateTable.text = sb.ToString();
            }

            if (pityLabel != null)
                pityLabel.text = $"{GearTable.Name(lamp.HighestTier)} 확정까지 {lamp.OfferingsUntilPity}회";

            if (upgradeCost != null)
                upgradeCost.text = lamp.LampLevel >= LampTable.MaxLevel
                    ? "최대 레벨"
                    : $"심지 {BigNum.Format(lamp.WickCost)} · 골드 {BigNum.Format(lamp.GoldCost)}";

            if (flame != null) flame.color = lamp.FlameColor;
            if (upgrade != null) upgrade.interactable = lamp.CanUpgrade();
        }
    }
}
