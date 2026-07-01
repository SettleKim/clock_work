using UnityEngine;

namespace ClockWork.Game
{
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new(0f, 8.5f, -10f);
        [SerializeField] float smoothTime = 0.12f;

        Vector3 velocity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
