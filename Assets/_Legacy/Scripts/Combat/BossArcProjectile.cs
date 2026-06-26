using System.Collections;
using UnityEngine;

public class BossArcProjectile : MonoBehaviour
{
    const float DefaultFlightDuration = 0.85f;

    [SerializeField] float arcHeight = 5f;
    [SerializeField] float hitRadius = 0.55f;

    SpriteRenderer spriteRenderer;
    CircleCollider2D circleCollider;
    Vector2 start;
    Vector2 end;
    float damage;
    float flightDuration;
    bool hasDamaged;

    public static void Spawn(Vector2 origin, Vector2 landingPoint, float projectileDamage, float travelTime = DefaultFlightDuration)
    {
        var projectileObject = new GameObject("BossArcProjectile");
        projectileObject.transform.position = origin;

        var projectile = projectileObject.AddComponent<BossArcProjectile>();
        projectile.SetupVisual();
        projectile.Initialize(origin, landingPoint, projectileDamage, travelTime);
    }

    void SetupVisual()
    {
        circleCollider = gameObject.AddComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = hitRadius;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CombatSpriteUtil.CreateRectSprite(12, 12, new Color(1f, 0.55f, 0.15f, 1f));
        spriteRenderer.sortingOrder = 30;
        transform.localScale = Vector3.one * 1.4f;
    }

    void Initialize(Vector2 origin, Vector2 landing, float projectileDamage, float travelTime)
    {
        start = origin;
        end = landing;
        damage = projectileDamage;
        flightDuration = Mathf.Max(0.1f, travelTime);
        StartCoroutine(FlyRoutine());
    }

    IEnumerator FlyRoutine()
    {
        float elapsed = 0f;
        while (elapsed < flightDuration)
        {
            elapsed += WorldSlowMotion.WorldDeltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);
            Vector2 flat = Vector2.Lerp(start, end, t);
            float height = arcHeight * 4f * t * (1f - t);
            transform.position = new Vector3(flat.x, flat.y + height, 0f);
            yield return null;
        }

        transform.position = end;
        TryDamageAt(end);
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        TryDamage(other.GetComponent<Health>());
    }

    void TryDamageAt(Vector2 point)
    {
        Collider2D hit = Physics2D.OverlapCircle(point, hitRadius);
        if (hit != null && hit.CompareTag("Player"))
            TryDamage(hit.GetComponent<Health>());
    }

    void TryDamage(Health targetHealth)
    {
        if (hasDamaged || targetHealth == null || targetHealth.IsDead)
            return;

        hasDamaged = true;
        targetHealth.TakeDamage(damage, gameObject);
    }
}
