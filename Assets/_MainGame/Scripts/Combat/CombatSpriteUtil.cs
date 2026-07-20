using UnityEngine;

namespace ClockWork.Game
{
    public static class CombatSpriteUtil
    {
        public static Sprite CreateRectSprite(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;

            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
