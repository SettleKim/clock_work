using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new(0f, 1.2f, -10f);
    [SerializeField] float smoothTime = 0.15f;
    [SerializeField] bool clampToBounds;
    [SerializeField] Vector2 minBounds = new(-34f, -10f);
    [SerializeField] Vector2 maxBounds = new(26f, 10f);

    Camera cam;
    Vector3 velocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
            else
                return;
        }

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

    public void SetTarget(Transform newTarget) => target = newTarget;

    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        clampToBounds = true;
    }
}
