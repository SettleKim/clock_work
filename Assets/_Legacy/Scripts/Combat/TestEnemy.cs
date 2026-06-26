using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Health))]
public class TestEnemy : MonoBehaviour
{
    [SerializeField] float moveSpeed = 2.5f;
    [SerializeField] float patrolDistance = 3f;
    [SerializeField] float contactDamage = 1f;
    [SerializeField] float contactCooldown = 0.8f;
    [SerializeField] Color hitFlashColor = new(1f, 0.55f, 0.45f);

    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    Health health;
    BoxCollider2D bodyCollider;
    Collider2D playerCollider;
    Vector3 startPosition;
    int direction = 1;
    float lastContactTime;
    Color originalColor;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
        health.DestroyOnDeath = false;

        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var collider = GetComponent<BoxCollider2D>();
        collider.size = new Vector2(0.9f, 0.85f);
        bodyCollider = collider;

        spriteRenderer.sprite = CombatSpriteUtil.CreateEnemySprite();
        spriteRenderer.sortingOrder = 3;

        health.HealthChanged += OnHealthChanged;
        health.Died += OnDied;
    }

    public void Configure(float patrolRange, float damage = 1f)
    {
        patrolDistance = patrolRange;
        contactDamage = damage;
    }

    void Start()
    {
        startPosition = transform.position;
        originalColor = spriteRenderer.color;
        gameObject.tag = "Enemy";

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerCollider = playerObject.GetComponent<Collider2D>();
            if (playerCollider != null)
                Physics2D.IgnoreCollision(bodyCollider, playerCollider, true);
        }
    }

    void FixedUpdate()
    {
        if (health.IsDead)
            return;

        float leftLimit = startPosition.x - patrolDistance;
        float rightLimit = startPosition.x + patrolDistance;

        if (transform.position.x <= leftLimit)
            direction = 1;
        else if (transform.position.x >= rightLimit)
            direction = -1;

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;

        TryContactDamage();
    }

    void TryContactDamage()
    {
        if (health.IsDead || playerCollider == null || Time.time - lastContactTime < contactCooldown)
            return;

        if (!bodyCollider.bounds.Intersects(playerCollider.bounds))
            return;

        Health playerHealth = playerCollider.GetComponent<Health>();
        if (playerHealth != null && !playerHealth.IsDead)
        {
            playerHealth.TakeDamage(contactDamage, gameObject);
            lastContactTime = Time.time;
        }
    }

    void OnHealthChanged(float current, float max)
    {
        StopAllCoroutines();
        StartCoroutine(HitFlashRoutine());
    }

    IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(0.08f);
        if (this != null && spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    void OnDied()
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        GetComponent<BoxCollider2D>().enabled = false;
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        float timer = 0.25f;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, transform.localScale, timer / 0.25f);
            yield return null;
        }

        Destroy(gameObject);
    }
}
