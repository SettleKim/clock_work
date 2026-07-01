using UnityEditor;
using UnityEngine;

namespace ClockWork.Game.Editor
{
    static class ComboDefinitionEditor
    {
        const string FistComboAssetPath = "Assets/Game/Resources/Combos/FistCombo.asset";

        [MenuItem("Clock Work/Combat/Ensure Fist Combo Asset")]
        public static void EnsureFistComboAsset()
        {
            EnsureFolder("Assets/Game/Resources/Combos");

            var existing = AssetDatabase.LoadAssetAtPath<ComboDefinition>(FistComboAssetPath);
            if (existing != null)
            {
                Debug.Log($"[ComboDefinition] Already exists: {FistComboAssetPath}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<ComboDefinition>();
            AssetDatabase.CreateAsset(asset, FistComboAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ComboDefinition] Created: {FistComboAssetPath}");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
