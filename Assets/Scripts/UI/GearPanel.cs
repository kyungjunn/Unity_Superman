using GrowNa.Core;
using GrowNa.Gear;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class GearPanel : MonoBehaviour
    {
        [System.Serializable]
        public struct Row
        {
            public GearSlot slot;
            public Text title;
            public Text detail;
            public Text cost;
            public Button button;
            public Image buttonImage;
        }

        [SerializeField] GameObject root;
        [SerializeField] Row[] rows;
        [SerializeField] Text stashLabel;
        [SerializeField] Text resultLabel;
        [SerializeField] Button dismantleButton;
        [SerializeField] Button fuseButton;
        [SerializeField] Button[] openButtons;
        [SerializeField] Button closeButton;

        Wallet wallet;
        Loadout loadout;
        Inventory inventory;
        EnhanceService enhance;

        static readonly Color Affordable = Color.white;
        static readonly Color TooExpensive = new Color(0.55f, 0.52f, 0.48f);

        public void Bind(GameObject panelRoot, Row[] gearRows, Text stash, Text result,
                         Button dismantle, Button fuse, Button[] opens, Button close)
        {
            root = panelRoot;
            rows = gearRows;
            stashLabel = stash;
            resultLabel = result;
            dismantleButton = dismantle;
            fuseButton = fuse;
            openButtons = opens;
            closeButton = close;
        }

        public void BindServices(Wallet boundWallet, Loadout boundLoadout, Inventory boundInventory, EnhanceService boundEnhance)
        {
            UnbindServices();
            wallet = boundWallet;
            loadout = boundLoadout;
            inventory = boundInventory;
            enhance = boundEnhance;
            if (wallet != null) wallet.Changed += Refresh;
            if (loadout != null) loadout.Changed += Refresh;
            if (inventory != null) inventory.Changed += Refresh;
            if (enhance != null) enhance.Changed += Refresh;
            Refresh();
        }

        void WireButtons()
        {
            foreach (var row in rows)
            {
                if (row.button == null) continue;
                var slot = row.slot;
                row.button.onClick.AddListener(() => Enhance(slot));
            }
            if (dismantleButton != null) dismantleButton.onClick.AddListener(DismantleSpares);
            if (fuseButton != null) fuseButton.onClick.AddListener(FuseAll);
            if (closeButton != null) closeButton.onClick.AddListener(() => SetOpen(false));
            if (openButtons != null)
                foreach (var opener in openButtons)
                    if (opener != null) opener.onClick.AddListener(Toggle);
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
            if (loadout != null) loadout.Changed -= Refresh;
            if (inventory != null) inventory.Changed -= Refresh;
            if (enhance != null) enhance.Changed -= Refresh;
        }

        public void Toggle() => SetOpen(root != null && !root.activeSelf);

        public void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void Enhance(GearSlot slot)
        {
            if (enhance == null || loadout == null) return;

            var before = loadout.Get(slot);
            if (!before.owned) { Say($"{GearTable.Name(slot)} 슬롯이 비었다", TooExpensive); return; }
            if (!enhance.CanEnhance(slot))
            {
                Say(EnhanceTable.IsMax(before.plus) ? "이미 최대 강화" : "골드가 모자란다", TooExpensive);
                return;
            }

            bool success = enhance.TryEnhance(slot);
            var after = loadout.Get(slot);
            Say(success
                    ? $"{after.DisplayName} +{after.plus} 성공!"
                    : $"강화 실패 — 연속 {enhance.FailStreak(slot)}회 (3회째 확정)",
                success ? GearTable.Color(after.tier) : TooExpensive);
        }

        void DismantleSpares()
        {
            if (inventory == null) return;

            int before = inventory.Stock.TotalCount;
            double shard = inventory.DismantleUpTo(GearTier.Heroic);
            Say(shard <= 0
                    ? "분해할 여분이 없다"
                    : $"{before - inventory.Stock.TotalCount}개 분해 → 파편 {BigNum.Format(shard)}",
                shard > 0 ? Affordable : TooExpensive);
        }

        void FuseAll()
        {
            if (inventory == null) return;

            int fused = 0;
            for (int s = 0; s < GearTable.SlotCount; s++)
                for (int t = 0; t < GearTable.TierCount; t++)
                    while (inventory.Fuse((GearSlot)s, (GearTier)t)) fused++;

            Say(fused > 0 ? $"합성 {fused}회 완료" : "재료가 모자란다 (같은 슬롯·티어 4개)",
                fused > 0 ? Affordable : TooExpensive);
        }

        void Say(string message, Color color)
        {
            if (resultLabel == null) return;
            resultLabel.text = message;
            resultLabel.color = color;
        }

        void Refresh()
        {
            if (loadout == null || rows == null) return;

            foreach (var row in rows)
            {
                var item = loadout.Get(row.slot);
                if (row.title != null) row.title.text = GearTable.Name(row.slot);

                if (row.detail != null)
                {
                    row.detail.text = item.owned
                        ? $"{item.ShortLabel} · 전투력 {BigNum.Format(item.Power)}"
                        : "빈 슬롯";
                    row.detail.color = item.owned ? GearTable.Color(item.tier) : TooExpensive;
                }

                double cost = enhance == null ? 0 : enhance.CostOf(row.slot);
                bool can = enhance != null && enhance.CanEnhance(row.slot);

                if (row.cost != null)
                    row.cost.text = !item.owned ? "-"
                        : EnhanceTable.IsMax(item.plus) ? "최대"
                        : $"{BigNum.Format(cost)} · {EnhanceTable.ChanceText(item.plus, enhance.FailStreak(row.slot))}";

                if (row.button != null) row.button.interactable = can;
                if (row.buttonImage != null) row.buttonImage.color = can ? Affordable : TooExpensive;
            }

            if (stashLabel != null)
            {
                stashLabel.text = inventory == null
                    ? ""
                    : $"재고 {inventory.Stock.TotalCount} · 도감 {inventory.CodexCount}/{GearTable.StockSize} " +
                      $"(전체 스탯 +{BigNum.Percent(inventory.CodexMultiplier - 1.0, 1)}) · " +
                      $"파편 {BigNum.Format(wallet == null ? 0 : wallet.Shard)}";
            }
        }
    }
}
