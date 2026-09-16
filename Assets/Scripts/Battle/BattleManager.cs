using System;
using System.Collections.Generic;
using GrowNa.Core;
using GrowNa.Monsters;
using UnityEngine;

namespace GrowNa.Battle
{
    public class BattleManager : MonoBehaviour
    {
        [SerializeField] Transform playerTransform;
        [SerializeField] Transform monsterRoot;
        [SerializeField] Sprite[] monsterSprites;
        [SerializeField] Sprite hpBarSprite;
        [SerializeField] float spawnX = 6.4f;
        [SerializeField] float groundY = 0.4f;
        [SerializeField] float waveSpacing = 0.95f;
        [SerializeField] float travelSpeed = 4.6f;

        public event Action StageChanged;
        public event Action<int> KillProgressChanged;
        public event Action<double> SkillDamageDealt;
        public event Action PlayerAttacked;
        public event Action<bool> RunningChanged;
        public event Action<float> Scrolled;
        public event Action<Vector3, double, bool, bool> DamageShown;

        readonly List<Monster> wave = new List<Monster>();

        PlayerStats stats;
        Wallet wallet;
        QuestService quests;
        IGameRandom rng;
        MonsterFactory factory;

        int world = 1;
        int stage = 1;
        int kills;
        bool bossPhase;
        float bossTimer;
        float playerAttackTimer;
        float respawnTimer;
        bool begun;

        public int World => world;
        public int Stage => stage;
        public int Kills => kills;
        public int KillsRequired => StageTable.MonstersPerStage;
        public bool BossPhase => bossPhase;
        public float BossTimeLeft => bossTimer;
        public int WaveCount => wave.Count;
        public string StageLabel => $"{StageTable.WorldName(world)} {world}-{stage}";
        public int CurrentRank => bossPhase ? StageTable.MonsterRanks - 1 : StageTable.RankOf(kills);
        public string CurrentRankLabel => bossPhase
            ? "보스"
            : $"{StageTable.RankName(CurrentRank)} ({CurrentRank + 1}/{StageTable.MonsterRanks}단계)";
        public bool HasLiveTarget => NearestTarget() != null;

        public void Bind(PlayerStats boundStats, Wallet boundWallet, QuestService boundQuests, IGameRandom boundRandom, MonsterFactory boundFactory)
        {
            stats = boundStats;
            wallet = boundWallet;
            quests = boundQuests;
            rng = boundRandom;
            factory = boundFactory;
        }

        Monster NearestTarget()
        {
            if (respawnTimer > 0f || playerTransform == null) return null;

            Monster best = null;
            float bestDistance = float.MaxValue;
            foreach (var m in wave)
            {
                if (m == null || m.IsDead || !m.InRangeOf(playerTransform.position)) continue;
                float distance = Mathf.Abs(m.transform.position.x - playerTransform.position.x);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = m;
            }
            return best;
        }

        public bool TrySkillStrike(double multiplier)
        {
            var victim = NearestTarget();
            if (stats == null || victim == null || multiplier <= 0) return false;

            double dmg = stats.Snapshot.attack * multiplier;
            Vector3 popupAt = victim.transform.position + Vector3.up * 1.0f;
            PlayerAttacked?.Invoke();
            victim.TakeDamage(dmg);
            DamageShown?.Invoke(popupAt, dmg, true, false);
            SkillDamageDealt?.Invoke(dmg);
            return true;
        }

        public void LoadFrom(int savedWorld, int savedStage, int savedKills)
        {
            world = Math.Max(1, savedWorld);
            stage = Math.Clamp(savedStage, 1, 10);
            kills = Math.Clamp(savedKills, 0, StageTable.MonstersPerStage);
            bossPhase = false;
            DespawnAll();
            begun = false;
            KillProgressChanged?.Invoke(kills);
            StageChanged?.Invoke();
        }

        public void Begin()
        {
            if (begun) return;
            begun = true;
            SpawnNext(0.3f);
        }

        public void Tick(float deltaTime)
        {
            if (stats == null) return;

            if (stats.IsDead)
            {
                stats.ReviveFull();
                kills = 0;
                bossPhase = false;
                DespawnAll();
                SpawnNext(1.0f);
                KillProgressChanged?.Invoke(kills);
                return;
            }

            stats.Recover(stats.MaxHp * 0.02 * deltaTime);

            if (respawnTimer > 0f)
            {
                Travel(deltaTime);
                respawnTimer -= deltaTime;
                if (respawnTimer <= 0f) SpawnWave();
                return;
            }

            if (bossPhase && wave.Count > 0)
            {
                bossTimer -= deltaTime;
                if (bossTimer <= 0f)
                {
                    bossPhase = false;
                    kills = 0;
                    DespawnAll();
                    SpawnNext(0.8f);
                    KillProgressChanged?.Invoke(kills);
                    StageChanged?.Invoke();
                    return;
                }
            }

            foreach (var monster in wave)
                if (monster != null && !monster.IsDead) monster.Tick(deltaTime);

            var victim = NearestTarget();
            if (victim == null)
            {
                Travel(deltaTime);
                return;
            }

            RunningChanged?.Invoke(false);
            playerAttackTimer -= deltaTime;
            if (playerAttackTimer > 0f) return;

            playerAttackTimer = (float)(1.0 / Math.Max(0.1, stats.AttackSpeed));
            PlayerAttacked?.Invoke();
            double dmg = stats.RollDamage(out bool crit);
            Vector3 popupAt = victim.transform.position + Vector3.up * 0.7f;
            victim.TakeDamage(dmg);
            DamageShown?.Invoke(popupAt, dmg, crit, false);
        }

        public void MonsterHitsPlayer(Monster attacker)
        {
            if (stats == null || attacker == null || attacker.IsDead) return;
            stats.TakeDamage(attacker.Attack);
            DamageShown?.Invoke(playerTransform.position + Vector3.up * 0.9f, attacker.Attack, false, true);
        }

        void Travel(float dt)
        {
            float distance = travelSpeed * dt;
            Scrolled?.Invoke(distance);
            foreach (var monster in wave)
            {
                if (monster == null) continue;
                monster.transform.position += Vector3.left * distance;
            }
            RunningChanged?.Invoke(true);
        }

        void SpawnNext(float delay) => respawnTimer = delay;

        void SpawnWave()
        {
            bool boss = kills >= StageTable.MonstersPerStage;
            bossPhase = boss;
            if (boss) bossTimer = StageTable.BossTimeLimit;

            int rank = boss ? StageTable.MonsterRanks - 1 : StageTable.RankOf(kills);
            int count = boss ? 1 : StageTable.WaveSize(kills, rng != null ? rng.Value01() : 0);

            double hp = StageTable.MonsterHp(world, stage, rank)
                      * (boss ? StageTable.BossHpMultiplier(world, stage) : 1.0);
            double atk = StageTable.MonsterAttack(world, stage, rank) * (boss ? 1.6 : 1.0);

            Sprite sprite = monsterSprites != null && monsterSprites.Length > 0
                ? monsterSprites[rank % monsterSprites.Length]
                : null;

            for (int i = 0; i < count; i++)
            {
                var monster = factory != null ? factory.Create(monsterRoot, hpBarSprite) : null;
                if (monster == null) continue;
                float originX = playerTransform != null ? playerTransform.position.x : 0f;
                monster.transform.position = new Vector3(originX + spawnX + i * waveSpacing, groundY + 0.55f, 0f);
                monster.Died += OnMonsterDied;
                monster.AttackRequested += MonsterHitsPlayer;
                monster.Spawn(sprite, hp, atk, boss, rank, playerTransform);
                wave.Add(monster);
            }

            StageChanged?.Invoke();
        }

        void OnMonsterDied(Monster m)
        {
            m.Died -= OnMonsterDied;
            m.AttackRequested -= MonsterHitsPlayer;
            wave.Remove(m);

            StageReward reward = m.IsBoss
                ? StageTable.BossReward(world, stage, rng != null ? rng.Value01() : 0)
                : StageTable.KillReward(world, stage, m.Rank, rng != null ? rng.Value01() : 0);

            if (wallet != null)
            {
                wallet.Add(CurrencyKind.Gold, reward.gold);
                wallet.Add(CurrencyKind.Grain, reward.grain);
                wallet.Add(CurrencyKind.Wick, reward.wick);
            }

            stats?.AddExp(reward.exp);
            quests?.ReportKill(m.IsBoss);

            if (m.IsBoss)
            {
                bossPhase = false;
                kills = 0;
                stage++;
                if (stage > 10) { stage = 1; world++; }
            }
            else
            {
                kills = Math.Min(StageTable.MonstersPerStage, kills + 1);
            }

            Destroy(m.gameObject, 0.05f);
            KillProgressChanged?.Invoke(kills);
            StageChanged?.Invoke();

            if (wave.Count == 0) SpawnNext(0.7f);
        }

        void DespawnAll()
        {
            foreach (var m in wave)
            {
                if (m == null) continue;
                m.Died -= OnMonsterDied;
                m.AttackRequested -= MonsterHitsPlayer;
                Destroy(m.gameObject);
            }
            wave.Clear();
        }
    }
}
