using GrowNa.Core;
using GrowNa.Gear;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class AutoOfferPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text tierValue;
        [SerializeField] Text batchValue;
        [SerializeField] Text statusLabel;
        [SerializeField] Text startLabel;
        [SerializeField] Button tierDown;
        [SerializeField] Button tierUp;
        [SerializeField] Button batchDown;
        [SerializeField] Button batchUp;
        [SerializeField] Button startButton;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;

        AutoOfferService auto;
        Wallet wallet;

        public void Bind(GameObject panelRoot, Text tier, Text batch, Text status, Text start,
                         Button tierMinus, Button tierPlus, Button batchMinus, Button batchPlus,
                         Button startToggle, Button open, Button close)
        {
            root = panelRoot;
            tierValue = tier;
            batchValue = batch;
            statusLabel = status;
            startLabel = start;
            tierDown = tierMinus;
            tierUp = tierPlus;
            batchDown = batchMinus;
            batchUp = batchPlus;
            startButton = startToggle;
            openButton = open;
            closeButton = close;
        }

        public void BindServices(AutoOfferService boundAuto, Wallet boundWallet)
        {
            UnbindServices();
            auto = boundAuto;
            wallet = boundWallet;
            if (auto != null) auto.Changed += Refresh;
            if (wallet != null) wallet.Changed += Refresh;
            Refresh();
        }

        void Start()
        {
            if (tierDown != null) tierDown.onClick.AddListener(() => Step(-1, 0));
            if (tierUp != null) tierUp.onClick.AddListener(() => Step(1, 0));
            if (batchDown != null) batchDown.onClick.AddListener(() => Step(0, -1));
            if (batchUp != null) batchUp.onClick.AddListener(() => Step(0, 1));
            if (startButton != null) startButton.onClick.AddListener(ToggleAuto);
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (closeButton != null) closeButton.onClick.AddListener(() => SetOpen(false));
            SetOpen(false);
        }

        void OnDestroy() => UnbindServices();

        void UnbindServices()
        {
            if (auto != null) auto.Changed -= Refresh;
            if (wallet != null) wallet.Changed -= Refresh;
        }

        public void Toggle() => SetOpen(root != null && !root.activeSelf);

        public void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void Step(int tierDelta, int batchDelta)
        {
            if (auto == null) return;
            if (tierDelta != 0) auto.StepKeepTier(tierDelta);
            if (batchDelta != 0) auto.StepBatchSize(batchDelta);
            Refresh();
        }

        void ToggleAuto()
        {
            auto?.Toggle();
            Refresh();
        }

        void Refresh()
        {
            if (auto == null) return;

            if (tierValue != null)
            {
                tierValue.text = $"{GearTable.Name(auto.KeepTier)} 이상";
                tierValue.color = GearTable.Color(auto.KeepTier);
            }

            if (batchValue != null) batchValue.text = $"{auto.BatchSize}개";
            if (startLabel != null) startLabel.text = auto.Running ? "중지" : "시작";

            if (statusLabel != null)
                statusLabel.text = auto.Running
                    ? $"점등 중 — {auto.SoldCount}개 판매 · 골드 +{BigNum.Format(auto.SoldGold)}"
                    : $"1회 {BigNum.Format(auto.BatchCost)} 낟알 · 보유 {BigNum.Format(wallet == null ? 0 : wallet.Grain)}";
        }
    }
}
