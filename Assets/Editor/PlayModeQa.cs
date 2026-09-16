using System.Text;
using GrowNa.Battle;
using GrowNa.Composition;
using GrowNa.Core;
using GrowNa.Gear;
using GrowNa.Persistence;
using GrowNa.UI;
using UnityEditor;
using UnityEngine;

namespace GrowNa.EditorTools
{
    public static class PlayModeQa
    {
        static SceneRuntimeBootstrap Boot()
        {
            var boots = Object.FindObjectsByType<SceneRuntimeBootstrap>(FindObjectsSortMode.None);
            return boots.Length == 1 ? boots[0] : null;
        }

        [MenuItem("GrowNa/QA/Grant Test Currency")]
        public static void GrantCurrency()
        {
            if (!Guard(out var boot)) return;
            var wallet = boot.Wallet;
            wallet.Add(CurrencyKind.Gold, 5_000_000);
            wallet.Add(CurrencyKind.Grain, 100_000);
            wallet.Add(CurrencyKind.Wick, 5_000);
            Debug.Log($"[QA] 재화 지급 — 골드 {BigNum.Format(wallet.Gold)}, 낟알 {BigNum.Format(wallet.Grain)}, 심지 {BigNum.Format(wallet.Wick)}");
        }

        [MenuItem("GrowNa/QA/Buy Stat Upgrades")]
        public static void BuyUpgrades()
        {
            if (!Guard(out var boot)) return;
            var stats = boot.Stats;
            var service = boot.Upgrades;
            double powerBefore = stats.Power;
            var log = new StringBuilder();

            foreach (var kind in UpgradeTable.All)
            {
                int bought = service.TryUpgradeMax(kind, 5);
                log.AppendLine($"{UpgradeTable.DisplayName(kind)} +{bought} -> Lv.{stats.UpgradeLevel(kind)} " +
                               $"({UpgradeTable.BonusText(kind, stats.UpgradeLevel(kind))})");
            }

            Debug.Log($"[QA] 강화 결과\n{log}전투력 {BigNum.Format(powerBefore)} -> {BigNum.Format(stats.Power)} " +
                      $"/ 남은 골드 {BigNum.Format(boot.Wallet.Gold)}");
        }

        [MenuItem("GrowNa/QA/Offer Ten")]
        public static void OfferTen()
        {
            if (!Guard(out var boot)) return;
            var lamp = boot.Lamp;
            double grainBefore = boot.Wallet.Grain;
            var results = lamp.Offer(10);
            if (results == null)
            {
                Debug.LogWarning("[QA] 봉헌 실패 — 낟알 부족");
                return;
            }

            var log = new StringBuilder();
            foreach (var item in results)
            {
                boot.Inventory?.Acquire(item);
                log.Append($"{item.DisplayName} / ");
            }

            var loadout = boot.Loadout;
            Debug.Log($"[QA] 10연 봉헌\n{log}\n낟알 {BigNum.Format(grainBefore)} -> {BigNum.Format(boot.Wallet.Grain)}, " +
                      $"장착 {loadout.EquippedCount}/8, 장비 전투력합 {BigNum.Format(loadout.TotalPower)}, " +
                      $"세트배율 {loadout.SetMultiplier():0.00}, 캐릭터 전투력 {BigNum.Format(boot.Stats.Power)}, " +
                      $"천장까지 {lamp.OfferingsUntilPity}회");
        }

        [MenuItem("GrowNa/QA/Upgrade Lamp Five Times")]
        public static void UpgradeLamp()
        {
            if (!Guard(out var boot)) return;
            var lamp = boot.Lamp;
            double legendBefore = lamp.LegendChance;
            int done = 0;
            for (int i = 0; i < 5 && lamp.TryUpgrade(); i++) done++;
            Debug.Log($"[QA] 심지 교체 {done}회 — 등불 Lv.{lamp.LampLevel}, " +
                      $"전설 {BigNum.Percent(legendBefore)} -> {BigNum.Percent(lamp.LegendChance)}, " +
                      $"최고티어 {GearTable.Name(lamp.HighestTier)}, 다음 비용 심지 {BigNum.Format(lamp.WickCost)}");
        }

        [MenuItem("GrowNa/QA/Verify Button Wiring")]
        public static void VerifyButtons()
        {
            if (!Guard(out _)) return;
            var report = new StringBuilder();
            int dead = 0;

            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
            {
                string path = button.name;
                for (var t = button.transform.parent; t != null; t = t.parent) path = $"{t.name}/{path}";
                if (!path.Contains("NavBar") && !path.Contains("LampButton") && !path.Contains("Slot_")) continue;

                bool before = AnyPanelOpen();
                button.onClick.Invoke();
                bool after = AnyPanelOpen();
                if (before == after && !path.Contains("NavBar")) { dead++; report.AppendLine($"무반응 {path}"); }
                else report.AppendLine($"반응함 {path} (패널 {before} → {after})");
                CloseAllPanels();
            }

            if (dead > 0) Debug.LogError($"[QA] 버튼 배선 실패 {dead}건\n{report}");
            else Debug.Log($"[QA] 버튼 배선 정상\n{report}");
        }

        static bool AnyPanelOpen()
        {
            foreach (var panel in Object.FindObjectsByType<GearPanel>(FindObjectsSortMode.None))
                if (panel.gameObject.activeSelf) return true;
            foreach (var panel in Object.FindObjectsByType<LampPanel>(FindObjectsSortMode.None))
                if (panel.gameObject.activeSelf) return true;
            foreach (var panel in Object.FindObjectsByType<UpgradePanel>(FindObjectsSortMode.None))
                if (panel.gameObject.activeSelf) return true;
            foreach (var panel in Object.FindObjectsByType<OfferResultPanel>(FindObjectsSortMode.None))
                if (panel.gameObject.activeSelf) return true;
            return false;
        }

        static void CloseAllPanels()
        {
            foreach (var panel in Resources.FindObjectsOfTypeAll<GearPanel>()) panel.SetOpen(false);
            foreach (var panel in Resources.FindObjectsOfTypeAll<LampPanel>()) panel.SetOpen(false);
            foreach (var panel in Resources.FindObjectsOfTypeAll<UpgradePanel>()) panel.SetOpen(false);
            foreach (var panel in Resources.FindObjectsOfTypeAll<OfferResultPanel>()) panel.SetOpen(false);
        }

        [MenuItem("GrowNa/QA/Auto Start With Decent Filter")]
        public static void StartAutoDecent()
        {
            if (!Guard(out var boot)) return;
            var auto = boot.AutoOffer;
            if (auto == null) { Debug.LogError("[QA] AutoOfferService 가 없다."); return; }

            while (auto.KeepTier > GearTier.Decent) auto.StepKeepTier(-1);
            while (auto.KeepTier < GearTier.Decent) auto.StepKeepTier(1);
            while (auto.BatchSize > 1) auto.StepBatchSize(-1);
            auto.StartAuto();
            Debug.Log($"[QA] 자동 시작 — 기준 {GearTable.Name(auto.KeepTier)} 이상 · 1회 {auto.BatchSize}개, Running={auto.Running}");
        }

        [MenuItem("GrowNa/QA/Report Auto")]
        public static void ReportAuto()
        {
            if (!Guard(out var boot)) return;
            var auto = boot.AutoOffer;
            var panels = Resources.FindObjectsOfTypeAll<OfferResultPanel>();
            if (auto == null || panels.Length == 0) { Debug.LogError("[QA] 자동 점등 구성 요소가 없다."); return; }

            Debug.Log($"[QA] 자동 상태 — Running={auto.Running} · 기준 {GearTable.Name(auto.KeepTier)} 이상 · " +
                      $"판매 {auto.SoldCount}개 골드 +{BigNum.Format(auto.SoldGold)} · " +
                      $"결과패널 열림={panels[0].gameObject.activeSelf}");
        }

        [MenuItem("GrowNa/QA/Verify Offer Flow")]
        public static void VerifyOfferFlow()
        {
            if (!Guard(out var boot)) return;
            var lamp = boot.Lamp;
            var inventory = boot.Inventory;
            var loadout = boot.Loadout;
            var wallet = boot.Wallet;
            var panel = Resources.FindObjectsOfTypeAll<OfferResultPanel>().Length > 0
                ? Resources.FindObjectsOfTypeAll<OfferResultPanel>()[0] : null;
            if (lamp == null || inventory == null || loadout == null || panel == null)
            {
                Debug.LogError("[QA] 봉헌 경로 구성 요소가 없다. Build Main Scene 후 다시.");
                return;
            }

            var log = new StringBuilder();
            double grainBefore = wallet.Grain;
            if (!lamp.TryOfferOne(out var first))
            {
                Debug.LogWarning("[QA] 낟알 부족 — Grant Test Currency 먼저.");
                return;
            }
            log.AppendLine($"봉헌 1회 — {first.DisplayName} / 낟알 {BigNum.Format(grainBefore)} → {BigNum.Format(wallet.Grain)}");

            panel.Show(first);
            log.AppendLine($"결과 패널 열림 = {panel.gameObject.activeSelf}");

            double goldBefore = wallet.Gold;
            double paid = inventory.Sell(first);
            log.AppendLine($"판매 — 골드 +{BigNum.Format(paid)} (예상 {BigNum.Format(first.SellGold)}) " +
                           $"→ {BigNum.Format(goldBefore)} → {BigNum.Format(wallet.Gold)}");
            panel.SetOpen(false);

            if (lamp.TryOfferOne(out var second))
            {
                inventory.Equip(second);
                var worn = loadout.Get(second.slot);
                log.AppendLine($"장착 — {second.DisplayName} → 슬롯 {GearTable.Name(second.slot)} = {worn.ShortLabel} " +
                               $"(일치 {worn.tier == second.tier})");
            }

            var auto = boot.AutoOffer;
            if (auto != null)
            {
                auto.StepKeepTier(1);
                auto.StepBatchSize(1);
                log.AppendLine($"자동 설정 — 기준 {GearTable.Name(auto.KeepTier)} 이상 · 1회 {auto.BatchSize}개 " +
                               $"(낟알 {BigNum.Format(auto.BatchCost)})");
                auto.StartAuto();
                log.AppendLine($"자동 시작 = {auto.Running}");
            }

            Debug.Log($"[QA] 봉헌 경로\n{log}");
        }

        [MenuItem("GrowNa/QA/Reset Save File")]
        public static void ResetSave()
        {
            string path = new FileSaveStore().Path;
            Debug.Log($"[QA] 세이브 경로 {path} — 자동 삭제는 잠긴 정책상 하지 않는다. 파일이 있으면 직접 지우거나 별도 승인을 받는다.");
        }

        [MenuItem("GrowNa/QA/Dismantle Spares")]
        public static void DismantleSpares()
        {
            if (!Guard(out var boot)) return;
            var inventory = boot.Inventory;
            if (inventory == null) { Debug.LogWarning("[QA] Inventory 가 없다. Build Main Scene 후 다시."); return; }

            int before = inventory.Stock.TotalCount;
            double shard = inventory.DismantleUpTo(GearTier.Heroic);
            Debug.Log($"[QA] 일괄 분해 — 영웅 이하 {before - inventory.Stock.TotalCount}개 → 파편 {BigNum.Format(shard)} " +
                      $"(보유 파편 {BigNum.Format(boot.Wallet.Shard)}, 남은 재고 {inventory.Stock.TotalCount}개)");
        }

        [MenuItem("GrowNa/QA/Enhance Weapon Five Times")]
        public static void EnhanceWeapon()
        {
            if (!Guard(out var boot)) return;
            var service = boot.Enhance;
            var loadout = boot.Loadout;
            if (service == null || loadout == null) { Debug.LogWarning("[QA] EnhanceService 가 없다."); return; }
            if (!loadout.Get(GearSlot.Weapon).owned) { Debug.LogWarning("[QA] 무기를 먼저 얻을 것."); return; }

            var log = new StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                var item = loadout.Get(GearSlot.Weapon);
                double cost = service.CostOf(GearSlot.Weapon);
                double chance = service.ChanceOf(GearSlot.Weapon);
                if (!service.CanEnhance(GearSlot.Weapon))
                {
                    log.AppendLine($"+{item.plus} 시도 불가 (골드 {BigNum.Format(cost)} 필요)");
                    break;
                }
                bool ok = service.TryEnhance(GearSlot.Weapon);
                log.AppendLine($"+{item.plus} → {(ok ? "성공" : "실패")} (확률 {BigNum.Percent(chance, 0)}, 골드 {BigNum.Format(cost)}, " +
                               $"연속실패 {service.FailStreak(GearSlot.Weapon)})");
            }

            Debug.Log($"[QA] 무기 강화\n{log}현재 {loadout.Get(GearSlot.Weapon).DisplayName} " +
                      $"+{loadout.Get(GearSlot.Weapon).plus} / 전투력 {BigNum.Format(boot.Stats.Power)}");
        }

        [MenuItem("GrowNa/QA/Fuse Everything Possible")]
        public static void FuseAll()
        {
            if (!Guard(out var boot)) return;
            var inventory = boot.Inventory;
            if (inventory == null) { Debug.LogWarning("[QA] Inventory 가 없다."); return; }

            int fused = 0;
            for (int s = 0; s < GearTable.SlotCount; s++)
                for (int t = 0; t < GearTable.TierCount; t++)
                    while (inventory.Fuse((GearSlot)s, (GearTier)t)) fused++;

            Debug.Log($"[QA] 합성 {fused}회 — 재고 {inventory.Stock.TotalCount}개, 도감 {inventory.CodexCount}/{GearTable.StockSize}, " +
                      $"도감 배수 {inventory.CodexMultiplier:0.000}, 전투력 {BigNum.Format(boot.Stats.Power)}");
        }

        [MenuItem("GrowNa/QA/Report State")]
        public static void Report()
        {
            if (!Guard(out var boot)) return;
            var stats = boot.Stats;
            var wallet = boot.Wallet;
            var loadout = boot.Loadout;
            var equipped = new StringBuilder();
            for (int i = 0; i < GearTable.SlotCount; i++)
            {
                var item = loadout.Get((GearSlot)i);
                equipped.Append($"{GearTable.Name((GearSlot)i)}={item.ShortLabel} ");
            }

            var inventory = boot.Inventory;
            string stash = inventory == null
                ? "인벤토리 없음"
                : $"재고 {inventory.Stock.TotalCount}개 · 도감 {inventory.CodexCount}/{GearTable.StockSize} " +
                  $"(배수 {inventory.CodexMultiplier:0.000})";

            Debug.Log($"[QA] 상태 — Lv.{stats.Level} 전투력 {BigNum.Format(stats.Power)} " +
                      $"HP {BigNum.Format(stats.CurrentHp)}/{BigNum.Format(stats.MaxHp)}\n" +
                      $"골드 {BigNum.Format(wallet.Gold)} 낟알 {BigNum.Format(wallet.Grain)} 심지 {BigNum.Format(wallet.Wick)} " +
                      $"파편 {BigNum.Format(wallet.Shard)}\n{stash}\n{equipped}");
        }

        static bool Guard(out SceneRuntimeBootstrap boot)
        {
            boot = null;
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[QA] 플레이 모드에서만 동작한다.");
                return false;
            }
            boot = Boot();
            if (boot == null || boot.Wallet == null || boot.Stats == null)
            {
                Debug.LogError("[QA] SceneRuntimeBootstrap 이 없다.");
                return false;
            }
            return true;
        }
    }
}
