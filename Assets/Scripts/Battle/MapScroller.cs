using UnityEngine;

namespace GrowNa.Battle
{
    public class MapScroller : MonoBehaviour
    {
        [SerializeField] Transform[] segments;
        [SerializeField] float width = 40f;

        public void Bind(Transform[] mapSegments, float segmentWidth)
        {
            segments = mapSegments;
            width = Mathf.Max(1f, segmentWidth);
        }

        public void Scroll(float distance)
        {
            if (segments == null || distance == 0f) return;
            float span = width * segments.Length;
            foreach (var segment in segments)
            {
                if (segment == null) continue;
                segment.position += Vector3.left * distance;
                if (segment.position.x <= -width)
                    segment.position += Vector3.right * span;
            }
        }
    }
}
