using System;
using GrowNa.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class SkillBarView : MonoBehaviour
    {
        [Serializable]
        public struct Slot
        {
            public Image frame;
            public Image cooldownFill;
            public Text nameLabel;
            public Text cooldownLabel;
            public Button button;
        }

        static readonly Color EmptySlot = new Color(0.45f, 0.42f, 0.40f);

        [SerializeField] Slot[] slots = new Slot[SkillTable.EquipSlots];
        [SerializeField] Button gachaButton;
        [SerializeField] SkillGachaPanel gachaPanel;

        SkillService skills;

        public void Bind(Slot[] boundSlots, Button opener, SkillGachaPanel panel)
        {
            slots = boundSlots;
            gachaButton = opener;
            gachaPanel = panel;
        }

        public void BindServices(SkillService bound)
        {
            if (skills != null) skills.Changed -= RefreshSlots;
            skills = bound;
            if (skills != null) skills.Changed += RefreshSlots;
            RefreshSlots();
        }

        void Start()
        {
            if (gachaButton != null && gachaPanel != null)
                gachaButton.onClick.AddListener(gachaPanel.Toggle);
        }

        void OnDestroy()
        {
            if (skills != null) skills.Changed -= RefreshSlots;
        }

        void RefreshSlots()
        {
            if (skills == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                bool has = skills.HasSkill(i);
                var slot = slots[i];

                if (slot.nameLabel != null)
                {
                    slot.nameLabel.text = has ? SkillTable.Get(skills.EquippedId(i)).name : "빈 칸";
                    slot.nameLabel.color = has
                        ? SkillTable.Color(SkillTable.Get(skills.EquippedId(i)).rarity)
                        : EmptySlot;
                }

                if (slot.frame != null)
                    slot.frame.color = has
                        ? SkillTable.Color(SkillTable.Get(skills.EquippedId(i)).rarity)
                        : EmptySlot;
            }
        }

        void Update()
        {
            if (skills == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                float ratio = skills.CooldownRatio(i);
                float remaining = skills.Cooldown(i);

                if (slot.cooldownFill != null)
                {
                    slot.cooldownFill.gameObject.SetActive(ratio > 0f);
                    slot.cooldownFill.fillAmount = ratio;
                }

                if (slot.cooldownLabel != null)
                    slot.cooldownLabel.text = remaining > 0f ? $"{remaining:0.0}" : "";
            }
        }
    }
}
