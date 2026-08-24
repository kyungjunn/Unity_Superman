using System;
using GrowNa.Battle;
using GrowNa.Gear;
using UnityEngine;

namespace GrowNa.Core
{
    public class SaveService : MonoBehaviour
    {
        public static SaveService Instance { get; private set; }

        [SerializeField] float autoSaveInterval = 20f;

        float timer;
        SaveData loaded;

        public event Action<OfflineResult> OfflineGranted;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            loaded = SaveSystem.Read();
        }

        void Start()
        {
            if (loaded != null) Apply(loaded);
            GrantOffline(loaded);
            timer = autoSaveInterval;
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = autoSaveInterval;
            Save();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        void OnApplicationQuit() => Save();

        static void Apply(SaveData data)
        {
            Wallet.Instance?.LoadFrom(data.gold, data.grain, data.wick, data.gem);
            PlayerStats.Instance?.LoadFrom(data.level, data.evolutionStage, data.upgradeLevels, data.currentHp);
            Loadout.Instance?.LoadFrom(data.gear);
            LampService.Instance?.LoadFrom(data.lampLevel, data.offeringsSincePity, data.totalOfferings);
            BattleManager.Instance?.LoadFrom(data.world, data.stage, data.kills);
        }

        void GrantOffline(SaveData data)
        {
            if (data == null) return;
            var result = OfflineRewards.Compute(data.world, data.stage, data.ElapsedSecondsSince(DateTime.UtcNow));
            if (!result.Any) return;

            var wallet = Wallet.Instance;
            if (wallet != null)
            {
                wallet.Add(CurrencyKind.Gold, result.gold);
                wallet.Add(CurrencyKind.Grain, result.grain);
            }
            OfflineGranted?.Invoke(result);
        }

        public SaveData Capture()
        {
            var data = new SaveData();
            var wallet = Wallet.Instance;
            if (wallet != null)
            {
                data.gold = wallet.Gold;
                data.grain = wallet.Grain;
                data.wick = wallet.Wick;
                data.gem = wallet.Gem;
            }

            var stats = PlayerStats.Instance;
            if (stats != null)
            {
                data.level = stats.Level;
                data.evolutionStage = stats.EvolutionStage;
                data.upgradeLevels = stats.UpgradeLevelsCopy();
                data.currentHp = stats.CurrentHp;
            }

            var battle = BattleManager.Instance;
            if (battle != null)
            {
                data.world = battle.World;
                data.stage = battle.Stage;
                data.kills = battle.Kills;
            }

            var lamp = LampService.Instance;
            if (lamp != null)
            {
                data.lampLevel = lamp.LampLevel;
                data.offeringsSincePity = lamp.OfferingsSincePity;
                data.totalOfferings = lamp.TotalOfferings;
            }

            var loadout = Loadout.Instance;
            if (loadout != null) data.gear = loadout.Snapshot();

            data.Stamp(DateTime.UtcNow);
            return data;
        }

        public void Save() => SaveSystem.Write(Capture());

        public void ResetProgress()
        {
            SaveSystem.Delete();
            loaded = null;
        }
    }
}
