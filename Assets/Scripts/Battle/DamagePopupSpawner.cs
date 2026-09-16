using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Battle
{
    public class DamagePopupSpawner : MonoBehaviour
    {
        UiFont fonts;
        IGameRandom rng;

        public void Bind(UiFont boundFonts, IGameRandom boundRandom)
        {
            fonts = boundFonts;
            rng = boundRandom;
        }

        public DamagePopup Spawn(Vector3 worldPos, double amount, bool critical, bool onPlayer)
        {
            float jitter = rng != null ? rng.Range(-0.18f, 0.18f) : 0f;
            var go = new GameObject("DamagePopup");
            go.transform.position = worldPos + new Vector3(jitter, 0f, 0f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = Wallet.Format(amount);
            mesh.font = fonts != null ? fonts.Get() : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.fontSize = 64;
            mesh.characterSize = 0.06f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = onPlayer ? new Color(1f, 0.45f, 0.45f)
                       : critical ? new Color(1f, 0.85f, 0.25f)
                                  : Color.white;
            mesh.fontStyle = critical ? FontStyle.Bold : FontStyle.Normal;

            var renderer = go.GetComponent<MeshRenderer>();
            if (mesh.font != null) renderer.material = mesh.font.material;
            renderer.sortingOrder = 40;

            var popup = go.AddComponent<DamagePopup>();
            float vx = rng != null ? rng.Range(-0.3f, 0.3f) : 0f;
            popup.Begin(mesh, 0.7f, new Vector3(vx, critical ? 2.1f : 1.6f, 0f));
            return popup;
        }
    }
}
