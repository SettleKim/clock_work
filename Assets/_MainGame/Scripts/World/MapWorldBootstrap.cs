using UnityEngine;

namespace ClockWork.Game
{
    /// <summary>
    /// MapRoot에 부착. Play 시 시작 Room 하나만 활성화합니다.
    /// </summary>
    public class MapWorldBootstrap : MonoBehaviour
    {
        [SerializeField] string startRoomId = "test_city";
        [SerializeField] string startSpawnId = "Default";
        [SerializeField] bool applyOnStart = true;

        void Start()
        {
            if (!applyOnStart)
                return;

            var transition = GetComponent<MapTransitionService>();
            if (transition == null)
                transition = gameObject.AddComponent<MapTransitionService>();

            transition.Initialize(startRoomId, startSpawnId);
        }
    }
}
