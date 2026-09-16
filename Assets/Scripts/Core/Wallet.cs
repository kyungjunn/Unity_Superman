using System;
using UnityEngine;

namespace GrowNa.Core
{
    public enum CurrencyKind { Gold, Grain, Wick, Gem, Shard }

    public class Wallet : MonoBehaviour
    {
        public event Action Changed;

        /// <summary>
        /// 새 계정이 들고 시작하는 낟알. 첫 봉헌을 몬스터 사냥 전에 한 번 돌려보게 하는 튜토리얼 씨앗이다.
        /// 세이브가 있으면 <see cref="LoadFrom"/> 이 덮어쓰므로 최초 1회만 의미가 있다.
        /// </summary>
        public const double StartingGrain = 2;

        [SerializeField] double gold;
        [SerializeField] double grain = StartingGrain;
        [SerializeField] double wick;
        [SerializeField] double gem;
        [SerializeField] double shard;

        public double Gold => gold;
        public double Grain => grain;
        public double Wick => wick;
        public double Gem => gem;
        public double Shard => shard;

        public double Get(CurrencyKind kind) => kind switch
        {
            CurrencyKind.Gold => gold,
            CurrencyKind.Grain => grain,
            CurrencyKind.Wick => wick,
            CurrencyKind.Shard => shard,
            _ => gem,
        };

        public void Add(CurrencyKind kind, double amount)
        {
            if (amount <= 0) return;
            switch (kind)
            {
                case CurrencyKind.Gold: gold += amount; break;
                case CurrencyKind.Grain: grain += amount; break;
                case CurrencyKind.Wick: wick += amount; break;
                case CurrencyKind.Shard: shard += amount; break;
                default: gem += amount; break;
            }
            Changed?.Invoke();
        }

        public bool TrySpend(CurrencyKind kind, double amount)
        {
            if (amount <= 0 || Get(kind) < amount) return false;
            switch (kind)
            {
                case CurrencyKind.Gold: gold -= amount; break;
                case CurrencyKind.Grain: grain -= amount; break;
                case CurrencyKind.Wick: wick -= amount; break;
                case CurrencyKind.Shard: shard -= amount; break;
                default: gem -= amount; break;
            }
            Changed?.Invoke();
            return true;
        }

        public void LoadFrom(double savedGold, double savedGrain, double savedWick, double savedGem, double savedShard)
        {
            gold = Math.Max(0, savedGold);
            grain = Math.Max(0, savedGrain);
            wick = Math.Max(0, savedWick);
            gem = Math.Max(0, savedGem);
            shard = Math.Max(0, savedShard);
            Changed?.Invoke();
        }

        public static string Format(double v) => BigNum.Format(v);
    }
}
