using UnityEngine;

namespace GrowNa.Battle
{
    public class IdleBob : MonoBehaviour
    {
        [SerializeField] float amplitude = 0.06f;
        [SerializeField] float frequency = 2.2f;
        [SerializeField] float swayDegrees = 2.5f;

        Vector3 origin;
        float seed;

        void Awake()
        {
            origin = transform.localPosition;
            seed = Random.Range(0f, 10f);
        }

        void Update()
        {
            float t = Time.time * frequency + seed;
            transform.localPosition = origin + new Vector3(0f, Mathf.Sin(t) * amplitude, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.5f) * swayDegrees);
        }
    }
}
