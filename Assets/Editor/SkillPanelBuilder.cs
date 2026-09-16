using GrowNa.Pet;
using GrowNa.Skill;
using GrowNa.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.EditorTools
{
    public static class SkillPanelBuilder
    {
        const float BarBottom = 812f;
        const float ChestBottom = 986f;
        const float Cell = 130f;
        const float Gap = 12f;

        static readonly Color CooldownVeil = new Color(0.05f, 0.04f, 0.08f, 0.78f);

        public static SkillBarView.Slot[] BuildSkillBar(Transform root, out Button skillGachaButton, out Button petGachaButton)
        {
            var panel = UiFactory.Sprite("SkillBar", root, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                             new Vector2(0f, BarBottom), new Vector2(880f, 158f));

            var slots = new SkillBarView.Slot[SkillTable.EquipSlots];
            float startX = -((SkillTable.EquipSlots - 1) * (Cell + Gap)) * 0.5f;

            for (int i = 0; i < SkillTable.EquipSlots; i++)
            {
                var go = UiFactory.Panel($"SkillSlot_{i}", panel.transform,
                                         MainSceneBuilder.Load("ui_slot.png"), Color.white, true);
                UiFactory.Anchor(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                 new Vector2(startX + i * (Cell + Gap), 0f), new Vector2(Cell, Cell));

                // 쿨타임 베일은 슬롯 위를 덮는다. Radial360 이라 시계 반대로 걷히면서 남은 시간이 보인다.
                var veil = UiFactory.Sprite("Cooldown", go.transform, MainSceneBuilder.Load("ui_slot.png"), CooldownVeil);
                UiFactory.Place(veil.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                veil.type = Image.Type.Filled;
                veil.fillMethod = Image.FillMethod.Radial360;
                veil.fillOrigin = (int)Image.Origin360.Top;
                veil.fillClockwise = false;
                veil.fillAmount = 0f;

                var nameLabel = UiFactory.Label("Name", go.transform, "빈 칸", 19,
                                                new Color(0.62f, 0.58f, 0.70f), TextAnchor.LowerCenter);
                UiFactory.Place(nameLabel.gameObject, Vector2.zero, Vector2.one, new Vector2(4f, 6f), new Vector2(-4f, -8f));

                var cooldownLabel = UiFactory.Label("Timer", go.transform, "", 30,
                                                    PanelBuilder.Cream, TextAnchor.MiddleCenter);
                UiFactory.Place(cooldownLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                UiFactory.Shadow(cooldownLabel.gameObject);

                slots[i] = new SkillBarView.Slot
                {
                    frame = go.GetComponent<Image>(),
                    cooldownFill = veil,
                    nameLabel = nameLabel,
                    cooldownLabel = cooldownLabel,
                    button = go.GetComponent<Button>(),
                };
            }

            // 뽑기 버튼 두 개는 스킬 바 오른쪽에 세로로 쌓는다. 스킬이 위, 펫이 그 아래.
            skillGachaButton = GachaButton(root, "SkillGachaButton", "스킬\n뽑기", BarBottom + 266f);
            petGachaButton = GachaButton(root, "PetGachaButton", "펫\n뽑기", BarBottom + 170f);
            return slots;
        }

        static Button GachaButton(Transform root, string name, string label, float y)
        {
            var go = UiFactory.Panel(name, root, MainSceneBuilder.Load("ui_frame_light.png"), Color.white, true);
            UiFactory.Anchor(go, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(390f, y), new Vector2(150f, 88f));
            var text = UiFactory.Label("Label", go.transform, label, 24, PanelBuilder.Ink, TextAnchor.MiddleCenter);
            UiFactory.Place(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return go.GetComponent<Button>();
        }

        public static SkillGachaPanel BuildGachaPanel(Transform root)
        {
            var panel = UiFactory.Sprite("SkillGachaPanel", root, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            panel.raycastTarget = true;
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(0f, -20f), new Vector2(920f, 1000f));

            var headline = UiFactory.Label("Headline", panel.transform, "스킬 뽑기", 44, PanelBuilder.Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(headline.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(760f, 60f));
            UiFactory.Shadow(headline.gameObject);

            var rates = UiFactory.Label("Rates", panel.transform, "", 30, PanelBuilder.Cream, TextAnchor.UpperLeft);
            UiFactory.Anchor(rates.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(700f, 220f));

            var owned = UiFactory.Label("Owned", panel.transform, "", 28, PanelBuilder.Gold, TextAnchor.MiddleCenter);
            UiFactory.Anchor(owned.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -380f), new Vector2(700f, 44f));

            var result = UiFactory.Label("Result", panel.transform, "", 30, PanelBuilder.Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(result.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -450f), new Vector2(860f, 52f));
            UiFactory.Shadow(result.gameObject);

            var cost = UiFactory.Label("Cost", panel.transform, "", 26, PanelBuilder.Dim, TextAnchor.MiddleCenter);
            UiFactory.Anchor(cost.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(700f, 40f));

            var drawGo = UiFactory.Panel("Draw", panel.transform, MainSceneBuilder.Load("ui_frame_light.png"), Color.white, true);
            UiFactory.Anchor(drawGo, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(360f, 96f));
            var drawLabel = UiFactory.Label("Label", drawGo.transform, "뽑기", 32, PanelBuilder.Ink, TextAnchor.MiddleCenter);
            UiFactory.Place(drawLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var component = panel.gameObject.AddComponent<SkillGachaPanel>();
            component.Bind(panel.gameObject, headline, rates, owned, result, cost,
                           drawGo.GetComponent<Button>(), drawGo.GetComponent<Image>(),
                           PanelBuilder.BuildCloseButton(panel.transform));
            return component;
        }

        public static SkillEquipPanel BuildSkillEquipPanel(Transform root, Button opener)
        {
            var panel = UiFactory.Sprite("SkillEquipPanel", root, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            panel.raycastTarget = true;
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(0f, -20f), new Vector2(980f, 1380f));

            var title = UiFactory.Label("Title", panel.transform, "스킬 장착", 44, PanelBuilder.Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(700f, 60f));
            UiFactory.Shadow(title.gameObject);

            var slots = new SkillEquipPanel.SlotButton[SkillTable.EquipSlots];
            const float slotCell = 142f;
            const float slotGap = 12f;
            float slotStartX = -((SkillTable.EquipSlots - 1) * (slotCell + slotGap)) * 0.5f;

            for (int i = 0; i < SkillTable.EquipSlots; i++)
            {
                var go = UiFactory.Panel($"Equip_{i}", panel.transform,
                                         MainSceneBuilder.Load("ui_slot.png"), Color.white, true);
                UiFactory.Anchor(go, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                 new Vector2(slotStartX + i * (slotCell + slotGap), -130f),
                                 new Vector2(slotCell, slotCell));

                var index = UiFactory.Label("Index", go.transform, $"{i + 1}", 22,
                                            new Color(0.52f, 0.48f, 0.58f), TextAnchor.UpperCenter);
                UiFactory.Place(index.gameObject, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -6f));

                var label = UiFactory.Label("Name", go.transform, "빈 칸", 20,
                                            new Color(0.62f, 0.58f, 0.70f), TextAnchor.MiddleCenter);
                UiFactory.Place(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                slots[i] = new SkillEquipPanel.SlotButton
                {
                    frame = go.GetComponent<Image>(),
                    label = label,
                    button = go.GetComponent<Button>(),
                };
            }

            var hint = UiFactory.Label("Hint", panel.transform, "", 26, PanelBuilder.Gold, TextAnchor.MiddleCenter);
            UiFactory.Anchor(hint.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -292f), new Vector2(900f, 44f));

            var entries = new SkillEquipPanel.SkillButton[SkillTable.Count];
            const int columns = 3;
            const float cardW = 296f;
            const float cardH = 132f;
            const float cardGap = 12f;
            float cardStartX = -((columns - 1) * (cardW + cardGap)) * 0.5f;

            for (int id = 0; id < SkillTable.Count; id++)
            {
                int row = id / columns;
                int col = id % columns;
                var go = UiFactory.Panel($"Skill_{id}", panel.transform,
                                         MainSceneBuilder.Load("ui_frame_light.png"), Color.white, true);
                UiFactory.Anchor(go, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                 new Vector2(cardStartX + col * (cardW + cardGap), -360f - row * (cardH + cardGap)),
                                 new Vector2(cardW, cardH));

                var label = UiFactory.Label("Name", go.transform, SkillTable.Get(id).name, 24,
                                            PanelBuilder.Ink, TextAnchor.MiddleCenter);
                UiFactory.Anchor(label.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(cardW - 16f, 44f));

                var detail = UiFactory.Label("Detail", go.transform, "", 20,
                                             new Color(0.42f, 0.34f, 0.22f), TextAnchor.MiddleCenter);
                UiFactory.Anchor(detail.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(cardW - 16f, 52f));

                entries[id] = new SkillEquipPanel.SkillButton
                {
                    frame = go.GetComponent<Image>(),
                    label = label,
                    detail = detail,
                    button = go.GetComponent<Button>(),
                };
            }

            Button auto = ChestButton(panel.transform, "AutoEquip", "희귀도 순 자동 장착", new Vector2(-190f, 90f));
            Button clear = ChestButton(panel.transform, "ClearSlot", "선택 칸 비우기", new Vector2(190f, 90f));

            var component = panel.gameObject.AddComponent<SkillEquipPanel>();
            component.Bind(panel.gameObject, slots, entries, hint, auto, clear,
                           opener, PanelBuilder.BuildCloseButton(panel.transform));
            return component;
        }

        public static PetEquipPanel BuildPetEquipPanel(Transform root, Button opener)
        {
            var panel = UiFactory.Sprite("PetEquipPanel", root, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            panel.raycastTarget = true;
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(0f, -20f), new Vector2(980f, 1200f));

            var title = UiFactory.Label("Title", panel.transform, "펫 장착", 44, PanelBuilder.Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(700f, 60f));
            UiFactory.Shadow(title.gameObject);

            var active = UiFactory.Label("Active", panel.transform, "", 28, PanelBuilder.Gold, TextAnchor.UpperCenter);
            UiFactory.Anchor(active.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(860f, 90f));

            var hint = UiFactory.Label("Hint", panel.transform, "", 26, PanelBuilder.Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(hint.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -206f), new Vector2(900f, 44f));

            var entries = new PetEquipPanel.PetButton[PetTable.Count];
            const int columns = 2;
            const float cardW = 440f;
            const float cardH = 136f;
            const float cardGap = 14f;
            float cardStartX = -((columns - 1) * (cardW + cardGap)) * 0.5f;

            for (int id = 0; id < PetTable.Count; id++)
            {
                int row = id / columns;
                int col = id % columns;
                var go = UiFactory.Panel($"Pet_{id}", panel.transform,
                                         MainSceneBuilder.Load("ui_frame_light.png"), Color.white, true);
                UiFactory.Anchor(go, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                 new Vector2(cardStartX + col * (cardW + cardGap), -272f - row * (cardH + cardGap)),
                                 new Vector2(cardW, cardH));

                var label = UiFactory.Label("Name", go.transform, PetTable.Get(id).name, 28,
                                            PanelBuilder.Ink, TextAnchor.MiddleCenter);
                UiFactory.Anchor(label.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(cardW - 20f, 46f));

                var detail = UiFactory.Label("Detail", go.transform, "", 22,
                                             new Color(0.42f, 0.34f, 0.22f), TextAnchor.MiddleCenter);
                UiFactory.Anchor(detail.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(cardW - 20f, 52f));

                entries[id] = new PetEquipPanel.PetButton
                {
                    frame = go.GetComponent<Image>(),
                    label = label,
                    detail = detail,
                    button = go.GetComponent<Button>(),
                };
            }

            Button best = ChestButton(panel.transform, "EquipBest", "가장 희귀한 펫 장착", new Vector2(-190f, 90f));
            Button unequip = ChestButton(panel.transform, "Unequip", "펫 해제", new Vector2(190f, 90f));

            var component = panel.gameObject.AddComponent<PetEquipPanel>();
            component.Bind(panel.gameObject, entries, active, hint, best, unequip,
                           opener, PanelBuilder.BuildCloseButton(panel.transform));
            return component;
        }

        public static PetGachaPanel BuildPetGachaPanel(Transform root, Button opener)
        {
            var panel = UiFactory.Sprite("PetGachaPanel", root, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            panel.raycastTarget = true;
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(0f, -20f), new Vector2(920f, 1060f));

            var headline = UiFactory.Label("Headline", panel.transform, "펫 뽑기", 44, PanelBuilder.Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(headline.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(760f, 60f));
            UiFactory.Shadow(headline.gameObject);

            var portrait = UiFactory.Sprite("PetPortrait", panel.transform, MainSceneBuilder.Load("pet_sparrow.png"), Color.white);
            UiFactory.Anchor(portrait.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(180f, 180f));

            var active = UiFactory.Label("Active", panel.transform, "", 28, PanelBuilder.Gold, TextAnchor.UpperCenter);
            UiFactory.Anchor(active.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -316f), new Vector2(800f, 90f));

            var rates = UiFactory.Label("Rates", panel.transform, "", 30, PanelBuilder.Cream, TextAnchor.UpperLeft);
            UiFactory.Anchor(rates.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -420f), new Vector2(700f, 220f));

            var owned = UiFactory.Label("Owned", panel.transform, "", 28, PanelBuilder.Gold, TextAnchor.MiddleCenter);
            UiFactory.Anchor(owned.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -652f), new Vector2(700f, 44f));

            var result = UiFactory.Label("Result", panel.transform, "", 30, PanelBuilder.Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(result.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -716f), new Vector2(860f, 52f));
            UiFactory.Shadow(result.gameObject);

            var cost = UiFactory.Label("Cost", panel.transform, "", 26, PanelBuilder.Dim, TextAnchor.MiddleCenter);
            UiFactory.Anchor(cost.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(700f, 40f));

            var drawGo = UiFactory.Panel("Draw", panel.transform, MainSceneBuilder.Load("ui_frame_light.png"), Color.white, true);
            UiFactory.Anchor(drawGo, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(360f, 96f));
            var drawLabel = UiFactory.Label("Label", drawGo.transform, "뽑기", 32, PanelBuilder.Ink, TextAnchor.MiddleCenter);
            UiFactory.Place(drawLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var component = panel.gameObject.AddComponent<PetGachaPanel>();
            component.Bind(panel.gameObject, headline, rates, active, owned, result, cost,
                           drawGo.GetComponent<Button>(), drawGo.GetComponent<Image>(),
                           opener, PanelBuilder.BuildCloseButton(panel.transform));
            return component;
        }

        public static LevelUpPopup BuildLevelUpPopup(Transform root)
        {
            var panel = UiFactory.Sprite("LevelUpPopup", root, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            panel.raycastTarget = true;
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(0f, 120f), new Vector2(760f, 520f));

            var title = UiFactory.Label("Title", panel.transform, "레벨 업!", 52, PanelBuilder.Gold, TextAnchor.MiddleCenter);
            UiFactory.Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(700f, 70f));
            UiFactory.Shadow(title.gameObject);

            var level = UiFactory.Label("Level", panel.transform, "Lv.1", 76, PanelBuilder.Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(level.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(700f, 100f));
            UiFactory.Shadow(level.gameObject);

            var detail = UiFactory.Label("Detail", panel.transform, "", 30, PanelBuilder.Cream, TextAnchor.UpperCenter);
            UiFactory.Anchor(detail.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(700f, 180f));

            var component = panel.gameObject.AddComponent<LevelUpPopup>();
            component.Bind(panel.gameObject, level, detail, PanelBuilder.BuildCloseButton(panel.transform));
            return component;
        }

        public static IdleChestPanel BuildIdleChest(Transform hudRoot, Transform popupRoot)
        {
            var chest = UiFactory.Panel("IdleChestButton", hudRoot, MainSceneBuilder.Load("ui_btn_round.png"), Color.white, true);
            UiFactory.Anchor(chest, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                             new Vector2(0f, ChestBottom), new Vector2(180f, 180f));
            var chestLabel = UiFactory.Label("Label", chest.transform, "보물\n상자", 30, PanelBuilder.Ink, TextAnchor.MiddleCenter);
            UiFactory.Place(chestLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            chest.SetActive(false);

            var panel = UiFactory.Sprite("IdleChestPanel", popupRoot, MainSceneBuilder.Load("ui_frame_dark.png"), Color.white);
            panel.raycastTarget = true;
            UiFactory.Anchor(panel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                             new Vector2(0f, -20f), new Vector2(900f, 780f));

            var title = UiFactory.Label("Title", panel.transform, "방치 보상", 44, PanelBuilder.Cream, TextAnchor.MiddleCenter);
            UiFactory.Anchor(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(700f, 60f));
            UiFactory.Shadow(title.gameObject);

            var duration = UiFactory.Label("Duration", panel.transform, "", 34, PanelBuilder.Gold, TextAnchor.MiddleCenter);
            UiFactory.Anchor(duration.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(700f, 52f));

            Text gold = RewardRow(panel.transform, "Gold", "ic_gold.png", -200f);
            Text grain = RewardRow(panel.transform, "Grain", "ic_grain.png", -310f);

            var notice = UiFactory.Label("Notice", panel.transform, "", 26, PanelBuilder.Dim, TextAnchor.MiddleCenter);
            UiFactory.Anchor(notice.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -430f), new Vector2(820f, 44f));

            Button claim = ChestButton(panel.transform, "Claim", "수령", new Vector2(-200f, 90f));
            Button adClaim = ChestButton(panel.transform, "AdClaim", "광고 보고 2배", new Vector2(200f, 90f));

            var component = panel.gameObject.AddComponent<IdleChestPanel>();
            component.Bind(chest, chest.GetComponent<Button>(), panel.gameObject, duration, gold, grain,
                           notice, claim, adClaim, PanelBuilder.BuildCloseButton(panel.transform));
            return component;
        }

        static Text RewardRow(Transform parent, string name, string icon, float y)
        {
            var row = UiFactory.Sprite($"Row_{name}", parent, MainSceneBuilder.Load("ui_frame_light.png"), Color.white);
            UiFactory.Anchor(row.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(760f, 96f));

            var iconImage = UiFactory.Sprite("Icon", row.transform, MainSceneBuilder.Load(icon), Color.white);
            UiFactory.Anchor(iconImage.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(60f, 60f));

            var value = UiFactory.Label("Value", row.transform, "0", 34, PanelBuilder.Ink, TextAnchor.MiddleRight);
            UiFactory.Place(value.gameObject, Vector2.zero, Vector2.one, new Vector2(90f, 0f), new Vector2(-24f, 0f));
            return value;
        }

        static Button ChestButton(Transform parent, string name, string label, Vector2 position)
        {
            var go = UiFactory.Panel(name, parent, MainSceneBuilder.Load("ui_btn_round.png"), Color.white, true);
            UiFactory.Anchor(go, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, new Vector2(340f, 100f));
            var text = UiFactory.Label("Label", go.transform, label, 28, PanelBuilder.Ink, TextAnchor.MiddleCenter);
            UiFactory.Place(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return go.GetComponent<Button>();
        }
    }
}
