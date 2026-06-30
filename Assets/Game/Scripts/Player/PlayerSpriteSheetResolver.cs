using System.Collections.Generic;
using UnityEngine;

namespace ClockWork.Game
{
    public static class PlayerSpriteSheetResolver
    {
        public const string WaitSheetPath = "Assets/Game/art/player/tick - wait.png";
        public const string WalkSheetPath = "Assets/Game/art/player/tick - walk.png";

        public static Sprite[] LoadSpritesSortedByX(string assetPath)
        {
#if UNITY_EDITOR
            var sprites = new List<Sprite>();
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                    sprites.Add(sprite);
            }

            sprites.Sort((a, b) => a.rect.x.CompareTo(b.rect.x));
            return sprites.ToArray();
#else
            return System.Array.Empty<Sprite>();
#endif
        }

        public static bool TryGetIdleSprites(out Sprite left, out Sprite right)
        {
            Sprite[] sprites = LoadSpritesSortedByX(WaitSheetPath);
            if (sprites.Length >= 2)
            {
                left = sprites[0];
                right = sprites[1];
                return true;
            }

            if (sprites.Length == 1)
            {
                left = sprites[0];
                right = sprites[0];
                return true;
            }

            left = null;
            right = null;
            return false;
        }

        public static Sprite[] LoadWalkFrames() => LoadSpritesSortedByX(WalkSheetPath);
    }
}
