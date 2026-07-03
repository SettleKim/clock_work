using UnityEngine;

namespace ClockWork.Game
{
    public enum RoomEdgeDirection { Left, Right, Up, Down }

    /// <summary>
    /// Room 가장자리 trigger — 진입 시 다른 Room으로 전환합니다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomEdgeTransition : MonoBehaviour
    {
        [SerializeField] string targetRoomId = "test_city";
        [SerializeField] string targetSpawnId = "FromLimbus";
        [SerializeField] RoomEdgeDirection edgeDirection = RoomEdgeDirection.Right;
        [SerializeField] bool requireMovingTowardEdge = true;
        [SerializeField] float minSpeed = 0.3f;

        void Reset()
        {
            var box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            if (requireMovingTowardEdge && !IsMovingTowardEdge(other))
                return;

            MapTransitionService.Instance?.GoTo(targetRoomId, targetSpawnId);
        }

        bool IsMovingTowardEdge(Collider2D playerCollider)
        {
            var body = playerCollider.attachedRigidbody;
            if (body == null)
                return true;

            Vector2 velocity = body.linearVelocity;
            if (velocity.sqrMagnitude < minSpeed * minSpeed)
                return false;

            return edgeDirection switch
            {
                RoomEdgeDirection.Right => velocity.x > 0f,
                RoomEdgeDirection.Left => velocity.x < 0f,
                RoomEdgeDirection.Up => velocity.y > 0f,
                RoomEdgeDirection.Down => velocity.y < 0f,
                _ => true
            };
        }
    }
}
