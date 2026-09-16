using System.Text;
using GrowNa.Core;
using GrowNa.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class SkillGachaPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text headline;
        [SerializeField] Text ratesLabel;
        [SerializeField] Text ownedLabel;
        [SerializeField] Text resultLabel;
        [SerializeField] Text costLabel;
        [SerializeField] Button drawButton;
        [SerializeField] Image drawButtonImage;
        [SerializeField] Button closeButton;

        Wallet wallet;
        SkillService skills;

        static readonly Color Affordable = Color.white;
        static readonly Color TooExpensive = new Color(0.62f, 0.60f, 0.58f);

        public void Bind(GameObject panelRoot, Text title, Text rates, Text owned, Text result,
                         Text cost, Button draw, Image drawImage, Button close)
        {
            root = panelRoot;
            headline = title;
            ratesLabel = rates;
            ownedLabel = owned;
            resultLabel = result;
            costLabel = cost;
            drawButton = draw;
            drawButtonImage = drawImage;
            closeButton = close;
        }

        public void BindServices(Wallet boundWallet, SkillService boundSkills)
        {
            UnbindServices();
            wallet = boundWallet;
            skills = boundSkills;
            if (wallet != null) wallet.Changed += Refresh;
            if (skills != null) skills.Changed += Refresh;
            Refresh();
        }

        void Start()
        {
            if (drawButton != null) drawButton.onClick.AddListener(Draw);
            if (closeButton != null) closeButton.onClick.AddListener(() => SetOpen(false));
            SetOpen(false);
        }

        void OnDestroy() => UnbindServices();

        void UnbindServices()
        {
            if (wallet != null) wallet.Changed -= Refresh;
            if (skills != null) skills.Changed -= Refresh;
        }

        public void Toggle() => SetOpen(root != null && !root.activeSelf);

        public void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void Draw()
        {
            if (skills == null) return;

            if (!skills.TryDraw(out var drawn, out bool duplicate, out bool refunded))
            {
                if (resultLabel != null) resultLabel.text = "다이아가 모자랍니다";
                return;
            }

            if (resultLabel == null) return;
            string rarity = SkillTable.Name(drawn.rarity);
            int lv = skills.Level(drawn.id);
            resultLabel.text = refunded
                ? $"[{rarity}] {drawn.name} — 만렙, 다이아 절반 반환"
                : duplicate
                    ? $"[{rarity}] {drawn.name} — 레벨 {lv}"
                    : $"[{rarity}] {drawn.name} 획득! Lv.{lv}";
            resultLabel.color = SkillTable.Color(drawn.rarity);
        }

        void Refresh()
        {
            if (skills == null || wallet == null) return;

            if (headline != null) headline.text = $"스킬 뽑기 · 다이아 {BigNum.Format(wallet.Gem)}";
            if (costLabel != null) costLabel.text = $"1회 {BigNum.Format(skills.DrawCost)} 다이아";
            if (ratesLabel != null) ratesLabel.text = Rates();
            if (ownedLabel != null) ownedLabel.text = Owned(skills);

            bool affordable = skills.CanDraw();
            if (drawButtonImage != null) drawButtonImage.color = affordable ? Affordable : TooExpensive;
        }

        static string Rates()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < SkillTable.RarityCount; i++)
            {
                var rarity = (SkillRarity)i;
                sb.AppendLine($"{SkillTable.Name(rarity)}  {SkillTable.RarityChance(rarity) * 100:0.#}%");
            }
            return sb.ToString();
        }

        static string Owned(SkillService service)
        {
            int count = 0;
            for (int i = 0; i < SkillTable.Count; i++) if (service.Owns(i)) count++;
            return $"보유 {count} / {SkillTable.Count}";
        }
    }
}
