using System.Collections.Generic;
using UnityEngine;

namespace ClockWork.Game
{
    public class MapTransitionService : MonoBehaviour
    {
        public static MapTransitionService Instance { get; private set; }

        [SerializeField] float transitionCooldown = 0.45f;

        readonly Dictionary<string, MapRoom> roomsById = new();
        bool isTransitioning;
        float lastTransitionTime = -10f;

        public string CurrentRoomId { get; private set; } = "";

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            CacheRooms();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void CacheRooms()
        {
            roomsById.Clear();
            var rooms = GetComponentsInChildren<MapRoom>(true);
            foreach (var room in rooms)
                roomsById[room.RoomId] = room;
        }

        public void GoTo(string roomId, string spawnId = "Default")
        {
            if (isTransitioning)
                return;

            if (Time.time - lastTransitionTime < transitionCooldown)
                return;

            if (!roomsById.TryGetValue(roomId, out var targetRoom))
            {
                Debug.LogWarning($"[Map] Room not found: {roomId}");
                return;
            }

            isTransitioning = true;
            lastTransitionTime = Time.time;

            foreach (var pair in roomsById)
                pair.Value.gameObject.SetActive(pair.Key == roomId);

            var player = GameObject.FindGameObjectWithTag("Player");
            var spawn = targetRoom.GetSpawn(spawnId);
            if (player != null && spawn != null)
            {
                player.transform.position = spawn.position;
                var body = player.GetComponent<Rigidbody2D>();
                if (body != null)
                    body.linearVelocity = Vector2.zero;
            }

            CurrentRoomId = roomId;
            targetRoom.ApplyCameraBounds();

            Debug.Log($"[Map] → {roomId} (spawn: {spawnId})");
            isTransitioning = false;
        }

        public void Initialize(string startRoomId, string spawnId = "Default")
        {
            CacheRooms();
            GoTo(startRoomId, spawnId);
        }
    }
}
