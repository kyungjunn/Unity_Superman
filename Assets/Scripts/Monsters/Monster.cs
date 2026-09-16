using System;
using UnityEngine;

namespace GrowNa.Monsters
{
    public class Monster : MonoBehaviour
    {
        public event Action<Monster> Died;
        public event Action<Monster> AttackRequested;

        [SerializeField] SpriteRenderer body;
        [SerializeField] Transform hpFill;
        [SerializeField] SpriteRenderer hpFillRenderer;

        double maxHp;
        double currentHp;
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
        public int Rank { get; private set; }
        public bool InRangeOf(Vector3 p) => Mathf.Abs(transform.position.x - p.x) <= attackRange;

        public void Bind(SpriteRenderer bodyRenderer, Transform fill, SpriteRenderer fillRenderer)
        {
            body = bodyRenderer;
            hpFill = fill;
            hpFillRenderer = fillRenderer;
        }

        public void Spawn(Sprite sprite, double hp, double attack, bool boss, int rank, Transform playerTransform)
        {
            body.sprite = sprite;
            body.flipX = true;
            maxHp = hp;
            currentHp = hp;
            Attack = attack;
            IsBoss = boss;
            Rank = rank;
            target = playerTransform;
            dead = false;
            attackTimer = 0.6f;
            transform.localScale = boss ? Vector3.one * 1.55f : Vector3.one;
            baseColor = boss ? new Color(1f, 0.78f, 0.78f) : Color.white;
            body.color = baseColor;
            gameObject.SetActive(true);
            UpdateHpBar();
        }

        public void Tick(float deltaTime)
        {
            if (dead || target == null) return;
            if (Mathf.Abs(transform.position.x - target.position.x) > attackRange) return;
            attackTimer -= deltaTime;
            if (attackTimer > 0f) return;
            attackTimer = attackInterval;
            AttackRequested?.Invoke(this);
        }

        void Update()
        {
            if (dead || body == null) return;
            if (flashTimer <= 0f) return;
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f) body.color = baseColor;
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
