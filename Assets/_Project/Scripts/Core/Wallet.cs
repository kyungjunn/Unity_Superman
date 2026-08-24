using System;
using UnityEngine;

namespace GrowNa.Core
{
    public enum CurrencyKind { Gold, Grain, Wick, Gem }

    public class Wallet : MonoBehaviour
    {
        public static Wallet Instance { get; private set; }
        public event Action Changed;

        [SerializeField] double gold;
        [SerializeField] double grain;   // 낟알
        [SerializeField] double wick;    // 잊혀진 심지
        [SerializeField] double gem;     // 보석

        public double Gold => gold;
        public double Grain => grain;
        public double Wick => wick;
        public double Gem => gem;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public double Get(CurrencyKind kind) => kind switch
        {
            CurrencyKind.Gold => gold,
            CurrencyKind.Grain => grain,
            CurrencyKind.Wick => wick,
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
                default: gem -= amount; break;
            }
            Changed?.Invoke();
            return true;
        }

        public void LoadFrom(double savedGold, double savedGrain, double savedWick, double savedGem)
        {
            gold = Math.Max(0, savedGold);
            grain = Math.Max(0, savedGrain);
            wick = Math.Max(0, savedWick);
            gem = Math.Max(0, savedGem);
            Changed?.Invoke();
        }

        public static string Format(double v) => BigNum.Format(v);
    }
}
