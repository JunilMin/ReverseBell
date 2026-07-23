using UnityEngine;

namespace ReverseBell.Prototype
{
    public static class PrototypeVisuals
    {
        private static Sprite whiteSprite;

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
    }
}

