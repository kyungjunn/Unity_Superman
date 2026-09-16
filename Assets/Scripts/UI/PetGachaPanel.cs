using System.Text;
using GrowNa.Core;
using GrowNa.Pet;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class PetGachaPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text headline;
        [SerializeField] Text ratesLabel;
        [SerializeField] Text activeLabel;
        [SerializeField] Text ownedLabel;
        [SerializeField] Text resultLabel;
        [SerializeField] Text costLabel;
        [SerializeField] Button drawButton;
        [SerializeField] Image drawButtonImage;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;

        Wallet wallet;
        PetService pets;

        static readonly Color TooExpensive = new Color(0.62f, 0.60f, 0.58f);

        public void Bind(GameObject panelRoot, Text title, Text rates, Text active, Text owned,
                         Text result, Text cost, Button draw, Image drawImage, Button open, Button close)
        {
            openButton = open;
            root = panelRoot;
            headline = title;
            ratesLabel = rates;
            activeLabel = active;
            ownedLabel = owned;
            resultLabel = result;
            costLabel = cost;
            drawButton = draw;
            drawButtonImage = drawImage;
            closeButton = close;
        }

        public void BindServices(Wallet boundWallet, PetService boundPets)
        {
            UnbindServices();
            wallet = boundWallet;
            pets = boundPets;
            if (wallet != null) wallet.Changed += Refresh;
            if (pets != null) pets.Changed += Refresh;
            Refresh();
        }

        void Start()
        {
            if (drawButton != null) drawButton.onClick.AddListener(Draw);
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (closeButton != null) closeButton.onClick.AddListener(() => SetOpen(false));
            SetOpen(false);
        }

        void OnDestroy() => UnbindServices();

        void UnbindServices()
        {
            if (wallet != null) wallet.Changed -= Refresh;
            if (pets != null) pets.Changed -= Refresh;
        }

        public void Toggle() => SetOpen(root != null && !root.activeSelf);

        public void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void Draw()
        {
            if (pets == null) return;

            if (!pets.TryDraw(out var drawn, out bool duplicate, out bool refunded))
            {
                if (resultLabel != null) resultLabel.text = "다이아가 모자랍니다";
                return;
            }

            if (resultLabel == null) return;
            string rarity = PetTable.Name(drawn.rarity);
            int lv = pets.Level(drawn.id);
            resultLabel.text = refunded
                ? $"[{rarity}] {drawn.name} — 만렙, 다이아 절반 반환"
                : duplicate
                    ? $"[{rarity}] {drawn.name} — 레벨 {lv}"
                    : $"[{rarity}] {drawn.name} 획득! Lv.{lv}";
            resultLabel.color = PetTable.Color(drawn.rarity);
        }

        void Refresh()
        {
            if (pets == null || wallet == null) return;

            if (headline != null) headline.text = $"펫 뽑기 · 다이아 {BigNum.Format(wallet.Gem)}";
            if (costLabel != null) costLabel.text = $"1회 {BigNum.Format(pets.DrawCost)} 다이아";
            if (ratesLabel != null) ratesLabel.text = Rates();
            if (ownedLabel != null) ownedLabel.text = $"보유 {pets.OwnedCount} / {PetTable.Count}";
            if (activeLabel != null) activeLabel.text = ActiveText(pets);

            if (drawButtonImage != null)
                drawButtonImage.color = pets.CanDraw() ? Color.white : TooExpensive;
        }

        static string ActiveText(PetService service)
        {
            if (!service.HasPet) return "함께 다니는 펫이 없습니다";
            int id = service.ActiveId;
            int lv = service.Level(id);
            double atk = (PetTable.ScaledAttackMultiplier(id, lv) - 1.0) * 100;
            double hp = (PetTable.ScaledHealthMultiplier(id, lv) - 1.0) * 100;
            return $"함께 다니는 펫 · {PetTable.Get(id).name} Lv.{lv}\n공격 +{atk:0}% · 체력 +{hp:0}%";
        }

        static string Rates()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < PetTable.RarityCount; i++)
            {
                var rarity = (PetRarity)i;
                sb.AppendLine($"{PetTable.Name(rarity)}  {PetTable.RarityChance(rarity) * 100:0.#}%");
            }
            return sb.ToString();
        }
    }
}
