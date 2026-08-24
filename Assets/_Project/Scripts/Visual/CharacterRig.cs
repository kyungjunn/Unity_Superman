using System;
using GrowNa.Gear;
using UnityEngine;

namespace GrowNa.Visual
{
    public class CharacterRig : MonoBehaviour
    {
        public const int BodySortingOrder = 10;

        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer[] layers = new SpriteRenderer[GearTable.SlotCount];
        [SerializeField] Sprite[] slotSprites = new Sprite[GearTable.SlotCount];

        static readonly int[] SortingOffsets = { 7, 4, 1, 3, -1, 2, 5, 6 };

        public static int SortingOrderFor(GearSlot slot) => BodySortingOrder + SortingOffsets[(int)slot];

        public SpriteRenderer Body => body;
        public SpriteRenderer LayerOf(GearSlot slot) => layers[(int)slot];
        public Sprite SpriteOf(GearSlot slot) => slotSprites[(int)slot];

        public void Bind(SpriteRenderer bodyRenderer, SpriteRenderer[] slotLayers, Sprite[] sprites)
        {
            body = bodyRenderer;
            layers = slotLayers;
            slotSprites = sprites;
        }

        void Start()
        {
            var loadout = Loadout.Instance;
            if (loadout == null) return;
            loadout.SlotChanged += Apply;
            for (int i = 0; i < GearTable.SlotCount; i++)
                Apply((GearSlot)i, loadout.Get((GearSlot)i));
        }

        void OnDestroy()
        {
            if (Loadout.Instance != null) Loadout.Instance.SlotChanged -= Apply;
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
            renderer.sprite = slotSprites[(int)slot];
            renderer.color = GearTable.Color(item.tier);
            renderer.sortingOrder = SortingOrderFor(slot);
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
