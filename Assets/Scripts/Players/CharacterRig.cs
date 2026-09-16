using System;
using GrowNa.Gear;
using UnityEngine;

namespace GrowNa.Players
{
    public class CharacterRig : MonoBehaviour
    {
        [Serializable]
        public struct AnimatedSlot
        {
            public Sprite[] idle;
            public Sprite[] run;
            public Sprite[] attack;
        }

        public const int BodySortingOrder = 10;

        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer[] layers = new SpriteRenderer[GearTable.SlotCount];
        [SerializeField] Sprite[] slotSprites = new Sprite[GearTable.SlotCount];
        [SerializeField] AnimatedSlot[] animatedSlots = new AnimatedSlot[GearTable.SlotCount];

        Loadout loadout;
        PlayerAnimationClip animationClip;
        int animationFrame;

        static readonly int[] SortingOffsets = { 7, 4, 1, 3, -1, 2, 5, 6 };

        public static int SortingOrderFor(GearSlot slot) => BodySortingOrder + SortingOffsets[(int)slot];

        public SpriteRenderer Body => body;
        public SpriteRenderer LayerOf(GearSlot slot) => layers[(int)slot];
        public Sprite SpriteOf(GearSlot slot) => slotSprites[(int)slot];

        public void Bind(SpriteRenderer bodyRenderer, SpriteRenderer[] slotLayers, Sprite[] sprites,
                         AnimatedSlot[] animations)
        {
            body = bodyRenderer;
            layers = slotLayers;
            slotSprites = sprites;
            animatedSlots = animations;
        }

        public void BindLoadout(Loadout bound)
        {
            if (loadout != null) loadout.SlotChanged -= Apply;
            loadout = bound;
            if (loadout == null) return;
            loadout.SlotChanged += Apply;
            for (int i = 0; i < GearTable.SlotCount; i++)
                Apply((GearSlot)i, loadout.Get((GearSlot)i));
        }

        void OnDestroy()
        {
            if (loadout != null) loadout.SlotChanged -= Apply;
        }

        public void Apply(GearSlot slot, GearItem item)
        {
            var renderer = layers[(int)slot];
            if (renderer == null) return;

            if (!item.owned)
            {
                renderer.enabled = false;
                return;
            }

            renderer.enabled = true;
            renderer.sprite = SpriteFor(slot, animationClip, animationFrame) ?? slotSprites[(int)slot];
            renderer.color = GearTable.Color(item.tier);
            renderer.sortingOrder = SortingOrderFor(slot);
            FitToBody(renderer, body);
        }

        public void SetAnimationFrame(PlayerAnimationClip clip, int frame)
        {
            animationClip = clip;
            animationFrame = frame;
            for (int i = 0; i < layers.Length; i++)
            {
                var renderer = layers[i];
                if (renderer == null || !renderer.enabled) continue;
                var sprite = SpriteFor((GearSlot)i, clip, frame);
                if (sprite == null) continue;
                renderer.sprite = sprite;
                FitToBody(renderer, body);
            }
        }

        Sprite SpriteFor(GearSlot slot, PlayerAnimationClip clip, int frame)
        {
            int index = (int)slot;
            if (animatedSlots == null || index < 0 || index >= animatedSlots.Length) return null;
            Sprite[] sprites = clip switch
            {
                PlayerAnimationClip.Run => animatedSlots[index].run,
                PlayerAnimationClip.Attack => animatedSlots[index].attack,
                _ => animatedSlots[index].idle,
            };
            return sprites != null && sprites.Length > 0 ? sprites[frame % sprites.Length] : null;
        }

        public static void FitToBody(SpriteRenderer layer, SpriteRenderer bodyRenderer)
        {
            if (layer == null || bodyRenderer == null || layer.sprite == null || bodyRenderer.sprite == null)
                return;
            float bodyH = bodyRenderer.sprite.rect.height / bodyRenderer.sprite.pixelsPerUnit;
            float gearH = layer.sprite.rect.height / layer.sprite.pixelsPerUnit;
            if (gearH < 0.0001f) return;
            float s = bodyH / gearH;
            layer.transform.localScale = new Vector3(s, s, 1f);
        }

        public string AuditReport()
        {
            var report = new System.Text.StringBuilder();
            Vector2 bodySize = body != null && body.sprite != null ? body.sprite.rect.size : Vector2.zero;
            Vector2 bodyPivot = body != null && body.sprite != null
                ? body.sprite.pivot / Mathf.Max(1f, body.sprite.rect.width)
                : Vector2.zero;

            for (int i = 0; i < GearTable.SlotCount; i++)
            {
                var slot = (GearSlot)i;
                var sprite = slotSprites[i];
                if (sprite == null)
                {
                    report.AppendLine($"{GearTable.Name(slot)}: 스프라이트 없음");
                    continue;
                }
                Vector2 size = sprite.rect.size;
                Vector2 pivot = sprite.pivot / Mathf.Max(1f, sprite.rect.width);
                bool sizeOk = Mathf.Approximately(size.x, bodySize.x) && Mathf.Approximately(size.y, bodySize.y);
                bool pivotOk = (pivot - bodyPivot).sqrMagnitude < 0.0001f;
                bool ppuOk = body != null && body.sprite != null
                    && Mathf.Approximately(sprite.pixelsPerUnit, body.sprite.pixelsPerUnit);
                report.AppendLine(
                    $"{GearTable.Name(slot)}: size={size.x}x{size.y} {(sizeOk ? "OK" : "MISMATCH")}, " +
                    $"pivot={pivot} {(pivotOk ? "OK" : "MISMATCH")}, ppu={(ppuOk ? "OK" : "MISMATCH")}, " +
                    $"order={SortingOrderFor(slot)}");
            }
            return report.ToString();
        }
    }
}
