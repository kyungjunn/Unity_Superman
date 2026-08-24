using GrowNa.Battle;
using GrowNa.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class HudBinder : MonoBehaviour
    {
        [SerializeField] Text nameLabel;
        [SerializeField] Text levelLabel;
        [SerializeField] Text hpLabel;
        [SerializeField] Text powerLabel;
        [SerializeField] Text goldLabel;
        [SerializeField] Text grainLabel;
        [SerializeField] Text wickLabel;
        [SerializeField] Text stageLabel;
        [SerializeField] Text killLabel;
        [SerializeField] Text bossTimerLabel;
        [SerializeField] Image hpFill;

        public void Bind(Text playerName, Text level, Text hp, Text power, Text gold, Text grain,
                         Text wick, Text stage, Text kills, Text bossTimer, Image hpFillImage)
        {
            nameLabel = playerName;
            levelLabel = level;
            hpLabel = hp;
            powerLabel = power;
            goldLabel = gold;
            grainLabel = grain;
            wickLabel = wick;
            stageLabel = stage;
            killLabel = kills;
            bossTimerLabel = bossTimer;
            hpFill = hpFillImage;
        }

        void Start()
        {
            RefreshAll();
            if (Wallet.Instance != null) Wallet.Instance.Changed += RefreshWallet;
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.HpChanged += RefreshHp;
                PlayerStats.Instance.StatsChanged += RefreshStats;
            }
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.StageChanged += RefreshStage;
                BattleManager.Instance.KillProgressChanged += _ => RefreshStage();
            }
        }

        void OnDestroy()
        {
            if (Wallet.Instance != null) Wallet.Instance.Changed -= RefreshWallet;
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.HpChanged -= RefreshHp;
                PlayerStats.Instance.StatsChanged -= RefreshStats;
            }
        }

        void Update()
        {
            var battle = BattleManager.Instance;
            if (battle == null || bossTimerLabel == null) return;
            bool boss = battle.BossPhase;
            bossTimerLabel.gameObject.SetActive(boss);
            if (boss) bossTimerLabel.text = $"보스 {battle.BossTimeLeft:0.0}초";
        }

        void RefreshAll()
        {
            RefreshWallet();
            RefreshHp();
            RefreshStats();
            RefreshStage();
        }

        void RefreshWallet()
        {
            var w = Wallet.Instance;
            if (w == null) return;
            if (goldLabel != null) goldLabel.text = Wallet.Format(w.Gold);
            if (grainLabel != null) grainLabel.text = Wallet.Format(w.Grain);
            if (wickLabel != null) wickLabel.text = Wallet.Format(w.Wick);
        }

        void RefreshHp()
        {
            var s = PlayerStats.Instance;
            if (s == null) return;
            if (hpFill != null) hpFill.fillAmount = (float)s.HpRatio;
            if (hpLabel != null) hpLabel.text = $"{Wallet.Format(s.CurrentHp)} / {Wallet.Format(s.MaxHp)}";
        }

        void RefreshStats()
        {
            var s = PlayerStats.Instance;
            if (s == null) return;
            if (nameLabel != null) nameLabel.text = s.DisplayName;
            if (levelLabel != null) levelLabel.text = $"Lv.{s.Level}";
            if (powerLabel != null) powerLabel.text = Wallet.Format(s.Power);
        }

        void RefreshStage()
        {
            var b = BattleManager.Instance;
            if (b == null) return;
            if (stageLabel != null) stageLabel.text = b.StageLabel;
            if (killLabel != null)
                killLabel.text = b.BossPhase ? "보스 등장!" : $"{b.Kills} / {b.KillsRequired}";
        }
    }
}
