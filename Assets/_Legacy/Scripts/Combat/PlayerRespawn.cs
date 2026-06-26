using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] float respawnDelay = 3f;
    [SerializeField] Vector2 respawnPosition = new(-6f, 2.2f);

    Health health;
    Rigidbody2D rb;
    SpriteRenderer[] renderers;
    Collider2D[] colliders;
    MetroidvaniaPlayerController movement;
    PlayerCombatController combat;
    bool respawning;
    public Vector2 SpawnPosition => respawnPosition;

    void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<MetroidvaniaPlayerController>();
        combat = GetComponent<PlayerCombatController>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);

        health.DestroyOnDeath = false;
        health.Configure(10f, false);
        health.Died += OnPlayerDied;
    }

    void OnDestroy()
    {
        if (health != null)
            health.Died -= OnPlayerDied;
    }

    public void SetSpawnPosition(Vector2 position)
    {
        respawnPosition = position;
    }

    void OnPlayerDied()
    {
        if (respawning)
            return;

        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        respawning = true;
        SetPlayerActive(false);
        yield return new WaitForSeconds(respawnDelay);
        transform.position = respawnPosition;
        rb.linearVelocity = Vector2.zero;
        health.ResetHealth();
        SetPlayerActive(true);
        respawning = false;
    }

    void SetPlayerActive(bool active)
    {
        foreach (SpriteRenderer renderer in renderers)
            renderer.enabled = active;

        foreach (Collider2D collider in colliders)
            collider.enabled = active;

        if (movement != null)
            movement.enabled = active;

        if (combat != null)
            combat.enabled = active;
    }
}
