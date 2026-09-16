using GrowNa.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class LevelUpPopup : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text levelLabel;
        [SerializeField] Text detailLabel;
        [SerializeField] Button closeButton;
        [SerializeField] float autoCloseSeconds = 3.5f;

        PlayerStats stats;
        float timer;

        public void Bind(GameObject panelRoot, Text level, Text detail, Button close)
        {
            root = panelRoot;
            levelLabel = level;
            detailLabel = detail;
            closeButton = close;
        }

        public void BindServices(PlayerStats boundStats)
        {
            if (stats != null) stats.LeveledUp -= Show;
            stats = boundStats;
            if (stats != null) stats.LeveledUp += Show;
        }

        void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            SetOpen(false);
        }

        void OnDestroy()
        {
            if (stats != null) stats.LeveledUp -= Show;
        }

        void Show(int level)
        {
            if (root == null || stats == null) return;

            if (levelLabel != null) levelLabel.text = $"Lv.{level}";
            if (detailLabel != null)
                detailLabel.text = $"전투력 {BigNum.Format(stats.Power)}\n" +
                                   $"체력 {BigNum.Format(stats.MaxHp)}\n" +
                                   $"이제 Lv.{level} 장비를 얻습니다";

            SetOpen(true);
            timer = autoCloseSeconds;
        }

        void Hide() => SetOpen(false);

        void SetOpen(bool open)
        {
            if (root != null) root.SetActive(open);
            if (!open) timer = 0f;
        }

        void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) SetOpen(false);
        }
    }
}
