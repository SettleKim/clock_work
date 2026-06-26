using UnityEngine;

public static class GearbotMotionLoader
{
    const string FrameRoot = "Gearbot/motion_frames/";
    const float PixelsPerUnit = 72f;

    public static Sprite[] LoadWalk() => LoadSequence("gearbot_walk", 6);
    public static Sprite[] LoadDash() => LoadSequence("gearbot_dash", 4);
    public static Sprite[] LoadGuard() => LoadSequence("gearbot_guard", 4);
    public static Sprite[] LoadHit() => LoadSequence("gearbot_hit", 1);

    public static Sprite LoadFrame(string resourcePath) => LoadSprite(resourcePath);

    static Sprite[] LoadSequence(string prefix, int count)
    {
        var sprites = new Sprite[count];
        int loaded = 0;

        for (int i = 0; i < count; i++)
        {
            string path = FrameRoot + $"{prefix}_{i + 1:00}";
            sprites[i] = LoadSprite(path);
            if (sprites[i] != null)
                loaded++;
        }

        if (loaded == 0)
            Debug.LogWarning($"GearbotMotionLoader: no sprites loaded for {prefix}.");
        else if (loaded < count)
            Debug.LogWarning($"GearbotMotionLoader: partial load for {prefix} ({loaded}/{count}).");

        return sprites;
    }

    static Sprite LoadSprite(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            Debug.LogWarning("GearbotMotionLoader: empty resource path.");
            return null;
        }

        string normalizedPath = resourcePath.Trim();
        Texture2D texture = Resources.Load<Texture2D>(normalizedPath);
        if (texture == null)
        {
            if (!normalizedPath.StartsWith(FrameRoot))
            {
                string fallbackPath = FrameRoot + normalizedPath;
                texture = Resources.Load<Texture2D>(fallbackPath);
                if (texture != null)
                    normalizedPath = fallbackPath;
            }

            if (texture == null)
            {
                Debug.LogWarning($"GearbotMotionLoader: missing texture at '{normalizedPath}'.");
                return null;
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        if (!texture.isReadable)
        {
            Sprite imported = Resources.Load<Sprite>(normalizedPath);
            if (imported != null)
                return imported;

            Debug.LogWarning($"GearbotMotionLoader: '{normalizedPath}' is not readable.");
            return null;
        }

        Vector2 pivot = PivotForTexture(texture);

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            pivot,
            PixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
    }

    static Vector2 PivotForTexture(Texture2D texture)
    {
        int footFromTop = IsHammerFrame(texture)
            ? FindHammerBootRowFromTop(texture)
            : FindBodyFootRowFromTop(texture);
        float pivotY = (texture.height - footFromTop - 0.5f) / texture.height;
        return new Vector2(0.5f, Mathf.Clamp01(pivotY));
    }

    static bool IsHammerFrame(Texture2D texture)
    {
        return texture.name.Contains("attack_hammer");
    }

    static int FindHammerBootRowFromTop(Texture2D texture)
    {
        if (!TryGetContentBounds(texture, out int minX, out int minY, out int maxX, out int maxY))
            return FindBodyFootRowFromTop(texture);

        int x0 = Mathf.Max(minX, texture.width / 5);
        int x1 = Mathf.Min(maxX + 1, (4 * texture.width) / 5);
        int minCoverage = Mathf.Max(4, (x1 - x0) / 5);
        int splashBand = Mathf.Clamp(Mathf.RoundToInt((maxY - minY + 1) * 0.18f), 18, 36);
        int scanStart = minY + splashBand;
        int scanEnd = minY + Mathf.Max(splashBand + 1, Mathf.RoundToInt((maxY - minY + 1) * 0.55f));

        for (int y = scanStart; y <= scanEnd; y++)
        {
            if (!RowHasCoverage(texture, y, x0, x1, minCoverage))
                continue;

            int upperY = y + Mathf.Clamp(Mathf.RoundToInt((maxY - minY + 1) * 0.22f), 12, 28);
            if (upperY <= maxY && RowHasCoverage(texture, upperY, x0, x1, minCoverage / 2))
                return texture.height - 1 - y;
        }

        return FindBodyFootRowFromTop(texture);
    }

    static int FindBodyFootRowFromTop(Texture2D texture)
    {
        if (!TryGetContentBounds(texture, out int minX, out int minY, out int maxX, out int maxY))
            return FindFootRowFromTop(texture);

        int contentHeight = maxY - minY + 1;
        int scanMaxY = minY + Mathf.Max(1, Mathf.RoundToInt(contentHeight * 0.45f));
        int x0 = Mathf.Max(minX, texture.width / 5);
        int x1 = Mathf.Min(maxX + 1, (4 * texture.width) / 5);
        int minCoverage = Mathf.Max(4, (x1 - x0) / 5);

        for (int y = minY; y <= scanMaxY; y++)
        {
            if (!RowHasCoverage(texture, y, x0, x1, minCoverage))
                continue;

            int upperY = y + Mathf.Clamp(Mathf.RoundToInt((maxY - minY + 1) * 0.22f), 12, 28);
            if (upperY <= maxY && RowHasCoverage(texture, upperY, x0, x1, minCoverage / 2))
                return texture.height - 1 - y;
        }

        return FindFootRowFromTop(texture);
    }

    static bool RowHasCoverage(Texture2D texture, int y, int x0, int x1, int minCoverage)
    {
        int count = 0;
        for (int x = x0; x < x1; x++)
        {
            if (texture.GetPixel(x, y).a > 0.05f)
                count++;
        }

        return count >= minCoverage;
    }

    static int FindFootRowFromTop(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        int x0 = Mathf.Max(0, width / 5);
        int x1 = Mathf.Min(width, (4 * width) / 5);
        int minCoverage = Mathf.Max(4, (x1 - x0) / 5);

        for (int y = 0; y < height; y++)
        {
            int count = 0;
            for (int x = x0; x < x1; x++)
            {
                if (texture.GetPixel(x, y).a > 0.05f)
                    count++;
            }

            if (count >= minCoverage)
                return height - 1 - y;
        }

        return Mathf.RoundToInt(height * 0.8f);
    }

    static bool TryGetContentBounds(
        Texture2D texture,
        out int minX,
        out int minY,
        out int maxX,
        out int maxY)
    {
        minX = texture.width;
        minY = texture.height;
        maxX = 0;
        maxY = 0;
        bool found = false;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                if (texture.GetPixel(x, y).a <= 0.05f)
                    continue;

                found = true;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
        }

        return found;
    }
}
