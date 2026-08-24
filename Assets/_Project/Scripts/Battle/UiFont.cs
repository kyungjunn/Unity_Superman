using UnityEngine;

namespace GrowNa.Battle
{
    public static class UiFont
    {
        static Font cached;

        public static Font Get()
        {
            if (cached != null) return cached;
            cached = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans KR", "Arial Unicode MS", "Arial" }, 32);
            if (cached == null) cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cached;
        }
    }
}
