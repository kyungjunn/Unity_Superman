using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class MenuStub : MonoBehaviour
    {
        [SerializeField] string menuName = "메뉴";
        [SerializeField] GameObject toast;
        [SerializeField] Text toastLabel;

        float hideTimer;

        public void Setup(string label, GameObject toastRoot, Text toastText)
        {
            menuName = label;
            toast = toastRoot;
            toastLabel = toastText;
        }

        void Start()
        {
            var button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(Show);
        }

        public void Show()
        {
            if (toast == null || toastLabel == null)
            {
                Debug.Log($"[GrowNa] {menuName} 화면은 아직 준비 중입니다.");
                return;
            }
            toastLabel.text = $"{menuName} — 준비 중";
            toast.SetActive(true);
            hideTimer = 1.4f;
        }

        void Update()
        {
            if (hideTimer <= 0f) return;
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f && toast != null) toast.SetActive(false);
        }
    }
}
