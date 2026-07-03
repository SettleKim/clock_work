using UnityEngine;

namespace ClockWork.Game
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new(0f, 8.5f, -10f);
        [SerializeField] float smoothTime = 0.12f;
        [SerializeField] bool clampToBounds;
        [SerializeField] Vector2 minBounds = new(-20f, -6f);
        [SerializeField] Vector2 maxBounds = new(20f, 14f);

        Camera cam;
        Vector3 velocity;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
            clampToBounds = true;
        }

        public void ClearBounds()
        {
            clampToBounds = false;
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 desired = target.position + offset;
            desired.z = offset.z;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

            if (!clampToBounds || cam == null || !cam.orthographic)
                return;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
            pos.y = Mathf.Clamp(pos.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);
            transform.position = pos;
        }
    }
}
