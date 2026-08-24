using System.Text;
using GrowNa.Core;
using GrowNa.Gear;
using UnityEditor;
using UnityEngine;

namespace GrowNa.EditorTools
{
    public static class PlayModeQa
    {
        [MenuItem("GrowNa/QA/Grant Test Currency")]
        public static void GrantCurrency()
        {
            if (!Guard()) return;
            var wallet = Wallet.Instance;
            wallet.Add(CurrencyKind.Gold, 5_000_000);
            wallet.Add(CurrencyKind.Grain, 100_000);
            wallet.Add(CurrencyKind.Wick, 5_000);
            Debug.Log($"[QA] 재화 지급 — 골드 {BigNum.Format(wallet.Gold)}, 낟알 {BigNum.Format(wallet.Grain)}, 심지 {BigNum.Format(wallet.Wick)}");
        }

        [MenuItem("GrowNa/QA/Buy Stat Upgrades")]
        public static void BuyUpgrades()
        {
            if (!Guard()) return;
            var stats = PlayerStats.Instance;
            var service = UpgradeService.Instance;
            double powerBefore = stats.Power;
            var log = new StringBuilder();

            foreach (var kind in UpgradeTable.All)
            {
                int bought = service.TryUpgradeMax(kind, 5);
                log.AppendLine($"{UpgradeTable.DisplayName(kind)} +{bought} -> Lv.{stats.UpgradeLevel(kind)} " +
                               $"({UpgradeTable.BonusText(kind, stats.UpgradeLevel(kind))})");
            }

            Debug.Log($"[QA] 강화 결과\n{log}전투력 {BigNum.Format(powerBefore)} -> {BigNum.Format(stats.Power)} " +
                      $"/ 남은 골드 {BigNum.Format(Wallet.Instance.Gold)}");
        }

        [MenuItem("GrowNa/QA/Offer Ten")]
        public static void OfferTen()
        {
            if (!Guard()) return;
            var lamp = LampService.Instance;
            double grainBefore = Wallet.Instance.Grain;
            var results = lamp.Offer(LampTable.TenOfferCount);
            if (results == null)
            {
                Debug.LogWarning("[QA] 봉헌 실패 — 낟알 부족");
                return;
            }

            var log = new StringBuilder();
            foreach (var item in results) log.Append($"{item.DisplayName} / ");

            var loadout = Loadout.Instance;
            Debug.Log($"[QA] 10연 봉헌\n{log}\n낟알 {BigNum.Format(grainBefore)} -> {BigNum.Format(Wallet.Instance.Grain)}, " +
                      $"장착 {loadout.EquippedCount}/8, 장비 전투력합 {BigNum.Format(loadout.TotalPower)}, " +
                      $"세트배율 {loadout.SetMultiplier():0.00}, 캐릭터 전투력 {BigNum.Format(PlayerStats.Instance.Power)}, " +
                      $"천장까지 {lamp.OfferingsUntilPity}회");
        }

        [MenuItem("GrowNa/QA/Upgrade Lamp Five Times")]
        public static void UpgradeLamp()
        {
            if (!Guard()) return;
            var lamp = LampService.Instance;
            double legendBefore = lamp.LegendChance;
            int done = 0;
            for (int i = 0; i < 5 && lamp.TryUpgrade(); i++) done++;
            Debug.Log($"[QA] 심지 교체 {done}회 — 등불 Lv.{lamp.LampLevel}, " +
                      $"전설 {BigNum.Percent(legendBefore)} -> {BigNum.Percent(lamp.LegendChance)}, " +
                      $"최고티어 {GearTable.Name(lamp.HighestTier)}, 다음 비용 심지 {BigNum.Format(lamp.WickCost)}");
        }

        [MenuItem("GrowNa/QA/Report State")]
        public static void Report()
        {
            if (!Guard()) return;
            var stats = PlayerStats.Instance;
            var wallet = Wallet.Instance;
            var loadout = Loadout.Instance;
            var equipped = new StringBuilder();
            for (int i = 0; i < GearTable.SlotCount; i++)
            {
                var item = loadout.Get((GearSlot)i);
                equipped.Append($"{GearTable.Name((GearSlot)i)}={item.ShortLabel} ");
            }

            Debug.Log($"[QA] 상태 — Lv.{stats.Level} 전투력 {BigNum.Format(stats.Power)} " +
                      $"HP {BigNum.Format(stats.CurrentHp)}/{BigNum.Format(stats.MaxHp)}\n" +
                      $"골드 {BigNum.Format(wallet.Gold)} 낟알 {BigNum.Format(wallet.Grain)} 심지 {BigNum.Format(wallet.Wick)}\n" +
                      $"{equipped}");
        }

        static bool Guard()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[QA] 플레이 모드에서만 동작한다.");
                return false;
            }
            if (Wallet.Instance == null || PlayerStats.Instance == null)
            {
                Debug.LogError("[QA] 시스템 싱글턴이 없다.");
                return false;
            }
            return true;
        }
    }
}
