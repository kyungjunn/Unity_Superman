using System;
using UnityEngine;

namespace GrowNa.Battle
{
    public class Monster : MonoBehaviour
    {
        public event Action<Monster> Died;

        [SerializeField] SpriteRenderer body;
        [SerializeField] Transform hpFill;
        [SerializeField] SpriteRenderer hpFillRenderer;

        double maxHp;
        double currentHp;
        float moveSpeed = 1.1f;
        float attackRange = 1.15f;
        float attackInterval = 1.6f;
        float attackTimer;
        Transform target;
        bool dead;
        Color baseColor = Color.white;
        float flashTimer;

        public double Attack { get; private set; }
        public bool IsBoss { get; private set; }
        public bool IsDead => dead;
        public bool InRangeOf(Vector3 p) => Mathf.Abs(transform.position.x - p.x) <= attackRange;

        public void Bind(SpriteRenderer bodyRenderer, Transform fill, SpriteRenderer fillRenderer)
        {
            body = bodyRenderer;
            hpFill = fill;
            hpFillRenderer = fillRenderer;
        }

        public void Spawn(Sprite sprite, double hp, double attack, bool boss, Transform playerTransform)
        {
            body.sprite = sprite;
            maxHp = hp;
            currentHp = hp;
            Attack = attack;
            IsBoss = boss;
            target = playerTransform;
            dead = false;
            attackTimer = 0.6f;
            transform.localScale = boss ? Vector3.one * 1.55f : Vector3.one;
            baseColor = boss ? new Color(1f, 0.78f, 0.78f) : Color.white;
            body.color = baseColor;
            gameObject.SetActive(true);
            UpdateHpBar();
        }

        void Update()
        {
            if (dead || target == null) return;

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f) body.color = baseColor;
            }

            float dx = transform.position.x - target.position.x;
            if (Mathf.Abs(dx) > attackRange)
            {
                transform.position += Vector3.left * (moveSpeed * Time.deltaTime);
                return;
            }

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = attackInterval;
                BattleManager.Instance?.MonsterHitsPlayer(this);
            }
        }

        public void TakeDamage(double amount)
        {
            if (dead) return;
            currentHp -= amount;
            body.color = Color.white * 1.4f;
            flashTimer = 0.07f;
            UpdateHpBar();
            if (currentHp <= 0)
            {
                dead = true;
                Died?.Invoke(this);
                gameObject.SetActive(false);
            }
        }

        void UpdateHpBar()
        {
            if (hpFill == null) return;
            float ratio = maxHp <= 0 ? 0f : Mathf.Clamp01((float)(currentHp / maxHp));
            hpFill.localScale = new Vector3(ratio, 1f, 1f);
            if (hpFillRenderer != null)
                hpFillRenderer.color = ratio > 0.5f ? new Color(0.42f, 0.78f, 0.36f)
                                     : ratio > 0.2f ? new Color(0.92f, 0.74f, 0.24f)
                                                    : new Color(0.86f, 0.28f, 0.28f);
        }
    }
}
