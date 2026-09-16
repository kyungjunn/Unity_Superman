using UnityEngine;

namespace GrowNa.Battle
{
    public class DamagePopup : MonoBehaviour
    {
        TextMesh label;
        float life;
        Vector3 velocity;

        public void Begin(TextMesh mesh, float duration, Vector3 motion)
        {
            label = mesh;
            life = duration;
            velocity = motion;
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
