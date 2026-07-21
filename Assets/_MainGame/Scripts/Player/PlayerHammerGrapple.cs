using System.Collections;
using UnityEngine;

namespace ClockWork.Game
{
    /// <summary>
    /// 망치→주먹 전환 시 발동하는 그래플 이동기.
    /// 바라보는 방향으로 망치를 던져(레이캐스트) 첫 장애물/적에서 멈추고,
    /// 적중 시 피해를 준 뒤, 줄을 당겨 망치가 떨어진 위치로 빠르게 이동한다.
    /// PlayerMovement 는 <see cref="IsPulling"/> 을 보고 이동/중력을 비켜준다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerHammerGrapple : MonoBehaviour
    {
        [Header("Throw")]
        [SerializeField] float throwRange = 8f;
        [SerializeField] float impactDamage = 3f;
        [SerializeField] float chestHeight = 0.85f;

        [Header("Pull")]
        [SerializeField] float pullSpeed = 30f;
        [SerializeField] float stopDistance = 0.6f;
        [SerializeField] float maxPullTime = 0.8f;

        [Header("Visual")]
        [SerializeField] float ropeWidth = 0.08f;
        [SerializeField] Color ropeColor = new(0.85f, 0.7f, 0.3f, 0.95f);

        Rigidbody2D rb;
        PlayerMovement movement;
        LineRenderer rope;

        readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[16];
        ContactFilter2D filter;

        bool isPulling;
        public bool IsPulling => isPulling;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            movement = GetComponent<PlayerMovement>();
            filter = new ContactFilter2D { useTriggers = false, useLayerMask = false };
            CreateRope();
        }

        void CreateRope()
        {
            var ropeObject = new GameObject("HammerRope");
            ropeObject.transform.SetParent(transform);
            rope = ropeObject.AddComponent<LineRenderer>();
            rope.positionCount = 2;
            rope.useWorldSpace = true;
            rope.startWidth = ropeWidth;
            rope.endWidth = ropeWidth;
            rope.material = new Material(Shader.Find("Sprites/Default"));
            rope.startColor = ropeColor;
            rope.endColor = ropeColor;
            rope.sortingOrder = 14;
            rope.enabled = false;
        }

        /// <summary>망치 투척 + 당겨 이동 시작. 이미 진행 중이면 false.</summary>
        public bool TryLaunch()
        {
            if (isPulling)
                return false;

            int facing = movement != null ? movement.FacingDirection : 1;
            Vector2 origin = (Vector2)transform.position + Vector2.up * chestHeight;
            Vector2 dir = new(facing, 0f);

            Vector2 landing = origin + dir * throwRange;
            Health hitEnemy = null;

            int count = Physics2D.Raycast(origin, dir, filter, hitBuffer, throwRange);
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = hitBuffer[i];
                if (h.collider == null)
                    continue;
                if (h.collider.transform == transform || h.collider.transform.IsChildOf(transform))
                    continue;
                if (h.distance >= bestDist)
                    continue;

                bestDist = h.distance;
                landing = h.point;
                hitEnemy = h.collider.CompareTag("Enemy") ? h.collider.GetComponent<Health>() : null;
            }

            if (hitEnemy != null && !hitEnemy.IsDead)
                hitEnemy.ApplyDamage(DamageInfo.Physical(impactDamage, gameObject));

            StartCoroutine(PullRoutine(landing));
            return true;
        }

        IEnumerator PullRoutine(Vector2 target)
        {
            isPulling = true;
            float cachedGravity = rb.gravityScale;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rope.enabled = true;

            Vector2 toTarget = target - rb.position;
            Vector2 dir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right;
            Vector2 stopPoint = target - dir * stopDistance;

            float elapsed = 0f;
            while (Vector2.Distance(rb.position, stopPoint) > 0.15f && elapsed < maxPullTime)
            {
                rb.MovePosition(Vector2.MoveTowards(rb.position, stopPoint, pullSpeed * Time.fixedDeltaTime));
                rope.SetPosition(0, transform.position);
                rope.SetPosition(1, target);
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = cachedGravity;
            rope.enabled = false;
            isPulling = false;
        }

        void OnDisable()
        {
            if (isPulling)
            {
                rb.gravityScale = 3f;
                rope.enabled = false;
                isPulling = false;
            }
        }
    }
}
