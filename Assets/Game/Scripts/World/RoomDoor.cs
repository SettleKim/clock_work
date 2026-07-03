using UnityEngine;
using UnityEngine.InputSystem;

namespace ClockWork.Game
{
    /// <summary>
    /// E(Interact)로 다른 Room으로 전환합니다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomDoor : MonoBehaviour
    {
        [SerializeField] string targetRoomId = "Limbus";
        [SerializeField] string targetSpawnId = "Default";
        [SerializeField] string doorLabel = "문";

        Transform playerInside;

        public string DoorLabel => doorLabel;

        void Reset()
        {
            var box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
        }

        void Update()
        {
            if (playerInside == null)
                return;

            if (!WasInteractPressed())
                return;

            MapTransitionService.Instance?.GoTo(targetRoomId, targetSpawnId);
        }

        static bool WasInteractPressed()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return false;

            var playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                var interact = playerInput.actions.FindAction("Interact", false);
                if (interact != null && interact.WasPressedThisFrame())
                    return true;
            }

            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                playerInside = other.transform;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") && other.transform == playerInside)
                playerInside = null;
        }
    }
}
