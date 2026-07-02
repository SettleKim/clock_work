using UnityEditor;
using UnityEngine;

namespace ClockWork.Game.Editor
{
    static class ComboDefinitionEditor
    {
        const string FistComboAssetPath = "Assets/Game/Resources/Combos/FistCombo.asset";
        const string HammerComboAssetPath = "Assets/Game/Resources/Combos/HammerCombo.asset";
        const string HammerWeaponAssetPath = "Assets/Game/Resources/Weapons/Hammer.asset";

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

        [MenuItem("Clock Work/Combat/Ensure Hammer Weapon Assets")]
        public static void EnsureHammerWeaponAssets()
        {
            EnsureFolder("Assets/Game/Resources/Combos");
            EnsureFolder("Assets/Game/Resources/Weapons");

            var combo = AssetDatabase.LoadAssetAtPath<ComboDefinition>(HammerComboAssetPath);
            if (combo == null)
            {
                combo = ScriptableObject.CreateInstance<ComboDefinition>();
                AssetDatabase.CreateAsset(combo, HammerComboAssetPath);
                Debug.Log($"[WeaponDefinition] Created: {HammerComboAssetPath}");
            }

            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(HammerWeaponAssetPath);
            if (weapon == null)
            {
                weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(weapon, HammerWeaponAssetPath);
                Debug.Log($"[WeaponDefinition] Created: {HammerWeaponAssetPath}");
            }

            var serializedWeapon = new SerializedObject(weapon);
            serializedWeapon.FindProperty("weaponId").stringValue = "hammer";
            serializedWeapon.FindProperty("displayName").stringValue = "Hammer";
            serializedWeapon.FindProperty("combo").objectReferenceValue = combo;
            serializedWeapon.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            Debug.Log($"[WeaponDefinition] Hammer weapon ready: {HammerWeaponAssetPath}");
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
