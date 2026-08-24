using UnityEditor;

namespace GrowNa.EditorTools
{
    public static class PlayModeToggle
    {
        [MenuItem("GrowNa/Enter Play Mode")]
        public static void Enter() => EditorApplication.isPlaying = true;

        [MenuItem("GrowNa/Exit Play Mode")]
        public static void Exit() => EditorApplication.isPlaying = false;
    }
}
