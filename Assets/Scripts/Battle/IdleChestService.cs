using System;
using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Battle
{
    public class IdleChestService : MonoBehaviour
    {
        public const double ReadySeconds = 300;

        [SerializeField] double bankedSeconds;

        Wallet wallet;
        BattleManager battle;

        public event Action Changed;
        public event Action<OfflineResult, bool> Claimed;

        public double BankedSeconds => bankedSeconds;
        public bool Ready => bankedSeconds >= ReadySeconds;
        public double ReadyRatio => Mathf.Clamp01((float)(bankedSeconds / ReadySeconds));

        public void Bind(Wallet boundWallet, BattleManager boundBattle)
        {
            wallet = boundWallet;
            battle = boundBattle;
        }

        public void Tick(float deltaTime)
        {
            bool wasReady = Ready;
            bankedSeconds += deltaTime;
            if (Ready != wasReady) Changed?.Invoke();
        }

        public void Bank(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds)) return;
            bankedSeconds += seconds;
            Changed?.Invoke();
        }

        public OfflineResult Preview(bool doubled)
        {
            int world = battle != null ? battle.World : 1;
            int stage = battle != null ? battle.Stage : 1;
            var result = OfflineRewards.Compute(world, stage, bankedSeconds);
            return doubled ? OfflineRewards.Doubled(result) : result;
        }

        public bool Claim(bool doubled)
        {
            var result = Preview(doubled);
            if (!result.Any) return false;

            if (wallet != null)
            {
                wallet.Add(CurrencyKind.Gold, result.gold);
                wallet.Add(CurrencyKind.Grain, result.grain);
            }

            bankedSeconds = 0;
            Claimed?.Invoke(result, doubled);
            Changed?.Invoke();
            return true;
        }

        public void LoadFrom(double savedSeconds)
        {
            bankedSeconds = savedSeconds > 0 ? savedSeconds : 0;
            Changed?.Invoke();
        }
    }
}
