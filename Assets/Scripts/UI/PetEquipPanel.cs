using System;
using GrowNa.Pet;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class PetEquipPanel : MonoBehaviour
    {
        [Serializable]
        public struct PetButton
        {
            public Image frame;
            public Text label;
            public Text detail;
            public Button button;
        }

        static readonly Color Locked = new Color(0.38f, 0.36f, 0.40f);
        static readonly Color Equipped = new Color(1f, 0.86f, 0.42f);

        [SerializeField] GameObject root;
        [SerializeField] PetButton[] entries = new PetButton[PetTable.Count];
        [SerializeField] Text activeLabel;
        [SerializeField] Text hintLabel;
        [SerializeField] Button bestButton;
        [SerializeField] Button unequipButton;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;

        PetService pets;

        public void Bind(GameObject panelRoot, PetButton[] boundEntries, Text active, Text hint,
                         Button best, Button unequip, Button open, Button close)
        {
            root = panelRoot;
            entries = boundEntries;
            activeLabel = active;
            hintLabel = hint;
            bestButton = best;
            unequipButton = unequip;
            openButton = open;
            closeButton = close;
        }

        public void BindServices(PetService bound)
        {
            if (pets != null) pets.Changed -= Refresh;
            pets = bound;
            if (pets != null) pets.Changed += Refresh;
            Refresh();
        }

        void Start()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                int id = i;
                if (entries[i].button != null) entries[i].button.onClick.AddListener(() => Equip(id));
            }

            if (bestButton != null) bestButton.onClick.AddListener(EquipBest);
            if (unequipButton != null) unequipButton.onClick.AddListener(Unequip);
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (closeButton != null) closeButton.onClick.AddListener(() => SetOpen(false));
            SetOpen(false);
        }

        void OnDestroy()
        {
            if (pets != null) pets.Changed -= Refresh;
        }

        public void Toggle() => SetOpen(root != null && !root.activeSelf);

        public void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void Equip(int id)
        {
            if (pets == null) return;

            if (!pets.Owns(id))
            {
                Hint("아직 뽑지 않은 펫입니다");
                return;
            }

            Hint(pets.TryEquip(id) ? $"{PetTable.Get(id).name} 와 함께 다닙니다" : "이미 데리고 있습니다");
            Refresh();
        }

        void EquipBest()
        {
            if (pets == null) return;

            if (pets.OwnedCount <= 0) Hint("보유한 펫이 없습니다");
            else Hint(pets.EquipBest() ? "가장 희귀한 펫을 장착했습니다" : "이미 최고 등급입니다");
            Refresh();
        }

        void Unequip()
        {
            if (pets == null) return;
            Hint(pets.Unequip() ? "펫을 돌려보냈습니다" : "장착한 펫이 없습니다");
            Refresh();
        }

        void Hint(string message)
        {
            if (hintLabel != null) hintLabel.text = message;
        }

        void Refresh()
        {
            if (pets == null) return;

            int active = pets.ActiveId;

            if (activeLabel != null)
                activeLabel.text = pets.HasPet
                    ? Describe(active, pets.Level(active))
                    : "함께 다니는 펫이 없습니다";

            for (int id = 0; id < entries.Length; id++)
            {
                var def = PetTable.Get(id);
                bool ownsIt = pets.Owns(id);
                bool isActive = id == active;

                int lv = pets.Level(id);
                if (entries[id].label != null)
                {
                    string title = ownsIt ? $"{def.name} Lv.{lv}" : def.name;
                    entries[id].label.text = isActive ? $"{title} ★" : title;
                    entries[id].label.color = ownsIt ? PetTable.Color(def.rarity) : Locked;
                }

                if (entries[id].detail != null)
                    entries[id].detail.text = ownsIt
                        ? $"{PetTable.Name(def.rarity)} · 공 +{(PetTable.ScaledAttackMultiplier(id, lv) - 1.0) * 100:0}% 체 +{(PetTable.ScaledHealthMultiplier(id, lv) - 1.0) * 100:0}%"
                        : "미보유";

                if (entries[id].frame != null)
                    entries[id].frame.color = isActive ? Equipped
                                            : ownsIt ? Color.white
                                                     : new Color(0.55f, 0.53f, 0.56f);
            }
        }

        static string Describe(int id, int level)
        {
            var def = PetTable.Get(id);
            double atk = (PetTable.ScaledAttackMultiplier(id, level) - 1.0) * 100;
            double hp = (PetTable.ScaledHealthMultiplier(id, level) - 1.0) * 100;
            return $"함께 다니는 펫 · {def.name} Lv.{level}\n공격 +{atk:0}% · 체력 +{hp:0}%";
        }
    }
}
