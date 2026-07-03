using ClockWork.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClockWork.Game.Editor
{
    public static class MapRoomSetupMenu
    {
        [MenuItem("ClockWork/Map/Setup MapRoot Components")]
        public static void SetupMapRoot()
        {
            var mapRoot = GameObject.Find("MapRoot");
            if (mapRoot == null)
            {
                EditorUtility.DisplayDialog("MapRoot 없음", "Hierarchy에 MapRoot를 먼저 만드세요.", "확인");
                return;
            }

            if (mapRoot.GetComponent<MapTransitionService>() == null)
                mapRoot.AddComponent<MapTransitionService>();

            if (mapRoot.GetComponent<MapWorldBootstrap>() == null)
                mapRoot.AddComponent<MapWorldBootstrap>();

            int roomCount = 0;
            foreach (Transform child in mapRoot.transform)
            {
                if (child.GetComponent<MapRoom>() == null)
                    child.gameObject.AddComponent<MapRoom>();

                roomCount++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[MapRoomSetup] MapRoot: MapTransitionService, MapWorldBootstrap, MapRoom ×{roomCount}");
        }
    }
}
