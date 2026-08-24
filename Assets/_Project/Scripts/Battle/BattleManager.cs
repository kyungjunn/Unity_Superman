using System;
using GrowNa.Core;
using UnityEngine;

namespace GrowNa.Battle
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [SerializeField] Transform playerTransform;
        [SerializeField] Transform monsterRoot;
        [SerializeField] Sprite[] monsterSprites;
        [SerializeField] Sprite hpBarSprite;
        [SerializeField] float spawnX = 4.2f;
        [SerializeField] float groundY = -0.25f;

        public event Action StageChanged;
        public event Action<int> KillProgressChanged;

        int world = 1;
        int stage = 1;
        int kills;
        bool bossPhase;
        float bossTimer;
        float playerAttackTimer;
        float respawnTimer;
        Monster current;

        public int World => world;
        public int Stage => stage;
        public int Kills => kills;
        public int KillsRequired => StageTable.MonstersPerStage;
        public bool BossPhase => bossPhase;
        public float BossTimeLeft => bossTimer;
        public string StageLabel => $"{StageTable.WorldName(world)} {world}-{stage}";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void LoadFrom(int savedWorld, int savedStage, int savedKills)
        {
            world = Math.Max(1, savedWorld);
            stage = Math.Clamp(savedStage, 1, 10);
            kills = Math.Clamp(savedKills, 0, StageTable.MonstersPerStage);
            bossPhase = false;
            DespawnCurrent();
            SpawnNext(0.4f);
            KillProgressChanged?.Invoke(kills);
            StageChanged?.Invoke();
        }

        void Start() => SpawnNext(0.3f);

        void Update()
        {
            var stats = PlayerStats.Instance;
            if (stats == null) return;

            if (stats.IsDead)
            {
                stats.ReviveFull();
                kills = 0;
                bossPhase = false;
                DespawnCurrent();
                SpawnNext(1.0f);
                KillProgressChanged?.Invoke(kills);
                return;
            }

            stats.Recover(stats.MaxHp * 0.02 * Time.deltaTime);

            if (respawnTimer > 0f)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f) SpawnMonster();
                return;
            }

            if (bossPhase && current != null)
            {
                bossTimer -= Time.deltaTime;
                if (bossTimer <= 0f)
                {
                    bossPhase = false;
                    kills = 0;
                    DespawnCurrent();
                    SpawnNext(0.8f);
                    KillProgressChanged?.Invoke(kills);
                    StageChanged?.Invoke();
                    return;
                }
            }

            if (current == null || current.IsDead) return;
            if (!current.InRangeOf(playerTransform.position)) return;

            playerAttackTimer -= Time.deltaTime;
            if (playerAttackTimer > 0f) return;

            playerAttackTimer = (float)(1.0 / Math.Max(0.1, stats.AttackSpeed));
            double dmg = stats.RollDamage(out bool crit);
            // 처치타면 TakeDamage 안에서 Died -> OnMonsterDied 가 돌면서 current 가 null 이 된다.
            // 팝업 위치는 반드시 때리기 전에 잡아둔다.
            Vector3 popupAt = current.transform.position + Vector3.up * 0.7f;
            current.TakeDamage(dmg);
            DamagePopup.Spawn(popupAt, dmg, crit, false);
        }

        public void MonsterHitsPlayer(Monster attacker)
        {
            var stats = PlayerStats.Instance;
            if (stats == null || attacker == null || attacker.IsDead) return;
            stats.TakeDamage(attacker.Attack);
            DamagePopup.Spawn(playerTransform.position + Vector3.up * 0.9f, attacker.Attack, false, true);
        }

        void SpawnNext(float delay) => respawnTimer = delay;

        void SpawnMonster()
        {
            bool boss = kills >= StageTable.MonstersPerStage;
            bossPhase = boss;
            if (boss) bossTimer = StageTable.BossTimeLimit;

            double hp = StageTable.MonsterHp(world, stage) * (boss ? StageTable.BossHpMultiplier : 1.0);
            double atk = StageTable.MonsterAttack(world, stage) * (boss ? 1.6 : 1.0);
            Sprite sprite = monsterSprites != null && monsterSprites.Length > 0
                ? monsterSprites[UnityEngine.Random.Range(0, monsterSprites.Length)]
                : null;

            current = MonsterFactory.Create(monsterRoot, hpBarSprite);
            current.transform.position = new Vector3(spawnX, groundY + 0.55f, 0f);
            current.Died += OnMonsterDied;
            current.Spawn(sprite, hp, atk, boss, playerTransform);
            StageChanged?.Invoke();
        }

        void OnMonsterDied(Monster m)
        {
            m.Died -= OnMonsterDied;
            var wallet = Wallet.Instance;
            StageReward reward = m.IsBoss ? StageTable.BossReward(world, stage) : StageTable.KillReward(world, stage);
            if (wallet != null)
            {
                wallet.Add(CurrencyKind.Gold, reward.gold);
                wallet.Add(CurrencyKind.Grain, reward.grain);
                wallet.Add(CurrencyKind.Wick, reward.wick);
            }

            if (m.IsBoss)
            {
                bossPhase = false;
                kills = 0;
                stage++;
                if (stage > 10) { stage = 1; world++; }
                PlayerStats.Instance?.GrantLevel();
            }
            else
            {
                kills++;
            }

            Destroy(m.gameObject, 0.05f);
            current = null;
            KillProgressChanged?.Invoke(kills);
            StageChanged?.Invoke();
            SpawnNext(0.45f);
        }

        void DespawnCurrent()
        {
            if (current == null) return;
            current.Died -= OnMonsterDied;
            Destroy(current.gameObject);
            current = null;
        }
    }
}
