using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(DamageDealer))]
public class HammerThrowProjectile : MonoBehaviour
{
    [SerializeField] float speed = 16f;
    [SerializeField] float maxDistance = 12f;

    Rigidbody2D rb;
    DamageDealer damageDealer;
    Vector2 start;
    Vector2 direction;
    float traveled;
    bool hasResolved;

    public Vector2 LandingPosition { get; private set; }
    public bool HasResolved => hasResolved;

    public static HammerThrowProjectile Spawn(Vector2 origin, int facing, float damage)
    {
        var projectileObject = new GameObject("HammerThrowProjectile");
        projectileObject.transform.position = origin;

        var projectile = projectileObject.AddComponent<HammerThrowProjectile>();
        projectile.Initialize(origin, facing, damage);
        return projectile;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        damageDealer = GetComponent<DamageDealer>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var collider = GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.35f;

        var spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = CombatSpriteUtil.CreateRectSprite(12, 12, new Color(0.75f, 0.48f, 0.22f));
        spriteRenderer.sortingOrder = 7;
    }

    void Initialize(Vector2 origin, int facing, float damage)
    {
        start = origin;
        direction = new Vector2(Mathf.Sign(facing), 0f);
        if (Mathf.Abs(direction.x) < 0.01f)
            direction = Vector2.right;

        LandingPosition = origin;
        damageDealer.Configure(damage, false);
        damageDealer.ResetHits();
    }

    void Update()
    {
        if (hasResolved)
            return;

        float delta = Time.deltaTime;
        float step = speed * delta;
        traveled += step;
        transform.position += (Vector3)(direction * step);
        LandingPosition = transform.position;

        if (traveled >= maxDistance)
            Resolve(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasResolved || !other.CompareTag("Enemy"))
            return;

        Resolve(true);
    }

    void Resolve(bool hitEnemy)
    {
        if (hasResolved)
            return;

        hasResolved = true;
        rb.linearVelocity = Vector2.zero;
        Destroy(gameObject, hitEnemy ? 0.05f : 0.2f);
    }
}
