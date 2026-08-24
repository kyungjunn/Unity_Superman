using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Battle
{
    public class DamagePopup : MonoBehaviour
    {
        TextMesh label;
        float life;
        Vector3 velocity;

        public static DamagePopup Spawn(Vector3 worldPos, double amount, bool critical, bool onPlayer)
        {
            var go = new GameObject("DamagePopup");
            go.transform.position = worldPos + new Vector3(Random.Range(-0.18f, 0.18f), 0f, 0f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = Wallet.Format(amount);
            mesh.font = UiFont.Get();
            mesh.fontSize = 64;
            mesh.characterSize = 0.06f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = onPlayer ? new Color(1f, 0.45f, 0.45f)
                       : critical ? new Color(1f, 0.85f, 0.25f)
                                  : Color.white;
            mesh.fontStyle = critical ? FontStyle.Bold : FontStyle.Normal;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.material = mesh.font.material;
            renderer.sortingOrder = 40;

            var popup = go.AddComponent<DamagePopup>();
            popup.label = mesh;
            popup.life = 0.7f;
            popup.velocity = new Vector3(Random.Range(-0.3f, 0.3f), critical ? 2.1f : 1.6f, 0f);
            return popup;
        }

        void Update()
        {
            life -= Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            velocity += Vector3.down * (3.2f * Time.deltaTime);
            if (label != null)
            {
                var c = label.color;
                c.a = Mathf.Clamp01(life / 0.35f);
                label.color = c;
            }
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
