using System.Collections;
using UnityEngine;

public class BossDangerZone : MonoBehaviour
{
    [SerializeField] float pulseSpeed = 6f;

    SpriteRenderer spriteRenderer;
    Vector3 baseScale;
    float elapsed;

    public static BossDangerZone Create(Vector2 center, Vector2 size)
    {
        var zoneObject = new GameObject("BossDangerZone");
        zoneObject.transform.position = center;
        zoneObject.transform.localScale = new Vector3(size.x, size.y, 1f);

        var spriteRenderer = zoneObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CombatSpriteUtil.CreateRectSprite(16, 16, new Color(1f, 0.12f, 0.08f, 0.5f));
        spriteRenderer.sortingOrder = 11;

        var pulse = zoneObject.AddComponent<BossDangerZone>();
        pulse.spriteRenderer = spriteRenderer;
        pulse.baseScale = zoneObject.transform.localScale;
        return pulse;
    }

    public static IEnumerator ShowWarning(Vector2 center, Vector2 size, float duration = 1f)
    {
        BossDangerZone zone = Create(center, size);
        yield return WorldSlowMotion.WaitWorldSeconds(duration);
        if (zone != null)
            Destroy(zone.gameObject);
    }

    public void SetCenter(Vector2 center)
    {
        transform.position = center;
    }

    void Update()
    {
        elapsed += WorldSlowMotion.WorldDeltaTime;
        float alpha = 0.35f + Mathf.Abs(Mathf.Sin(elapsed * pulseSpeed)) * 0.25f;
        spriteRenderer.color = new Color(1f, 0.12f, 0.08f, alpha);
    }
}
