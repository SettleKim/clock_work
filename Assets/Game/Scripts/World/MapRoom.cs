using UnityEngine;

namespace ClockWork.Game
{
    /// <summary>
    /// 맵 전환 단위(Room) 루트에 부착합니다.
    /// </summary>
    public class MapRoom : MonoBehaviour
    {
        [SerializeField] string roomId = "";
        [SerializeField] Transform defaultSpawn;
        [SerializeField] bool useCameraBounds;
        [SerializeField] Vector2 cameraBoundsMin = new(-20f, -6f);
        [SerializeField] Vector2 cameraBoundsMax = new(20f, 14f);

        public string RoomId => string.IsNullOrEmpty(roomId) ? gameObject.name : roomId;
        public bool UseCameraBounds => useCameraBounds;

        void Awake()
        {
            if (string.IsNullOrEmpty(roomId))
                roomId = gameObject.name;
        }

        public Transform GetSpawn(string spawnId)
        {
            if (defaultSpawn == null)
                defaultSpawn = transform.Find("Spawns/Default");

            if (string.IsNullOrEmpty(spawnId) || spawnId == "Default" || spawnId == "default")
                return defaultSpawn;

            var named = transform.Find($"Spawns/{spawnId}");
            return named != null ? named : defaultSpawn;
        }

        public void ApplyCameraBounds()
        {
            if (!useCameraBounds)
                return;

            var cameraFollow = FindFirstObjectByType<CameraFollow2D>();
            if (cameraFollow != null)
                cameraFollow.SetBounds(cameraBoundsMin, cameraBoundsMax);
        }
    }
}
