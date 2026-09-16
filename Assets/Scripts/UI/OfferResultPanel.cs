using System;
using GrowNa.Core;
using GrowNa.Gear;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class OfferResultPanel : MonoBehaviour
    {
        [Serializable]
        public struct Card
        {
            public Image icon;
            public Text title;
            public Text subtitle;
            public Text statNames;
            public Text statValues;
            public Text statDeltas;
        }

        const string StatRows = "HP\n공격력\n방어력\n공속";
        static readonly Color Empty = new Color(0.55f, 0.52f, 0.48f);

        [SerializeField] GameObject root;
        [SerializeField] Card current;
        [SerializeField] Card fresh;
        [SerializeField] Sprite[] slotSprites;
        [SerializeField] Text sellLabel;
        [SerializeField] Button sellButton;
        [SerializeField] Button equipButton;

        Inventory inventory;
        Loadout loadout;
        PlayerStats stats;
        AutoOfferService auto;
        GearItem pending;

        public void Bind(GameObject panelRoot, Card currentCard, Card freshCard, Sprite[] sprites,
                         Text sellText, Button sell, Button equip)
        {
            root = panelRoot;
            current = currentCard;
            fresh = freshCard;
            slotSprites = sprites;
            sellLabel = sellText;
            sellButton = sell;
            equipButton = equip;
        }

        public void BindServices(Inventory boundInventory, Loadout boundLoadout, PlayerStats boundStats, AutoOfferService boundAuto)
        {
            if (auto != null) auto.Kept -= Show;
            inventory = boundInventory;
            loadout = boundLoadout;
            stats = boundStats;
            auto = boundAuto;
            if (auto != null) auto.Kept += Show;
        }

        void Start()
        {
            if (sellButton != null) sellButton.onClick.AddListener(Sell);
            if (equipButton != null) equipButton.onClick.AddListener(Equip);
            SetOpen(false);
        }

        void OnDestroy()
        {
            if (auto != null) auto.Kept -= Show;
        }

        public void Show(GearItem item)
        {
            if (!item.owned) return;
            if (pending.owned) inventory?.Acquire(pending);
            pending = item;
            SetOpen(true);
        }

        public void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void Sell()
        {
            if (pending.owned) inventory?.Sell(pending);
            Dismiss();
        }

        void Equip()
        {
            if (pending.owned) inventory?.Equip(pending);
            Dismiss();
        }

        void Dismiss()
        {
            pending = GearItem.Empty;
            SetOpen(false);
        }

        void Refresh()
        {
            if (loadout == null || stats == null || !pending.owned) return;

            var worn = loadout.Get(pending.slot);
            var nowSnap = stats.SnapshotWith(loadout.CurrentBonus());
            var nextSnap = stats.SnapshotWith(loadout.BonusWith(pending));

            FillCard(current, worn, nowSnap, nowSnap, GearTable.Name(pending.slot));
            FillCard(fresh, pending, nextSnap, nowSnap, GearTable.Name(pending.slot));

            if (sellLabel != null) sellLabel.text = $"판매 {BigNum.Format(pending.SellGold)}";
        }

        void FillCard(Card card, GearItem item, CombatSnapshot snap, CombatSnapshot baseline, string slotName)
        {
            if (card.icon != null)
            {
                bool hasSprite = slotSprites != null && (int)pending.slot < slotSprites.Length;
                card.icon.sprite = hasSprite ? slotSprites[(int)pending.slot] : card.icon.sprite;
                card.icon.color = item.owned ? GearTable.Color(item.tier) : Empty;
            }

            if (card.title != null)
            {
                card.title.text = item.owned ? item.DisplayName : "빈 슬롯";
                card.title.color = item.owned ? GearTable.Color(item.tier) : Empty;
            }

            if (card.subtitle != null)
                card.subtitle.text = item.owned
                    ? $"{slotName} · 장비 전투력 {BigNum.Format(item.Power)}"
                    : $"{slotName} · 착용 없음";

            if (card.statNames != null) card.statNames.text = StatRows;
            if (card.statValues != null) card.statValues.text = Values(snap);
            if (card.statDeltas != null) card.statDeltas.text = Deltas(snap, baseline);
        }

        static string Values(CombatSnapshot s)
            => $"{BigNum.Format(s.maxHp)}\n{BigNum.Format(s.attack)}\n{BigNum.Format(s.defense)}\n{s.attackSpeed:0.0}";

        static string Deltas(CombatSnapshot s, CombatSnapshot baseline)
            => $"{Arrow(s.maxHp - baseline.maxHp)}\n{Arrow(s.attack - baseline.attack)}\n" +
               $"{Arrow(s.defense - baseline.defense)}\n{Arrow(s.attackSpeed - baseline.attackSpeed)}";

        static string Arrow(double delta)
            => delta > 0 ? "<color=#8FBF6A>▲</color>"
             : delta < 0 ? "<color=#D4595C>▼</color>"
             : "";
    }
}
