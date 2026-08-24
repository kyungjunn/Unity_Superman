using UnityEngine;

namespace GrowNa.Battle
{
    public class PetOrbit : MonoBehaviour
    {
        [SerializeField] Transform anchor;
        [SerializeField] Vector3 offset = new Vector3(-0.85f, 0.75f, 0f);
        [SerializeField] float followSpeed = 3.5f;
        [SerializeField] float hoverAmplitude = 0.12f;
        [SerializeField] float hoverFrequency = 3.1f;

        public void Bind(Transform target) => anchor = target;

        void Update()
        {
            if (anchor == null) return;
            Vector3 hover = new Vector3(0f, Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude, 0f);
            Vector3 desired = anchor.position + offset + hover;
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        }
    }
}
