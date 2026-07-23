using UnityEngine;

namespace ReverseBell.Prototype
{
    public static class PrototypeVisuals
    {
        private static Sprite whiteSprite;
        private static Sprite circleSprite;

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null)
                {
                    return whiteSprite;
                }

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = "Runtime White Texture",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };

                texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                texture.Apply();

                whiteSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 2f, 2f),
                    new Vector2(0.5f, 0.5f),
                    2f);
                whiteSprite.name = "Runtime White Sprite";
                return whiteSprite;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (circleSprite != null)
                {
                    return circleSprite;
                }

                const int size = 32;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "Runtime Circle Texture",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                var pixels = new Color[size * size];
                Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
                float radius = size * 0.46f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), center);
                        pixels[(y * size) + x] = distance <= radius
                            ? Color.white
                            : Color.clear;
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();

                circleSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f),
                    size);
                circleSprite.name = "Runtime Circle Sprite";
                return circleSprite;
            }
        }
    }
}
