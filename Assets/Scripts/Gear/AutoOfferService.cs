using System;
using UnityEngine;

namespace GrowNa.Gear
{
    public class AutoOfferService : MonoBehaviour
    {
        [SerializeField] GearTier keepTier = GearTier.Rare;
        [SerializeField] int batchSize = 1;
        [SerializeField] float interval = 0.25f;

        LampService lamp;
        Inventory inventory;
        float timer;
        bool running;

        public event Action Changed;
        public event Action<GearItem> Kept;

        public GearTier KeepTier => keepTier;
        public int BatchSize => batchSize;
        public bool Running => running;
        public int SoldCount { get; private set; }
        public double SoldGold { get; private set; }
        public double BatchCost => LampTable.OfferCost(batchSize);

        public void Bind(LampService boundLamp, Inventory boundInventory)
        {
            lamp = boundLamp;
            inventory = boundInventory;
        }

        public void StepKeepTier(int delta)
        {
            int next = Mathf.Clamp((int)keepTier + delta, 0, GearTable.TierCount - 1);
            if (next == (int)keepTier) return;
            keepTier = (GearTier)next;
            Changed?.Invoke();
        }

        public void StepBatchSize(int delta)
        {
            int next = Mathf.Clamp(batchSize + delta, 1, LampTable.MaxAutoBatch);
            if (next == batchSize) return;
            batchSize = next;
            Changed?.Invoke();
        }

        public void Toggle()
        {
            if (running) StopAuto();
            else StartAuto();
        }

        public void StartAuto()
        {
            if (running) return;
            running = true;
            timer = 0f;
            SoldCount = 0;
            SoldGold = 0;
            Changed?.Invoke();
        }

        public void StopAuto()
        {
            if (!running) return;
            running = false;
            Changed?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            if (!running) return;
            timer -= deltaTime;
            if (timer > 0f) return;
            timer = interval;
            Pulse();
        }

        void Pulse()
        {
            if (lamp == null || inventory == null || !lamp.CanOffer(batchSize)) { StopAuto(); return; }

            var results = lamp.Offer(batchSize);
            if (results == null) { StopAuto(); return; }

            var kept = GearItem.Empty;
            foreach (var item in results)
            {
                if (item.tier < keepTier)
                {
                    SoldGold += inventory.Sell(item);
                    SoldCount++;
                }
                else if (!kept.owned) kept = item;
                else inventory.Acquire(item);
            }

            Changed?.Invoke();
            if (!kept.owned) return;

            StopAuto();
            Kept?.Invoke(kept);
        }
    }
}
