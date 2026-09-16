using System;
using GrowNa.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class SkillEquipPanel : MonoBehaviour
    {
        [Serializable]
        public struct SlotButton
        {
            public Image frame;
            public Text label;
            public Button button;
        }

        [Serializable]
        public struct SkillButton
        {
            public Image frame;
            public Text label;
            public Text detail;
            public Button button;
        }

        static readonly Color Selected = new Color(1f, 0.86f, 0.42f);
        static readonly Color Idle = new Color(0.62f, 0.58f, 0.70f);
        static readonly Color Locked = new Color(0.38f, 0.36f, 0.40f);

        [SerializeField] GameObject root;
        [SerializeField] SlotButton[] slots = new SlotButton[SkillTable.EquipSlots];
        [SerializeField] SkillButton[] entries = new SkillButton[SkillTable.Count];
        [SerializeField] Text hintLabel;
        [SerializeField] Button autoButton;
        [SerializeField] Button clearButton;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;

        SkillService skills;
        int selectedSlot;

        public void Bind(GameObject panelRoot, SlotButton[] boundSlots, SkillButton[] boundEntries,
                         Text hint, Button auto, Button clear, Button open, Button close)
        {
            root = panelRoot;
            slots = boundSlots;
            entries = boundEntries;
            hintLabel = hint;
            autoButton = auto;
            clearButton = clear;
            openButton = open;
            closeButton = close;
        }

        public void BindServices(SkillService bound)
        {
            if (skills != null) skills.Changed -= Refresh;
            skills = bound;
            if (skills != null) skills.Changed += Refresh;
            Refresh();
        }

        void Start()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int slot = i;
                if (slots[i].button != null) slots[i].button.onClick.AddListener(() => SelectSlot(slot));
            }

            for (int i = 0; i < entries.Length; i++)
            {
                int id = i;
                if (entries[i].button != null) entries[i].button.onClick.AddListener(() => Equip(id));
            }

            if (autoButton != null) autoButton.onClick.AddListener(AutoEquip);
            if (clearButton != null) clearButton.onClick.AddListener(ClearSelected);
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (closeButton != null) closeButton.onClick.AddListener(() => SetOpen(false));
            SetOpen(false);
        }

        void OnDestroy()
        {
            if (skills != null) skills.Changed -= Refresh;
        }

        public void Toggle() => SetOpen(root != null && !root.activeSelf);

        public void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (open) Refresh();
        }

        void SelectSlot(int slot)
        {
            selectedSlot = slot;
            if (hintLabel != null) hintLabel.text = string.Empty;
            Refresh();
        }

        void Equip(int id)
        {
            if (skills == null) return;

            if (!skills.Owns(id))
            {
                if (hintLabel != null) hintLabel.text = "아직 뽑지 않은 스킬입니다";
                return;
            }

            skills.TryEquip(selectedSlot, id);
            Refresh();
        }

        void AutoEquip()
        {
            if (skills == null) return;

            bool changed = skills.AutoEquipBest();
            if (hintLabel != null)
                hintLabel.text = changed ? "희귀도 순으로 장착했습니다" : "이미 최적 구성입니다";
            Refresh();
        }

        void ClearSelected()
        {
            skills?.TryClear(selectedSlot);
            Refresh();
        }

        void Refresh()
        {
            if (skills == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                bool has = skills.HasSkill(i);
                var def = has ? SkillTable.Get(skills.EquippedId(i)) : default;

                if (slots[i].label != null)
                {
                    slots[i].label.text = has
                        ? $"{def.name} Lv.{skills.Level(skills.EquippedId(i))}"
                        : "빈 칸";
                    slots[i].label.color = has ? SkillTable.Color(def.rarity) : Idle;
                }

                if (slots[i].frame != null)
                    slots[i].frame.color = i == selectedSlot ? Selected : Color.white;
            }

            for (int id = 0; id < entries.Length; id++)
            {
                var def = SkillTable.Get(id);
                bool ownsIt = skills.Owns(id);

                if (entries[id].label != null)
                {
                    entries[id].label.text = def.name;
                    entries[id].label.color = ownsIt ? SkillTable.Color(def.rarity) : Locked;
                }

                if (entries[id].detail != null)
                {
                    int lv = skills.Level(id);
                    entries[id].detail.text = ownsIt
                        ? $"Lv.{lv} · {SkillTable.Name(def.rarity)} · x{SkillTable.ScaledDamage(id, lv):0.0} / {def.cooldown:0}초"
                        : "미보유";
                }

                if (entries[id].frame != null)
                    entries[id].frame.color = ownsIt ? Color.white : new Color(0.55f, 0.53f, 0.56f);
            }

            if (hintLabel != null && string.IsNullOrEmpty(hintLabel.text))
                hintLabel.text = $"{selectedSlot + 1}번 칸에 넣을 스킬을 고르세요";
        }
    }
}
