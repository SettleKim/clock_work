using System.Collections.Generic;
using UnityEngine;

namespace ClockWork.Game
{
    public class GrapplePoint : MonoBehaviour
    {
        public enum GrapplePointKind { Anchor, Momentum }
        public enum HighlightState { Normal, Available, Selected }

        static readonly List<GrapplePoint> activePoints = new();

        [Header("Type")]
        [SerializeField] GrapplePointKind kind = GrapplePointKind.Momentum;

        [Header("Common")]
        [SerializeField] float useRadius = 4f;
        [SerializeField] float ropeSnapDuration = 0.1f;

        [Header("Anchor — 붙는 포인트")]
        [SerializeField] Vector2 attachOffset = Vector2.zero;

        [Header("Momentum — 관성 발사")]
        [SerializeField] Vector2 launchDirection = new(1f, 0.5f);
        [SerializeField] float launchSpeed = 28f;

        SpriteRenderer markerRenderer;
        Vector3 baseScale = Vector3.one;
        HighlightState highlightState = HighlightState.Normal;

        static readonly Color anchorNormal = new(0.35f, 0.92f, 0.95f, 0.9f);
        static readonly Color momentumNormal = new(0.95f, 0.72f, 0.28f, 0.85f);
        readonly Color availableColor = new(1f, 0.88f, 0.42f, 0.95f);
        readonly Color selectedColor = new(0.45f, 0.95f, 1f, 1f);

        public GrapplePointKind Kind => kind;
        public bool IsAnchor => kind == GrapplePointKind.Anchor;
        public bool IsMomentum => kind == GrapplePointKind.Momentum;
        public Vector2 Anchor => transform.position;
        public Vector2 AttachPosition => (Vector2)transform.position + attachOffset;
        public Vector2 LaunchDirection => launchDirection.sqrMagnitude > 0.01f ? launchDirection.normalized : Vector2.right;
        public float LaunchSpeed => launchSpeed;
        public float UseRadius => useRadius;
        public float RopeSnapDuration => ropeSnapDuration;

        public static IReadOnlyList<GrapplePoint> ActivePoints => activePoints;

        void Awake()
        {
            markerRenderer = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale.sqrMagnitude > 0.001f ? transform.localScale : Vector3.one;
            ApplyKindVisual();
            ApplyHighlight(HighlightState.Normal);
        }

        void OnEnable() => activePoints.Add(this);

        void OnDisable()
        {
            activePoints.Remove(this);
            ApplyHighlight(HighlightState.Normal);
        }

        public bool IsInRange(Vector2 playerPosition)
        {
            return Vector2.Distance(playerPosition, Anchor) <= useRadius;
        }

        public void SetHighlight(HighlightState state)
        {
            if (highlightState == state)
                return;

            highlightState = state;
            ApplyHighlight(state);
        }

        void ApplyKindVisual()
        {
            if (markerRenderer == null)
                return;

            markerRenderer.color = IsAnchor ? anchorNormal : momentumNormal;
        }

        void ApplyHighlight(HighlightState state)
        {
            if (markerRenderer == null)
                return;

            switch (state)
            {
                case HighlightState.Selected:
                    markerRenderer.color = selectedColor;
                    transform.localScale = baseScale * 1.6f;
                    break;
                case HighlightState.Available:
                    markerRenderer.color = availableColor;
                    transform.localScale = baseScale * 1.2f;
                    break;
                default:
                    ApplyKindVisual();
                    transform.localScale = baseScale;
                    break;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = IsAnchor
                ? new Color(0.35f, 0.92f, 0.95f, 0.35f)
                : new Color(1f, 0.85f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, useRadius);

            if (IsAnchor)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(AttachPosition, 0.15f);
                if (attachOffset.sqrMagnitude > 0.0001f)
                {
                    Gizmos.color = new Color(0.35f, 0.92f, 0.95f, 0.8f);
                    Gizmos.DrawLine(transform.position, AttachPosition);
                }
            }
            else
            {
                Vector2 dir = LaunchDirection;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)(dir * 2.5f));
            }
        }
    }
}
