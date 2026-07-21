using UnityEditor;
using UnityEngine;

namespace ClockWork.Game.Editor
{
    static class PlayerDashAnimationBuilder
    {
        const string DashClipPath = "Assets/_MainGame/art/player/dash/Player_Dash.anim";
        const float FrameDuration = 0.08f; // 4 frames over ~0.32s dash duration

        static readonly string[] FramePaths =
        {
            "Assets/_MainGame/art/player/dash/dash-01-anticipation.png",
            "Assets/_MainGame/art/player/dash/dash-02-push.png",
            "Assets/_MainGame/art/player/dash/dash-03-active.png",
            "Assets/_MainGame/art/player/dash/dash-04-recovery.png",
        };

        [MenuItem("Clock Work/Player/Rebuild Dash Animation")]
        public static void Rebuild()
        {
            var sprites = new Sprite[FramePaths.Length];
            for (int i = 0; i < FramePaths.Length; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(FramePaths[i]);
                if (sprites[i] == null)
                {
                    Debug.LogWarning($"[PlayerDashAnimationBuilder] Sprite not found at {FramePaths[i]}");
                    return;
                }
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DashClipPath);
            bool isNew = clip == null;
            if (isNew)
            {
                clip = new AnimationClip { name = "Player_Dash" };
                AssetDatabase.CreateAsset(clip, DashClipPath);
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
            AssetDatabase.SaveAssets();

            Debug.Log($"[PlayerDashAnimationBuilder] {(isNew ? "Created" : "Rebuilt")} Player_Dash.anim (guid={AssetDatabase.AssetPathToGUID(DashClipPath)})");
        }
    }
}
