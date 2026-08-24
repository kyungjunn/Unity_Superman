using UnityEngine;

namespace GrowNa.Battle
{
    public static class MonsterFactory
    {
        public static Monster Create(Transform parent, Sprite barSprite)
        {
            var root = new GameObject("Monster");
            root.transform.SetParent(parent, false);

            var body = root.AddComponent<SpriteRenderer>();
            body.sortingOrder = 12;

            var barRoot = new GameObject("HpBar");
            barRoot.transform.SetParent(root.transform, false);
            barRoot.transform.localPosition = new Vector3(0f, 0.95f, 0f);

            var back = new GameObject("Back").AddComponent<SpriteRenderer>();
            back.transform.SetParent(barRoot.transform, false);
            back.sprite = barSprite;
            back.color = new Color(0.1f, 0.09f, 0.08f, 0.85f);
            back.sortingOrder = 14;
            back.drawMode = SpriteDrawMode.Sliced;
            back.size = new Vector2(1.1f, 0.16f);

            var fillPivot = new GameObject("FillPivot").transform;
            fillPivot.SetParent(barRoot.transform, false);
            fillPivot.localPosition = new Vector3(-0.55f, 0f, 0f);

            var fill = new GameObject("Fill").AddComponent<SpriteRenderer>();
            fill.transform.SetParent(fillPivot, false);
            fill.sprite = barSprite;
            fill.sortingOrder = 15;
            fill.drawMode = SpriteDrawMode.Sliced;
            fill.size = new Vector2(1.06f, 0.12f);
            fill.transform.localPosition = new Vector3(0.53f, 0f, 0f);

            var monster = root.AddComponent<Monster>();
            monster.Bind(body, fillPivot, fill);
            return monster;
        }
    }
}
