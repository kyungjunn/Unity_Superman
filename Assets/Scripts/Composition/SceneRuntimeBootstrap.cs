using GrowNa.Battle;
using GrowNa.Core;
using GrowNa.Gear;
using GrowNa.Monsters;
using GrowNa.Persistence;
using GrowNa.Pet;
using GrowNa.Players;
using GrowNa.Skill;
using GrowNa.UI;
using UnityEngine;

namespace GrowNa.Composition
{
    public class SceneRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] Wallet wallet;
        [SerializeField] PlayerStats stats;
        [SerializeField] UpgradeService upgrades;
        [SerializeField] QuestService quests;
        [SerializeField] Loadout loadout;
        [SerializeField] Inventory inventory;
        [SerializeField] EnhanceService enhance;
        [SerializeField] LampService lamp;
        [SerializeField] AutoOfferService autoOffer;
        [SerializeField] SkillService skills;
        [SerializeField] PetService pets;
        [SerializeField] IdleChestService chest;
        [SerializeField] BattleManager battle;
        [SerializeField] SaveService save;
        [SerializeField] MonsterFactory factory;
        [SerializeField] BattlePresenter presenter;
        [SerializeField] DamagePopupSpawner popups;
        [SerializeField] UiFont fonts;

        ISaveStore store;
        IClock clock;
        IGameRandom rng;
        bool persistenceConfigured;
        BootstrapResult last;
        BootstrapPhase phase = BootstrapPhase.Cold;
        int readCount;
        int initializeCount;

        public BootstrapPhase Phase => phase;
        public BootstrapResult LastResult => last;
        public int ReadCount => readCount;
        public int InitializeCount => initializeCount;
        public void BindCore(Wallet boundWallet, PlayerStats boundStats, UpgradeService boundUpgrades, QuestService boundQuests, SaveService boundSave)
        {
            wallet = boundWallet;
            stats = boundStats;
            upgrades = boundUpgrades;
            quests = boundQuests;
            save = boundSave;
        }

        public void ConfigurePersistence(ISaveStore saveStore, IClock saveClock, IGameRandom gameRandom = null)
        {
            if (phase != BootstrapPhase.Cold) return;
            store = saveStore;
            clock = saveClock;
            rng = gameRandom;
            persistenceConfigured = true;
        }

        void Start() => Initialize();

        public BootstrapResult Initialize()
        {
            initializeCount++;
            if (phase == BootstrapPhase.Initializing)
            {
                last = new BootstrapResult(BootstrapPhase.InProgress, last.Read, last.Failure);
                return last;
            }
            if (phase == BootstrapPhase.Ready || phase == BootstrapPhase.Failed || phase == BootstrapPhase.Stopped)
                return last;

            phase = BootstrapPhase.Initializing;
            if (!persistenceConfigured)
            {
                store ??= new FileSaveStore();
                clock ??= new SystemClock();
                rng ??= new SystemGameRandom();
                persistenceConfigured = true;
            }

            if (wallet == null || stats == null || save == null)
                return Fail("missing required refs");

            if (Application.isPlaying)
            {
                int siblings = 0;
                foreach (var root in FindObjectsByType<SceneRuntimeBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (root != null && root.gameObject.scene == gameObject.scene) siblings++;
                if (siblings != 1)
                    return Fail("bootstrap count != 1");
            }

            stats.BindRandom(rng);
            upgrades?.Bind(wallet, stats);
            quests?.Bind(wallet);
            loadout?.Bind(stats);
            inventory?.Bind(wallet, stats, loadout, rng);
            enhance?.Bind(wallet, loadout, rng);
            lamp?.Bind(wallet, stats, quests, rng);
            autoOffer?.Bind(lamp, inventory);
            skills?.Bind(wallet, quests, battle, rng);
            pets?.Bind(wallet, stats, quests, rng);
            chest?.Bind(wallet, battle);
            factory ??= GetComponent<MonsterFactory>();
            presenter ??= GetComponent<BattlePresenter>();
            popups ??= GetComponent<DamagePopupSpawner>();
            fonts ??= GetComponent<UiFont>();
            popups?.Bind(fonts, rng);
            battle?.Bind(stats, wallet, quests, rng, factory);
            BindPresenter();
            save.Bind(store, clock, wallet, stats, loadout, inventory, enhance, lamp, skills, pets, quests, chest, battle);

            var read = save.Read();
            readCount++;
            if (!read.CanProceed)
            {
                save.BlockWrites();
                return Fail(read.ErrorCode, read);
            }

            SaveData data = read.Status == SaveReadStatus.Loaded ? read.Data : SaveData.NewGame();
            save.Apply(data);
            stats.EnsureInitialized();
            stats.ClampHpToMax();
            save.GrantOffline(read.Status == SaveReadStatus.Loaded ? data : null);
            save.OpenWrites();
            loadout?.PushDerived();
            inventory?.PushDerived();
            pets?.PushDerived();
            BindViews();
            battle?.Begin();

            phase = BootstrapPhase.Ready;
            last = new BootstrapResult(BootstrapPhase.Ready, read);
            Debug.Log("[GrowNa] bootstrap Ready");
            return last;
        }

        public void Tick(float deltaTime)
        {
            if (phase != BootstrapPhase.Ready) return;
            battle?.Tick(deltaTime);
            skills?.Tick(deltaTime);
            autoOffer?.Tick(deltaTime);
            chest?.Tick(deltaTime);
            save?.Tick(deltaTime);
        }

        public Wallet Wallet => wallet;
        public PlayerStats Stats => stats;
        public UpgradeService Upgrades => upgrades;
        public QuestService Quests => quests;
        public Loadout Loadout => loadout;
        public Inventory Inventory => inventory;
        public EnhanceService Enhance => enhance;
        public LampService Lamp => lamp;
        public AutoOfferService AutoOffer => autoOffer;
        public SkillService Skills => skills;
        public PetService Pets => pets;
        public IdleChestService Chest => chest;
        public BattleManager Battle => battle;
        public SaveService Save => save;

        T[] InScene<T>() where T : UnityEngine.Object
        {
            var found = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < found.Length; i++)
            {
                var mb = found[i] as Component;
                if (mb != null && mb.gameObject.scene == gameObject.scene) count++;
            }
            var filtered = new T[count];
            int n = 0;
            for (int i = 0; i < found.Length; i++)
            {
                var mb = found[i] as Component;
                if (mb != null && mb.gameObject.scene == gameObject.scene) filtered[n++] = found[i];
            }
            return filtered;
        }

        void BindPresenter()
        {
            if (presenter == null || battle == null) return;
            presenter.Bind(battle, popups);
        }

        void BindViews()
        {
            foreach (var view in InScene<GrowNa.Players.CharacterRig>())
                view.BindLoadout(loadout);
            foreach (var view in InScene<GrowNa.Players.PlayerHpBar>())
                view.BindStats(stats);
            foreach (var view in InScene<GrowNa.Pet.PetView>())
                view.BindService(pets);
            foreach (var view in InScene<GrowNa.UI.HudBinder>())
                view.BindServices(wallet, stats, battle);
            foreach (var view in InScene<GrowNa.UI.OfflineNotice>())
                view.BindSave(save);
            foreach (var view in InScene<GrowNa.UI.LoadoutView>())
                view.BindServices(loadout);
            foreach (var view in InScene<GrowNa.UI.GearPanel>())
                view.BindServices(wallet, loadout, inventory, enhance);
            foreach (var view in InScene<GrowNa.UI.LampPanel>())
                view.BindServices(wallet, lamp);
            foreach (var view in InScene<GrowNa.UI.UpgradePanel>())
                view.BindServices(wallet, stats, upgrades);
            foreach (var view in InScene<GrowNa.UI.QuestPanel>())
                view.BindServices(quests);
            foreach (var view in InScene<GrowNa.UI.LevelUpPopup>())
                view.BindServices(stats);
            foreach (var view in InScene<GrowNa.UI.AutoOfferPanel>())
                view.BindServices(autoOffer, wallet);
            foreach (var view in InScene<GrowNa.UI.OfferResultPanel>())
                view.BindServices(inventory, loadout, stats, autoOffer);
            foreach (var view in InScene<GrowNa.UI.QuickOfferButton>())
                view.BindServices(lamp, inventory);
            foreach (var view in InScene<GrowNa.UI.IdleChestPanel>())
                view.BindServices(chest);
            foreach (var view in InScene<GrowNa.UI.SkillGachaPanel>())
                view.BindServices(wallet, skills);
            foreach (var view in InScene<GrowNa.UI.SkillEquipPanel>())
                view.BindServices(skills);
            foreach (var view in InScene<GrowNa.UI.SkillBarView>())
                view.BindServices(skills);
            foreach (var view in InScene<GrowNa.UI.PetGachaPanel>())
                view.BindServices(wallet, pets);
            foreach (var view in InScene<GrowNa.UI.PetEquipPanel>())
                view.BindServices(pets);
        }

        void Update() => Tick(Time.deltaTime);

        public void Shutdown()
        {
            if (phase == BootstrapPhase.Stopped) return;
            if (phase == BootstrapPhase.Ready) save?.Save();
            save?.CloseWrites();
            phase = BootstrapPhase.Stopped;
            last = new BootstrapResult(BootstrapPhase.Stopped, last.Read, last.Failure);
        }

        void OnDisable() => Shutdown();

        void OnApplicationPause(bool paused)
        {
            if (paused && phase == BootstrapPhase.Ready) save?.Save();
        }

        void OnApplicationQuit()
        {
            if (phase == BootstrapPhase.Ready) save?.Save();
        }


        BootstrapResult Fail(string reason, SaveReadResult read = default)
        {
            phase = BootstrapPhase.Failed;
            last = new BootstrapResult(BootstrapPhase.Failed, read, reason);
            Debug.LogWarning($"[GrowNa] bootstrap Failed: {reason} {read.JsonPath}");
            return last;
        }
    }
}
