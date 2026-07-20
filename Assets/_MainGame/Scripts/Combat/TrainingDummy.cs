using System.Collections;
using UnityEngine;

namespace ClockWork.Game
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class TrainingDummy : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] float respawnDelay = 3f;
        [SerializeField] Vector2 colliderSize = new(0.9f, 1.4f);
        [SerializeField] bool syncSpriteSizeToCollider = true;

        [Header("Physics")]
        [SerializeField] float gravityScale = 3f;

        Health health;
        Rigidbody2D rb;
        SpriteRenderer spriteRenderer;
        BoxCollider2D boxCollider;
        WorldHealthBar healthBar;
        Vector3 spawnPosition;
        bool respawning;

#if UNITY_EDITOR
        bool spriteSizeApplyScheduled;
#endif

        public Rigidbody2D Body => rb;

        void Awake()
        {
            health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            boxCollider = GetComponent<BoxCollider2D>();
            spawnPosition = transform.position;

            gameObject.tag = "Enemy";
            health.DestroyOnDeath = false;

            if (syncSpriteSizeToCollider && spriteRenderer.sprite != null)
                ApplySpriteSizeToCollider();

            boxCollider.size = colliderSize;
            ConfigureRigidbody();

            if (GetComponent<WorldHealthBar>() == null)
            {
                healthBar = gameObject.AddComponent<WorldHealthBar>();
                healthBar.Configure(WorldHealthBar.BarStyle.Enemy, showCumulativeDamage: true);
            }
            else
            {
                healthBar = GetComponent<WorldHealthBar>();
                healthBar.Configure(WorldHealthBar.BarStyle.Enemy, showCumulativeDamage: true);
            }

            health.Died += OnDied;
        }

        void Start()
        {
            IgnorePlayerPhysicalCollision();
        }

        void ConfigureRigidbody()
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = gravityScale;
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void ApplySpriteSizeToCollider()
        {
            if (spriteRenderer.drawMode == SpriteDrawMode.Simple)
                spriteRenderer.drawMode = SpriteDrawMode.Sliced;

            spriteRenderer.size = colliderSize;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (boxCollider == null)
                TryGetComponent(out boxCollider);
            if (spriteRenderer == null)
                TryGetComponent(out spriteRenderer);

            if (boxCollider != null)
                boxCollider.size = colliderSize;

            if (syncSpriteSizeToCollider && spriteRenderer != null && spriteRenderer.sprite != null)
                ScheduleApplySpriteSizeToCollider();
        }

        void ScheduleApplySpriteSizeToCollider()
        {
            if (spriteSizeApplyScheduled)
                return;

            spriteSizeApplyScheduled = true;
            UnityEditor.EditorApplication.delayCall += ApplySpriteSizeDeferred;
        }

        void ApplySpriteSizeDeferred()
        {
            UnityEditor.EditorApplication.delayCall -= ApplySpriteSizeDeferred;
            spriteSizeApplyScheduled = false;

            if (this == null)
                return;

            if (spriteRenderer == null)
                TryGetComponent(out spriteRenderer);

            if (syncSpriteSizeToCollider && spriteRenderer != null && spriteRenderer.sprite != null)
                ApplySpriteSizeToCollider();
        }
#endif

        void IgnorePlayerPhysicalCollision()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            var playerColliders = player.GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < playerColliders.Length; i++)
            {
                Collider2D playerCollider = playerColliders[i];
                if (playerCollider == null || playerCollider.isTrigger)
                    continue;

                Physics2D.IgnoreCollision(boxCollider, playerCollider, true);
            }
        }

        /// <summary>대검 등 공격 넉백·부양. 추후 Damage/Launch 파이프에서 호출.</summary>
        public void ApplyLaunch(Vector2 velocity)
        {
            if (rb == null || respawning)
                return;

            rb.linearVelocity = velocity;
        }

        public void ApplyLaunchImpulse(Vector2 impulse)
        {
            if (rb == null || respawning)
                return;

            rb.AddForce(impulse, ForceMode2D.Impulse);
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
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            health.ResetHealth();
            healthBar.ResetTotalDamage();
            SetActiveState(true);
            respawning = false;
        }

        void SetActiveState(bool active)
        {
            spriteRenderer.enabled = active;
            boxCollider.enabled = active;
            rb.simulated = active;
        }
    }
}
