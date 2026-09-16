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
        [SerializeField] Text powerLabel;
        [SerializeField] Text goldLabel;
        [SerializeField] Text grainLabel;
        [SerializeField] Text wickLabel;
        [SerializeField] Text gemLabel;
        [SerializeField] Text stageLabel;
        [SerializeField] Text killLabel;
        [SerializeField] Text bossTimerLabel;
        [SerializeField] Image expFill;
        [SerializeField] Text expLabel;

        Wallet wallet;
        PlayerStats stats;
        BattleManager battle;

        public void Bind(Text playerName, Text level, Text power, Text gold, Text grain,
                         Text wick, Text gem, Text stage, Text kills, Text bossTimer)
        {
            nameLabel = playerName;
            levelLabel = level;
            powerLabel = power;
            goldLabel = gold;
            grainLabel = grain;
            wickLabel = wick;
            gemLabel = gem;
            stageLabel = stage;
            killLabel = kills;
            bossTimerLabel = bossTimer;
        }

        public void BindProgress(Image exp, Text expText)
        {
            expFill = exp;
            expLabel = expText;
        }

        public void BindServices(Wallet boundWallet, PlayerStats boundStats, BattleManager boundBattle)
        {
            UnbindServices();
            wallet = boundWallet;
            stats = boundStats;
            battle = boundBattle;
            if (wallet != null) wallet.Changed += RefreshWallet;
            if (stats != null)
            {
                stats.StatsChanged += RefreshStats;
                stats.ExpChanged += RefreshExp;
            }
            if (battle != null)
            {
                battle.StageChanged += RefreshStage;
                battle.KillProgressChanged += OnKillProgress;
            }
            RefreshAll();
        }

        void OnDestroy() => UnbindServices();

        void UnbindServices()
        {
            if (wallet != null) wallet.Changed -= RefreshWallet;
            if (stats != null)
            {
                stats.StatsChanged -= RefreshStats;
                stats.ExpChanged -= RefreshExp;
            }
            if (battle != null)
            {
                battle.StageChanged -= RefreshStage;
                battle.KillProgressChanged -= OnKillProgress;
            }
        }

        void Update()
        {
            if (battle == null || bossTimerLabel == null) return;
            bool boss = battle.BossPhase;
            bossTimerLabel.gameObject.SetActive(boss);
            if (boss) bossTimerLabel.text = $"보스 {battle.BossTimeLeft:0.0}초";
        }

        void OnKillProgress(int _) => RefreshStage();

        void RefreshAll()
        {
            RefreshWallet();
            RefreshStats();
            RefreshExp();
            RefreshStage();
        }

        void RefreshWallet()
        {
            if (wallet == null) return;
            if (goldLabel != null) goldLabel.text = Wallet.Format(wallet.Gold);
            if (grainLabel != null) grainLabel.text = Wallet.Format(wallet.Grain);
            if (wickLabel != null) wickLabel.text = Wallet.Format(wallet.Wick);
            if (gemLabel != null) gemLabel.text = Wallet.Format(wallet.Gem);
        }

        void RefreshExp()
        {
            if (stats == null) return;
            if (expFill != null) expFill.fillAmount = (float)stats.ExpRatio;
            if (expLabel != null)
                expLabel.text = $"EXP {Wallet.Format(stats.Exp)} / {Wallet.Format(stats.ExpRequired)}";
        }

        void RefreshStats()
        {
            if (stats == null) return;
            if (nameLabel != null) nameLabel.text = stats.DisplayName;
            if (levelLabel != null) levelLabel.text = $"Lv.{stats.Level}";
            if (powerLabel != null) powerLabel.text = Wallet.Format(stats.Power);
        }

        void RefreshStage()
        {
            if (battle == null) return;
            if (stageLabel != null) stageLabel.text = battle.StageLabel;
            if (killLabel != null)
                killLabel.text = battle.BossPhase
                    ? "보스 등장!"
                    : $"{battle.Kills} / {battle.KillsRequired} · {battle.CurrentRankLabel}";
        }
    }
}
