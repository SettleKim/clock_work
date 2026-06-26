using System.Collections;
using UnityEngine;

public class LaserBeamAttack : MonoBehaviour
{
    [SerializeField] float damage = 2f;
    [SerializeField] float range = 12f;
    [SerializeField] float beamDuration = 0.18f;
    [SerializeField] float beamWidth = 0.12f;
    [SerializeField] LayerMask hitLayers = ~0;
    [SerializeField] string[] targetTags = { "Enemy" };

    LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        Shader shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
            ?? Shader.Find("Universal Render Pipeline/Unlit");

        if (shader != null)
            lineRenderer.material = new Material(shader);

        lineRenderer.startColor = new Color(0.45f, 0.85f, 1f, 0.95f);
        lineRenderer.endColor = new Color(0.85f, 0.95f, 1f, 0.15f);
        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth * 0.35f;
        lineRenderer.positionCount = 2;
        lineRenderer.sortingOrder = 7;
        lineRenderer.enabled = false;
        lineRenderer.useWorldSpace = true;
    }

    public void Fire(Vector2 origin, Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right;

        direction = direction.normalized;
        StopAllCoroutines();
        StartCoroutine(BeamRoutine(origin, direction));
    }

    public void Configure(float newDamage)
    {
        damage = newDamage;
    }

    IEnumerator BeamRoutine(Vector2 origin, Vector2 direction)
    {
        Vector2 end = origin + direction * range;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, range, hitLayers);

        float closestDistance = range;
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform.root))
                continue;

            if (hit.distance < closestDistance)
                closestDistance = hit.distance;
        }

        end = origin + direction * closestDistance;

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, end);

        ApplyDamage(origin, direction, closestDistance);

        yield return new WaitForSeconds(beamDuration);
        lineRenderer.enabled = false;
    }

    void ApplyDamage(Vector2 origin, Vector2 direction, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance, hitLayers);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            GameObject target = hit.collider.gameObject;
            if (!IsValidTarget(target))
                continue;

            Health health = target.GetComponent<Health>();
            if (health != null && !health.IsDead)
                health.TakeDamage(damage, gameObject);
        }
    }

    bool IsValidTarget(GameObject target)
    {
        for (int i = 0; i < targetTags.Length; i++)
        {
            if (target.CompareTag(targetTags[i]))
                return true;
        }

        return false;
    }
}
