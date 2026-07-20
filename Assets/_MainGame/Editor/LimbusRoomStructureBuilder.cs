using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClockWork.Game.Editor
{
    public static class LimbusRoomStructureBuilder
    {
        const string MenuPath = "ClockWork/Map/Create Limbus Room Structure";

        [MenuItem(MenuPath)]
        public static void CreateWithDialog()
        {
            if (!EditorUtility.DisplayDialog(
                    "Limbus Room 구조",
                    "MapRoot / Room_B70_Limbus_Surface 및 자식 폴더를 생성합니다.\n" +
                    "이미 있으면 해당 Room 하위를 비우고 다시 만듭니다.",
                    "생성",
                    "취소"))
                return;

            CreateInternal();
        }

        [MenuItem("ClockWork/Map/Create Limbus Room Structure (Auto)")]
        public static void CreateAuto() => CreateInternal();

        static void CreateInternal()
        {
            var mapRoot = FindOrCreateRoot("MapRoot");
            var room = FindOrCreateChild(mapRoot.transform, "Room_B70_Limbus_Surface");

            ClearChildren(room.transform);

            var visuals = CreateChild(room.transform, "Visuals");
            CreateChild(visuals.transform, "Background");
            CreateChild(visuals.transform, "Platforms");

            CreateChild(room.transform, "Zones");
            CreateChild(room.transform, "GrapplePoints");
            CreateChild(room.transform, "Transitions");

            var spawns = CreateChild(room.transform, "Spawns");
            CreateChild(spawns.transform, "Default");
            CreateChild(spawns.transform, "FromKaligo");
            CreateChild(spawns.transform, "FromDungeon");

            Selection.activeGameObject = room;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log(
                "[LimbusRoomStructureBuilder] 완료\n" +
                "MapRoot/Room_B70_Limbus_Surface/{Visuals(Background,Platforms),Zones,GrapplePoints,Transitions,Spawns(Default,FromKaligo,FromDungeon)}");
        }

        static GameObject FindOrCreateRoot(string name)
        {
            var existing = GameObject.Find(name);
            return existing != null ? existing : new GameObject(name);
        }

        static GameObject FindOrCreateChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
                return child.gameObject;

            return CreateChild(parent, name);
        }

        static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
