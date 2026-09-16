using GrowNa.Gear;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class QuickOfferButton : MonoBehaviour
    {
        [SerializeField] Button offerButton;
        [SerializeField] OfferResultPanel resultPanel;
        [SerializeField] GameObject toast;
        [SerializeField] Text toastLabel;
        [SerializeField] float toastSeconds = 1.6f;

        LampService lamp;
        Inventory inventory;
        float timer;

        public void Bind(Button button, OfferResultPanel results, GameObject toastRoot, Text toastText)
        {
            offerButton = button;
            resultPanel = results;
            toast = toastRoot;
            toastLabel = toastText;
        }

        public void BindServices(LampService boundLamp, Inventory boundInventory)
        {
            lamp = boundLamp;
            inventory = boundInventory;
        }

        void Start()
        {
            if (offerButton != null) offerButton.onClick.AddListener(Offer);
        }

        void Offer()
        {
            if (lamp == null) return;

            if (!lamp.TryOfferOne(out var item))
            {
                Notify($"낟알이 부족합니다 · {LampTable.OfferCost(1):0}개 필요");
                return;
            }

            if (resultPanel != null) resultPanel.Show(item);
            else inventory?.Acquire(item);
        }

        void Notify(string message)
        {
            if (toast == null || toastLabel == null) return;
            toastLabel.text = message;
            toast.SetActive(true);
            timer = toastSeconds;
        }

        void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.deltaTime;
            if (timer <= 0f && toast != null) toast.SetActive(false);
        }
    }
}
