using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(DamageDealer))]
public class SwordWaveProjectile : MonoBehaviour
{
    [SerializeField] float speed = 14f;
    [SerializeField] float lifetime = 1.2f;

    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    float direction = 1f;

    public void Launch(Vector2 launchDirection, float damage)
    {
        transform.SetParent(null);

        direction = Mathf.Sign(launchDirection.x);
        if (Mathf.Abs(direction) < 0.01f)
            direction = 1f;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
        rb.linearVelocity = new Vector2(direction * speed, 0f);

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;

        var collider = GetComponent<BoxCollider2D>();
        collider.size = new Vector2(1.1f, 0.45f);
        collider.offset = Vector2.zero;

        GetComponent<DamageDealer>().Configure(damage, false);
        Destroy(gameObject, lifetime);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var collider = GetComponent<BoxCollider2D>();
        collider.isTrigger = true;

        spriteRenderer.sprite = CombatSpriteUtil.CreateSwordWaveSprite();
        spriteRenderer.sortingOrder = 6;
    }
}
