using UnityEditor;
using UnityEngine;

namespace ClockWork.Game.Editor
{
    static class PlayerSpriteAnimationBuilder
    {
        const string IdleAnimPath = "Assets/Game/art/player/Player_Idle.anim";
        const string WalkAnimPath = "Assets/Game/art/player/Player_Walk.anim";
        const float WalkFrameDuration = 0.15f;

        [MenuItem("Clock Work/Player/Rebuild Idle && Walk Animations")]
        public static void RebuildFromMenu() => RebuildAll();

        public static void RebuildAll()
        {
            if (!PlayerSpriteSheetResolver.TryGetIdleSprites(out Sprite idleLeft, out _))
            {
                Debug.LogWarning("[PlayerSpriteAnimationBuilder] Idle sprites not found.");
                return;
            }

            Sprite[] walkFrames = PlayerSpriteSheetResolver.LoadWalkFrames();
            if (walkFrames.Length < 2)
            {
                Debug.LogWarning("[PlayerSpriteAnimationBuilder] Walk sheet needs at least 2 sprites.");
                return;
            }

            RebuildIdleClip(idleLeft);
            RebuildWalkClip(walkFrames);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlayerSpriteAnimationBuilder] Rebuilt Player_Idle and Player_Walk.");
        }

        static void RebuildIdleClip(Sprite idleSprite)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleAnimPath);
            if (clip == null)
                return;

            SetSpriteCurve(clip, new[] { idleSprite }, new[] { 0f }, loopTime: false);
            EditorUtility.SetDirty(clip);
        }

        static void RebuildWalkClip(Sprite[] walkFrames)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkAnimPath);
            if (clip == null)
                return;

            var sprites = new Sprite[walkFrames.Length + 1];
            var times = new float[walkFrames.Length + 1];
            for (int i = 0; i < walkFrames.Length; i++)
            {
                sprites[i] = walkFrames[i];
                times[i] = i * WalkFrameDuration;
            }

            sprites[walkFrames.Length] = walkFrames[0];
            times[walkFrames.Length] = walkFrames.Length * WalkFrameDuration;

            SetSpriteCurve(clip, sprites, times, loopTime: true);
            EditorUtility.SetDirty(clip);
        }

        static void SetSpriteCurve(AnimationClip clip, Sprite[] sprites, float[] times, bool loopTime)
        {
            var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = times[i],
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loopTime;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }
    }

    class PlayerSpriteSheetImportPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (path == PlayerSpriteSheetResolver.WaitSheetPath ||
                    path == PlayerSpriteSheetResolver.WalkSheetPath)
                {
                    PlayerSpriteAnimationBuilder.RebuildAll();
                    return;
                }
            }
        }
    }
}
