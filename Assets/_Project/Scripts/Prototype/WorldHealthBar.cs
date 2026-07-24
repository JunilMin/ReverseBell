using UnityEngine;

namespace ReverseBell.Prototype
{
    public sealed class WorldHealthBar : MonoBehaviour
    {
        private Transform fill;
        private float fullWidth;
        private int currentHealth;
        private int maximumHealth = 1;

        public void Initialize(float yOffset, float width, Color fillColor)
        {
            fullWidth = width;
            var root = new GameObject("Health Bar").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0f, yOffset, 0f);

            CreateBarPart(
                root,
                "Health Bar Border",
                new Vector3(width + 0.12f, 0.22f, 1f),
                new Color(0.02f, 0.025f, 0.035f),
                20);
            CreateBarPart(
                root,
                "Health Bar Background",
                new Vector3(width, 0.12f, 1f),
                new Color(0.2f, 0.04f, 0.05f),
                21);

            fill = CreateBarPart(
                root,
                "Health Bar Fill",
                new Vector3(width, 0.12f, 1f),
                fillColor,
                22).transform;
        }

        public void SetHealth(int current, int maximum)
        {
            currentHealth = Mathf.Max(0, current);
            maximumHealth = Mathf.Max(1, maximum);
            float ratio = Mathf.Clamp01((float)currentHealth / maximumHealth);
            fill.localScale = new Vector3(fullWidth * ratio, 0.12f, 1f);
            fill.localPosition =
                new Vector3((-fullWidth + (fullWidth * ratio)) * 0.5f, 0f, 0f);
        }

        private static GameObject CreateBarPart(
            Transform parent,
            string objectName,
            Vector3 scale,
            Color color,
            int sortingOrder)
        {
            var part = new GameObject(objectName);
            part.transform.SetParent(parent, false);
            part.transform.localScale = scale;
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeVisuals.WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return part;
        }
    }
}
