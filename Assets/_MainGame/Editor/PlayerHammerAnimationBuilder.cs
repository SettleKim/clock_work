using UnityEditor;
using UnityEngine;

namespace ClockWork.Game.Editor
{
    static class PlayerHammerAnimationBuilder
    {
        const string Hammer1ClipPath = "Assets/_MainGame/art/player/hammer/tick_attack_hammer_1.anim";
        const string Hammer2ClipPath = "Assets/_MainGame/art/player/hammer/tick_attack_hammer_2.anim";
        const float FrameDuration = 0.15f; // 4 frames * 0.15s ≈ HammerCombo motionHold(0.6s)

        static readonly string[] Step1Frames =
        {
            "Assets/_MainGame/art/player/hammer/00.png",
            "Assets/_MainGame/art/player/hammer/01.png",
            "Assets/_MainGame/art/player/hammer/02.png",
            "Assets/_MainGame/art/player/hammer/03.png",
        };

        static readonly string[] Step2Frames =
        {
            "Assets/_MainGame/art/player/hammer/04.png",
            "Assets/_MainGame/art/player/hammer/05.png",
            "Assets/_MainGame/art/player/hammer/06.png",
            "Assets/_MainGame/art/player/hammer/07.png",
        };

        [MenuItem("Clock Work/Player/Rebuild Hammer Attack Animations")]
        public static void Rebuild()
        {
            BuildClip(Hammer1ClipPath, "tick_attack_hammer_1", Step1Frames);
            BuildClip(Hammer2ClipPath, "tick_attack_hammer_2", Step2Frames);
            AssetDatabase.SaveAssets();
        }

        static void BuildClip(string path, string clipName, string[] framePaths)
        {
            var sprites = new Sprite[framePaths.Length];
            for (int i = 0; i < framePaths.Length; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(framePaths[i]);
                if (sprites[i] == null)
                {
                    Debug.LogWarning($"[PlayerHammerAnimationBuilder] Sprite not found at {framePaths[i]}");
                    return;
                }
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = clip == null;
            if (isNew)
            {
                clip = new AnimationClip { name = clipName };
                AssetDatabase.CreateAsset(clip, path);
            }

            var keys = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i * FrameDuration,
                    value = sprites[i],
                };
            }

            var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            Debug.Log($"[PlayerHammerAnimationBuilder] {(isNew ? "Created" : "Rebuilt")} {clipName} (guid={AssetDatabase.AssetPathToGUID(path)})");
        }
    }
}
