using GrowNa.Gear;
using GrowNa.Players;
using UnityEditor;
using UnityEngine;

namespace GrowNa.EditorTools
{
    public static class GearLayerAudit
    {
        [MenuItem("GrowNa/Verify Gear Layer Composition")]
        public static void Verify()
        {
            var rig = Object.FindFirstObjectByType<CharacterRig>();
            if (rig == null)
            {
                Debug.LogError("[GrowNa] CharacterRig 가 씬에 없다. GrowNa/Build Main Scene 먼저 실행할 것.");
                return;
            }

            Debug.Log($"[GrowNa] 레이어 정렬 감사\n{rig.AuditReport()}");

            int missing = 0;
            for (int i = 0; i < GearTable.SlotCount; i++)
            {
                var slot = (GearSlot)i;
                if (rig.SpriteOf(slot) == null || rig.LayerOf(slot) == null) missing++;
            }

            if (missing > 0) Debug.LogError($"[GrowNa] 레이어 {missing}개가 비어 있다.");
            else Debug.Log("[GrowNa] 8슬롯 레이어가 모두 연결됐다.");
        }

        [MenuItem("GrowNa/Diagnose Gear Sprite Import")]
        public static void DiagnoseImport()
        {
            string[] files = { "gear_weapon", "gear_helmet", "gear_armor" };
            var report = new System.Text.StringBuilder("[GrowNa] 스프라이트 임포트 진단\n");

            foreach (string file in files)
            {
                string path = $"Assets/Art/Sprites/{file}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (sprite == null || importer == null)
                {
                    report.AppendLine($"{file}: 로드 실패 (sprite={sprite != null}, importer={importer != null})");
                    continue;
                }

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                report.AppendLine(
                    $"{file}: tex={texture.width}x{texture.height} rect={sprite.rect} " +
                    $"pivotPx={sprite.pivot} mesh={settings.spriteMeshType} align={settings.spriteAlignment} " +
                    $"mode={importer.spriteImportMode} ppu={sprite.pixelsPerUnit}");
            }
            Debug.Log(report.ToString());
        }

        [MenuItem("GrowNa/Preview Full Gear Set")]
        public static void PreviewFullSet()
        {
            var rig = Object.FindFirstObjectByType<CharacterRig>();
            if (rig == null)
            {
                Debug.LogError("[GrowNa] CharacterRig 가 씬에 없다.");
                return;
            }

            for (int i = 0; i < GearTable.SlotCount; i++)
            {
                var slot = (GearSlot)i;
                rig.Apply(slot, GearItem.Of(slot, GearTier.Legend, 5));
            }
            Debug.Log("[GrowNa] 전설 풀세트를 리그에 입혔다. 씬 뷰에서 실루엣 확인.");
        }

        [MenuItem("GrowNa/Clear Gear Preview")]
        public static void ClearPreview()
        {
            var rig = Object.FindFirstObjectByType<CharacterRig>();
            if (rig == null) return;
            for (int i = 0; i < GearTable.SlotCount; i++)
                rig.Apply((GearSlot)i, GearItem.Empty);
            Debug.Log("[GrowNa] 장비 프리뷰를 벗겼다.");
        }
    }
}
