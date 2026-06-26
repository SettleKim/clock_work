using System.Collections.Generic;
using UnityEngine;

public class GrapplePoint : MonoBehaviour
{
    public enum HighlightState { Normal, Available, Selected }

    static readonly List<GrapplePoint> activePoints = new();

    [SerializeField] Vector2 launchDirection = Vector2.right;
    [SerializeField] float launchSpeed = 28f;
    [SerializeField] float useRadius = 4f;
    [SerializeField] float snapDuration = 0.1f;

    SpriteRenderer markerRenderer;
    readonly Color normalColor = new(0.95f, 0.72f, 0.28f, 0.65f);
    readonly Color availableColor = new(1f, 0.88f, 0.42f, 0.95f);
    readonly Color selectedColor = new(0.45f, 0.95f, 1f, 1f);
    HighlightState highlightState = HighlightState.Normal;

    public Vector2 Anchor => transform.position;
    public Vector2 LaunchDirection => launchDirection.sqrMagnitude > 0.01f ? launchDirection.normalized : Vector2.right;
    public float LaunchSpeed => launchSpeed;
    public float UseRadius => useRadius;
    public float SnapDuration => snapDuration;

    public static IReadOnlyList<GrapplePoint> ActivePoints => activePoints;

    public void Configure(Vector2 direction, float speed, float radius, float snapTime = 0.1f)
    {
        launchDirection = direction;
        launchSpeed = speed;
        useRadius = radius;
        snapDuration = snapTime;
    }

    void Awake()
    {
        markerRenderer = GetComponent<SpriteRenderer>();
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

    void ApplyHighlight(HighlightState state)
    {
        if (markerRenderer == null)
            return;

        switch (state)
        {
            case HighlightState.Selected:
                markerRenderer.color = selectedColor;
                transform.localScale = Vector3.one * 1.6f;
                break;
            case HighlightState.Available:
                markerRenderer.color = availableColor;
                transform.localScale = Vector3.one * 1.2f;
                break;
            default:
                markerRenderer.color = normalColor;
                transform.localScale = Vector3.one;
                break;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, useRadius);

        Vector2 dir = LaunchDirection;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(dir * 2.5f));
    }
}
