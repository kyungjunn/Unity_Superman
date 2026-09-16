using GrowNa.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.EditorTools
{
    public static class UiFactory
    {
        public static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();

        public static GameObject Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static void Place(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
                                 Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = Rect(go);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static void Anchor(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = Rect(go);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static Image Sprite(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = Node(name, parent);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        static Font ResolveFont()
        {
            var fonts = UnityEngine.Object.FindObjectsByType<UiFont>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (fonts.Length > 0) return fonts[0].Get();
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static Text Label(string name, Transform parent, string content, int size,
                                 Color color, TextAnchor align = TextAnchor.MiddleLeft, bool bold = true)
        {
            var go = Node(name, parent);
            var text = go.AddComponent<Text>();
            text.font = ResolveFont();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static GameObject Panel(string name, Transform parent, Sprite frame, Color color, bool interactable)
        {
            var img = Sprite(name, parent, frame, color);
            img.raycastTarget = interactable;
            if (interactable)
            {
                var button = img.gameObject.AddComponent<Button>();
                button.targetGraphic = img;
            }
            return img.gameObject;
        }

        public static Outline Shadow(GameObject go)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            outline.effectDistance = new Vector2(2f, -2f);
            return outline;
        }
    }
}
