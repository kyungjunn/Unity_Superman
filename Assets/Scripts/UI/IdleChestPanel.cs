using GrowNa.Battle;
using GrowNa.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class IdleChestPanel : MonoBehaviour
    {
        [SerializeField] GameObject chestButtonRoot;
        [SerializeField] Button chestButton;
        [SerializeField] GameObject root;
        [SerializeField] Text durationLabel;
        [SerializeField] Text goldLabel;
        [SerializeField] Text grainLabel;
        [SerializeField] Text noticeLabel;
        [SerializeField] Button claimButton;
        [SerializeField] Button adClaimButton;
        [SerializeField] Button closeButton;

        IdleChestService chest;

        public void Bind(GameObject buttonRoot, Button opener, GameObject panelRoot, Text duration,
                         Text gold, Text grain, Text notice, Button claim, Button adClaim, Button close)
        {
            chestButtonRoot = buttonRoot;
            chestButton = opener;
            root = panelRoot;
            durationLabel = duration;
            goldLabel = gold;
            grainLabel = grain;
            noticeLabel = notice;
            claimButton = claim;
            adClaimButton = adClaim;
            closeButton = close;
        }

        public void BindServices(IdleChestService bound)
        {
            if (chest != null) chest.Changed -= RefreshChestButton;
            chest = bound;
            if (chest != null) chest.Changed += RefreshChestButton;
            RefreshChestButton();
        }

        void Start()
        {
            if (chestButton != null) chestButton.onClick.AddListener(Open);
            if (claimButton != null) claimButton.onClick.AddListener(() => Claim(false));
            if (adClaimButton != null) adClaimButton.onClick.AddListener(() => Claim(true));
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            SetOpen(false);
        }

        void OnDestroy()
        {
            if (chest != null) chest.Changed -= RefreshChestButton;
        }

        void RefreshChestButton()
        {
            if (chestButtonRoot == null || chest == null) return;
            if (chestButtonRoot.activeSelf != chest.Ready) chestButtonRoot.SetActive(chest.Ready);
        }

        void Open() => SetOpen(true);

        void Close() => SetOpen(false);

        void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void Claim(bool doubled)
        {
            if (chest == null) return;
            if (!chest.Claim(doubled) && noticeLabel != null)
            {
                noticeLabel.text = "아직 쌓인 보상이 없습니다";
                return;
            }
            SetOpen(false);
        }

        void Update() => Refresh();

        void Refresh()
        {
            if (chest == null) return;

            var single = chest.Preview(false);
            var doubled = chest.Preview(true);

            if (durationLabel != null) durationLabel.text = $"방치 {BigNum.Duration(single.seconds)}";
            if (goldLabel != null) goldLabel.text = BigNum.Format(single.gold);
            if (grainLabel != null) grainLabel.text = BigNum.Format(single.grain);
            if (noticeLabel != null)
                noticeLabel.text = single.capped
                    ? $"최대 {BigNum.Duration(OfflineRewards.MaxSeconds)}까지 쌓입니다"
                    : $"광고 시청 시 골드 {BigNum.Format(doubled.gold)} · 낟알 {BigNum.Format(doubled.grain)}";
        }
    }
}
