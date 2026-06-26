using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class TrainingDummy : MonoBehaviour
{
    [SerializeField] float maxHealth = 10f;
    [SerializeField] float respawnDelay = 3f;

    Health health;
    SpriteRenderer spriteRenderer;
    BoxCollider2D boxCollider;
    WorldHealthBar healthBar;
    Vector3 spawnPosition;
    bool respawning;

    void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        spawnPosition = transform.position;

        gameObject.tag = "Enemy";
        health.DestroyOnDeath = false;
        health.Configure(maxHealth, false);

        spriteRenderer.sprite = CombatSpriteUtil.CreateRectSprite(14, 22, new Color(0.72f, 0.58f, 0.42f));
        spriteRenderer.sortingOrder = 2;

        boxCollider.size = new Vector2(0.9f, 1.4f);

        healthBar = gameObject.AddComponent<WorldHealthBar>();
        healthBar.Bind(health);

        health.Died += OnDied;
    }

    void OnDestroy()
    {
        if (health != null)
            health.Died -= OnDied;
    }

    void OnDied()
    {
        if (respawning)
            return;

        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        respawning = true;
        SetActiveState(false);
        healthBar.SetVisible(false);
        yield return new WaitForSeconds(respawnDelay);
        transform.position = spawnPosition;
        health.ResetHealth();
        healthBar.ResetTotalDamage();
        SetActiveState(true);
        respawning = false;
    }

    void SetActiveState(bool active)
    {
        spriteRenderer.enabled = active;
        boxCollider.enabled = active;
    }
}
