using System.Collections.Generic;
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
        [SerializeField] Button offerOnce;
        [SerializeField] Button offerTen;
        [SerializeField] Button upgrade;

        public void Bind(GameObject panelRoot, Text head, Text rates, Text pity, Text result,
                         Text cost, Image flameImage, Button once, Button ten, Button upgradeButton)
        {
            root = panelRoot;
            headline = head;
            rateTable = rates;
            pityLabel = pity;
            resultLabel = result;
            upgradeCost = cost;
            flame = flameImage;
            offerOnce = once;
            offerTen = ten;
            upgrade = upgradeButton;

            offerOnce.onClick.AddListener(() => Offer(1));
            offerTen.onClick.AddListener(() => Offer(LampTable.TenOfferCount));
            upgrade.onClick.AddListener(UpgradeLamp);
        }

        void Start()
        {
            if (Wallet.Instance != null) Wallet.Instance.Changed += Refresh;
            if (LampService.Instance != null) LampService.Instance.Changed += Refresh;
            Refresh();
            SetOpen(false);
        }

        void OnDestroy()
        {
            if (Wallet.Instance != null) Wallet.Instance.Changed -= Refresh;
            if (LampService.Instance != null) LampService.Instance.Changed -= Refresh;
        }

        public void Toggle() => SetOpen(root != null && !root.activeSelf);

        public void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void Offer(int count)
        {
            var lamp = LampService.Instance;
            if (lamp == null) return;
            List<GearItem> results = lamp.Offer(count);
            if (results == null)
            {
                if (resultLabel != null) resultLabel.text = "낟알이 모자란다";
                return;
            }

            GearItem best = results[0];
            foreach (var item in results) if (item.StrongerThan(best)) best = item;
            if (resultLabel != null)
            {
                resultLabel.text = count == 1
                    ? $"{best.DisplayName} 획득!"
                    : $"{count}회 봉헌 — 최고 {best.DisplayName}";
                resultLabel.color = GearTable.Color(best.tier);
            }
            Refresh();
        }

        void UpgradeLamp()
        {
            var lamp = LampService.Instance;
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
            var lamp = LampService.Instance;
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
            if (offerOnce != null) offerOnce.interactable = lamp.CanOffer(1);
            if (offerTen != null) offerTen.interactable = lamp.CanOffer(LampTable.TenOfferCount);
            if (upgrade != null) upgrade.interactable = lamp.CanUpgrade();
        }
    }
}
