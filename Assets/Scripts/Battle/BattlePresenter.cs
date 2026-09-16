using GrowNa.Players;
using UnityEngine;

namespace GrowNa.Battle
{
    public class BattlePresenter : MonoBehaviour
    {
        [SerializeField] MapScroller mapScroller;
        [SerializeField] PlayerAnimation playerAnimation;

        BattleManager battle;
        DamagePopupSpawner popups;

        public void Bind(BattleManager boundBattle, DamagePopupSpawner spawner)
        {
            Unbind();
            battle = boundBattle;
            popups = spawner;
            if (battle == null) return;
            battle.PlayerAttacked += OnAttack;
            battle.RunningChanged += OnRunning;
            battle.Scrolled += OnScroll;
            battle.DamageShown += OnDamage;
        }

        void OnDestroy() => Unbind();

        void Unbind()
        {
            if (battle == null) return;
            battle.PlayerAttacked -= OnAttack;
            battle.RunningChanged -= OnRunning;
            battle.Scrolled -= OnScroll;
            battle.DamageShown -= OnDamage;
        }

        void OnAttack() => playerAnimation?.PlayAttack();
        void OnRunning(bool running) => playerAnimation?.SetRunning(running);
        void OnScroll(float distance) => mapScroller?.Scroll(distance);
        void OnDamage(Vector3 at, double amount, bool critical, bool onPlayer)
            => popups?.Spawn(at, amount, critical, onPlayer);
    }
}
