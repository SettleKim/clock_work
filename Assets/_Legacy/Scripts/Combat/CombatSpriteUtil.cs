using UnityEngine;

public static class CombatSpriteUtil
{
    public static Sprite CreateRectSprite(int width, int height, Color color)
    {
        var texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f);
    }

    public static Sprite CreateGroundSprite(int width, int height)
    {
        var texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;

        Color top = new Color(0.55f, 0.48f, 0.36f);
        Color body = new Color(0.42f, 0.35f, 0.28f);
        Color edge = new Color(0.28f, 0.22f, 0.18f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (y >= height - 2)
                    texture.SetPixel(x, y, top);
                else if (x == 0 || x == width - 1 || y == 0)
                    texture.SetPixel(x, y, edge);
                else
                    texture.SetPixel(x, y, body);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f);
    }

    public static Sprite CreateEnemySprite()
    {
        const int width = 20;
        const int height = 18;
        var texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;

        Color shell = new Color(0.72f, 0.28f, 0.22f);
        Color core = new Color(0.95f, 0.55f, 0.18f);
        Color eye = new Color(0.08f, 0.08f, 0.1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool body = x >= 3 && x <= 16 && y >= 2 && y <= 14;
                bool head = x >= 6 && x <= 13 && y >= 10 && y <= 16;
                bool eyeL = x == 8 && y == 13;
                bool eyeR = x == 11 && y == 13;

                if (eyeL || eyeR)
                    texture.SetPixel(x, y, eye);
                else if (head)
                    texture.SetPixel(x, y, core);
                else if (body)
                    texture.SetPixel(x, y, shell);
                else
                    texture.SetPixel(x, y, Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.2f), 16f);
    }

    public static Sprite CreateSwordWaveSprite()
    {
        const int width = 24;
        const int height = 10;
        var texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;

        Color edge = new Color(0.85f, 0.95f, 1f, 0.35f);
        Color core = new Color(0.55f, 0.85f, 1f, 0.95f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                float profile = 1f - Mathf.Abs((y - (height - 1) * 0.5f) / (height * 0.5f));
                profile = Mathf.Clamp01(profile + t * 0.35f);

                if (profile > 0.55f)
                    texture.SetPixel(x, y, core);
                else if (profile > 0.25f)
                    texture.SetPixel(x, y, edge);
                else
                    texture.SetPixel(x, y, Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f);
    }
}
