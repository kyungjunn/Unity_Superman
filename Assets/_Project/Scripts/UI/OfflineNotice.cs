using GrowNa.Battle;
using GrowNa.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class OfflineNotice : MonoBehaviour
    {
        [SerializeField] GameObject toast;
        [SerializeField] Text label;
        [SerializeField] float duration = 4f;

        float timer;

        public void Bind(GameObject toastRoot, Text toastLabel)
        {
            toast = toastRoot;
            label = toastLabel;
        }

        void Start()
        {
            if (SaveService.Instance != null) SaveService.Instance.OfflineGranted += Show;
        }

        void OnDestroy()
        {
            if (SaveService.Instance != null) SaveService.Instance.OfflineGranted -= Show;
        }

        void Show(OfflineResult result)
        {
            if (toast == null || label == null) return;
            string capped = result.capped ? " (최대 8시간)" : "";
            label.text = $"자리를 비운 {BigNum.Duration(result.seconds)}{capped}\n" +
                         $"골드 {BigNum.Format(result.gold)} · 낟알 {BigNum.Format(result.grain)}";
            toast.SetActive(true);
            timer = duration;
        }

        void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.deltaTime;
            if (timer <= 0f && toast != null) toast.SetActive(false);
        }
    }
}
