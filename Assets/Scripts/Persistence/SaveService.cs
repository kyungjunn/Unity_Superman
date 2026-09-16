using System;
using GrowNa.Battle;
using GrowNa.Core;
using GrowNa.Gear;
using GrowNa.Pet;
using GrowNa.Skill;
using UnityEngine;

namespace GrowNa.Persistence
{
    public enum SaveWriteGate
    {
        Closed = 0,
        Open = 1,
        Blocked = 2,
    }

    public class SaveService : MonoBehaviour
    {
        ISaveStore store;
        IClock clock;
        Wallet wallet;
        PlayerStats stats;
        Loadout loadout;
        Inventory inventory;
        EnhanceService enhance;
        LampService lamp;
        SkillService skills;
        PetService pets;
        QuestService quests;
        IdleChestService chest;
        BattleManager battle;

        SaveWriteGate gate = SaveWriteGate.Closed;
        float autoSaveInterval = 20f;
        float timer;
        bool ticking;

        public event Action<OfflineResult> OfflineGranted;

        public SaveWriteGate Gate => gate;
        public ISaveStore Store => store;
        public IClock Clock => clock;

        public void Bind(
            ISaveStore saveStore,
            IClock saveClock,
            Wallet boundWallet,
            PlayerStats boundStats,
            Loadout boundLoadout,
            Inventory boundInventory,
            EnhanceService boundEnhance,
            LampService boundLamp,
            SkillService boundSkills,
            PetService boundPets,
            QuestService boundQuests,
            IdleChestService boundChest,
            BattleManager boundBattle,
            float interval = 20f)
        {
            store = saveStore;
            clock = saveClock;
            wallet = boundWallet;
            stats = boundStats;
            loadout = boundLoadout;
            inventory = boundInventory;
            enhance = boundEnhance;
            lamp = boundLamp;
            skills = boundSkills;
            pets = boundPets;
            quests = boundQuests;
            chest = boundChest;
            battle = boundBattle;
            autoSaveInterval = interval;
            timer = interval;
        }

        public SaveReadResult Read()
        {
            if (store == null) return SaveReadResult.Io("store missing");
            var raw = store.Read();
            if (raw.Kind == SaveStoreReadKind.NotFound) return SaveReadResult.NotFound();
            if (raw.Kind == SaveStoreReadKind.IoFailure) return SaveReadResult.Io(raw.Message);
            return SaveValidator.Read(raw.Bytes);
        }

        public void OpenWrites()
        {
            if (gate == SaveWriteGate.Blocked) return;
            gate = SaveWriteGate.Open;
            ticking = true;
            timer = autoSaveInterval;
        }

        public void BlockWrites()
        {
            gate = SaveWriteGate.Blocked;
            ticking = false;
        }

        public void CloseWrites()
        {
            if (gate == SaveWriteGate.Blocked) return;
            gate = SaveWriteGate.Closed;
            ticking = false;
        }

        public void Tick(float deltaTime)
        {
            if (!ticking || gate != SaveWriteGate.Open) return;
            timer -= deltaTime;
            if (timer > 0f) return;
            timer = autoSaveInterval;
            Save();
        }

        public SaveWriteResult Save()
        {
            if (gate != SaveWriteGate.Open)
                return new SaveWriteResult(gate == SaveWriteGate.Blocked ? SaveWriteStatus.Blocked : SaveWriteStatus.Closed);
            if (store == null) return new SaveWriteResult(SaveWriteStatus.IoFailure, "store missing");

            var data = Capture();
            data.Stamp(clock != null ? clock.UtcNow : DateTime.UtcNow);
            var check = SaveValidator.Read(SaveValidator.WriteBytes(data));
            if (check.Status != SaveReadStatus.Loaded)
                return new SaveWriteResult(SaveWriteStatus.InvalidCapture, check.Message);
            if (!store.Write(SaveValidator.WriteBytes(data)))
                return new SaveWriteResult(SaveWriteStatus.IoFailure, "write failed");
            return new SaveWriteResult(SaveWriteStatus.Succeeded);
        }

        public SaveData Capture()
        {
            var data = SaveData.NewGame();
            if (wallet != null)
            {
                data.gold = wallet.Gold;
                data.grain = wallet.Grain;
                data.wick = wallet.Wick;
                data.gem = wallet.Gem;
                data.shard = wallet.Shard;
            }
            if (inventory != null)
            {
                data.gearStock = inventory.CountsSnapshot();
                data.gearCodex = inventory.CodexSnapshot();
            }
            if (enhance != null) data.enhanceFailStreak = enhance.Snapshot();
            if (stats != null)
            {
                data.level = stats.Level;
                data.evolutionStage = stats.EvolutionStage;
                data.upgradeLevels = stats.UpgradeLevelsCopy();
                data.currentHp = stats.CurrentHp;
                data.exp = stats.Exp;
            }
            if (skills != null)
            {
                data.skillLevel = skills.LevelSnapshot();
                data.skillEquipped = skills.EquippedSnapshot();
            }
            if (pets != null)
            {
                data.petLevel = pets.LevelSnapshot();
                data.petEquipped = pets.EquippedId;
            }
            if (quests != null)
            {
                data.questProgress = quests.ProgressSnapshot();
                data.questRounds = quests.RoundsSnapshot();
            }
            if (chest != null) data.idleBankedSeconds = chest.BankedSeconds;
            if (battle != null)
            {
                data.world = battle.World;
                data.stage = battle.Stage;
                data.kills = battle.Kills;
            }
            if (lamp != null)
            {
                data.lampLevel = lamp.LampLevel;
                data.offeringsSincePity = lamp.OfferingsSincePity;
                data.totalOfferings = lamp.TotalOfferings;
            }
            if (loadout != null) data.gear = loadout.Snapshot();
            return data;
        }

        public void Apply(SaveData data)
        {
            if (data == null) return;
            wallet?.LoadFrom(data.gold, data.grain, data.wick, data.gem, data.shard);
            stats?.LoadFrom(data.level, data.evolutionStage, data.upgradeLevels, data.currentHp, data.exp);
            loadout?.LoadFrom(data.gear);
            inventory?.LoadFrom(data.gearStock, data.gearCodex);
            enhance?.LoadFrom(data.enhanceFailStreak);
            lamp?.LoadFrom(data.lampLevel, data.offeringsSincePity, data.totalOfferings);
            skills?.LoadFrom(null, data.skillLevel, data.skillEquipped);
            pets?.LoadFrom(null, data.petLevel, data.petEquipped);
            quests?.LoadFrom(data.questProgress, data.questRounds);
            chest?.LoadFrom(data.idleBankedSeconds);
            battle?.LoadFrom(data.world, data.stage, data.kills);
        }

        public OfflineResult GrantOffline(SaveData data)
        {
            if (data == null || clock == null) return default;
            double elapsed = data.ElapsedSecondsSince(clock.UtcNow);
            var result = OfflineRewards.Compute(data.world, data.stage, elapsed);
            if (!result.Any) return result;
            chest?.Bank(elapsed);
            OfflineGranted?.Invoke(result);
            return result;
        }
    }
}
