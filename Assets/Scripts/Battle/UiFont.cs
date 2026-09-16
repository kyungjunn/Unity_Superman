using UnityEngine;

namespace GrowNa.Battle
{
    public class UiFont : MonoBehaviour
    {
        Font cached;

        public Font Get()
        {
            if (cached != null) return cached;
            cached = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans KR", "Arial Unicode MS", "Arial" }, 32);
            if (cached == null) cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cached;
        }
    }
}
