using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Players
{
    public class PlayerHpBar : MonoBehaviour
    {
        [SerializeField] Transform fillPivot;
        [SerializeField] SpriteRenderer fillRenderer;
        PlayerStats stats;

        public void Bind(Transform pivot, SpriteRenderer fill)
        {
            fillPivot = pivot;
            fillRenderer = fill;
        }

        public void BindStats(PlayerStats boundStats)
        {
            if (stats != null)
            {
                stats.HpChanged -= Refresh;
                stats.StatsChanged -= Refresh;
            }
            stats = boundStats;
            if (stats != null)
            {
                stats.HpChanged += Refresh;
                stats.StatsChanged += Refresh;
            }
            Refresh();
        }

        void OnDestroy()
        {
            if (stats != null)
            {
                stats.HpChanged -= Refresh;
                stats.StatsChanged -= Refresh;
            }
        }

        void Refresh()
        {
            if (stats == null || fillPivot == null) return;

            float ratio = (float)stats.HpRatio;
            fillPivot.localScale = new Vector3(ratio, 1f, 1f);

            if (fillRenderer != null)
                fillRenderer.color = ratio > 0.5f ? new Color(0.42f, 0.78f, 0.36f)
                                   : ratio > 0.2f ? new Color(0.92f, 0.74f, 0.24f)
                                                  : new Color(0.86f, 0.28f, 0.28f);
        }
    }
}
