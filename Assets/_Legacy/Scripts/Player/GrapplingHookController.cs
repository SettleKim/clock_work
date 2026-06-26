using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class GrapplingHookController : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] Transform hookOrigin;
    [SerializeField] float selectionRadius = 14f;
    [SerializeField] float slowMotionScale = 0.16f;
    [SerializeField] float directionSelectThreshold = 0.2f;

    [Header("Launch")]
    [SerializeField] float launchGravityScale = 0.12f;
    [SerializeField] float launchControlDuration = 0.4f;
    [SerializeField] float launchSpeedMultiplier = 1.1f;
    [SerializeField] float minLaunchSpeed = 24f;

    [Header("Visual")]
    [SerializeField] float ropeWidth = 0.07f;
    [SerializeField] Color ropeColor = new(0.82f, 0.85f, 0.9f, 0.95f);
    [SerializeField] Color previewRopeColor = new(0.45f, 0.95f, 1f, 0.75f);
    [SerializeField] Color hookColor = new(0.95f, 0.6f, 0.18f);

    enum GrapplePhase { Idle, Selecting, Snapping, Launching }

    Rigidbody2D rb;
    MetroidvaniaPlayerController movement;
    PlayerInput playerInput;
    InputAction grappleAction;
    InputAction moveAction;

    GrapplePhase phase = GrapplePhase.Idle;
    GrapplePoint activePoint;
    GrapplePoint selectedPoint;
    readonly List<GrapplePoint> selectablePoints = new();
    Vector2 anchorPoint;
    Vector2 hookTip;
    Vector2 launchVelocity;
    Vector2 storedVelocity;
    float phaseTimer;
    float defaultGravityScale;
    LineRenderer ropeLine;
    Transform hookVisual;

    public bool IsActive => phase != GrapplePhase.Idle;
    public bool IsSelecting => phase == GrapplePhase.Selecting;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<MetroidvaniaPlayerController>();
        playerInput = GetComponent<PlayerInput>();
        defaultGravityScale = rb.gravityScale;

        grappleAction = playerInput.actions["Grapple"];
        moveAction = playerInput.actions["Move"];

        if (hookOrigin == null)
        {
            var origin = transform.Find("HookOrigin");
            hookOrigin = origin != null ? origin : transform;
        }

        CreateVisuals();
    }

    void OnDisable()
    {
        if (phase == GrapplePhase.Selecting)
            CancelSelection();

        if (phase == GrapplePhase.Launching || phase == GrapplePhase.Snapping)
            EndGrapple();
    }

    void CreateVisuals()
    {
        var ropeObject = new GameObject("GrappleRope");
        ropeObject.transform.SetParent(transform);
        ropeLine = ropeObject.AddComponent<LineRenderer>();
        ropeLine.positionCount = 2;
        ropeLine.useWorldSpace = true;
        ropeLine.startWidth = ropeWidth;
        ropeLine.endWidth = ropeWidth * 0.55f;
        ropeLine.material = new Material(Shader.Find("Sprites/Default"));
        ropeLine.startColor = ropeColor;
        ropeLine.endColor = ropeColor;
        ropeLine.sortingOrder = 15;
        ropeLine.enabled = false;

        hookVisual = new GameObject("HookTip").transform;
        hookVisual.SetParent(transform);
        var hookSprite = hookVisual.gameObject.AddComponent<SpriteRenderer>();
        hookSprite.sprite = CombatSpriteUtil.CreateRectSprite(6, 6, hookColor);
        hookSprite.sortingOrder = 16;
        hookVisual.gameObject.SetActive(false);
    }

    void Update()
    {
        if (PlayerMenuUI.IsMenuOpen || TradeUI.IsTradeOpen || grappleAction == null)
            return;

        switch (phase)
        {
            case GrapplePhase.Idle:
                if (grappleAction.WasPressedThisFrame())
                    TryEnterSelection();
                break;

            case GrapplePhase.Selecting:
                if (!grappleAction.IsPressed())
                {
                    ConfirmSelection();
                    break;
                }

                UpdateSelectionFromInput();
                UpdateSelectionVisuals();
                UpdatePreviewRope();
                break;
        }
    }

    void FixedUpdate()
    {
        if (phase == GrapplePhase.Snapping)
            TickSnapping();
        else if (phase == GrapplePhase.Launching)
            TickLaunching();
        else if (phase == GrapplePhase.Selecting)
            rb.linearVelocity = Vector2.zero;
    }

    void TryEnterSelection()
    {
        if (movement != null && movement.IsGrappleBlocked())
            return;

        GatherSelectablePoints();
        if (selectablePoints.Count == 0)
            return;

        storedVelocity = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        selectedPoint = FindNearestPoint();
        phase = GrapplePhase.Selecting;

        GrappleSlowMotion.Enter(slowMotionScale);
        ropeLine.enabled = true;
        hookVisual.gameObject.SetActive(true);
        UpdateSelectionVisuals();
        UpdatePreviewRope();
    }

    void GatherSelectablePoints()
    {
        selectablePoints.Clear();
        Vector2 playerPos = transform.position;

        foreach (GrapplePoint point in GrapplePoint.ActivePoints)
        {
            if (point == null)
                continue;

            if (Vector2.Distance(playerPos, point.Anchor) <= selectionRadius)
                selectablePoints.Add(point);
        }
    }

    void UpdateSelectionFromInput()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.sqrMagnitude < directionSelectThreshold * directionSelectThreshold)
            return;

        GrapplePoint directional = SelectPointByDirection(input);
        if (directional != null)
            selectedPoint = directional;
    }

    GrapplePoint SelectPointByDirection(Vector2 input)
    {
        Vector2 dir = input.normalized;
        GrapplePoint best = null;
        float bestScore = -999f;

        foreach (GrapplePoint point in selectablePoints)
        {
            Vector2 toPoint = point.Anchor - (Vector2)transform.position;
            float distance = toPoint.magnitude;
            if (distance < 0.01f)
                continue;

            Vector2 pointDir = toPoint / distance;
            float alignment = Vector2.Dot(pointDir, dir);
            float score = alignment - distance * 0.015f;

            if (score > bestScore)
            {
                bestScore = score;
                best = point;
            }
        }

        return bestScore > 0.05f ? best : selectedPoint;
    }

    GrapplePoint FindNearestPoint()
    {
        GrapplePoint best = null;
        float bestDistance = float.MaxValue;
        Vector2 playerPos = transform.position;

        foreach (GrapplePoint point in selectablePoints)
        {
            float distance = Vector2.Distance(playerPos, point.Anchor);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = point;
        }

        return best;
    }

    void UpdateSelectionVisuals()
    {
        foreach (GrapplePoint point in GrapplePoint.ActivePoints)
        {
            if (point == null)
                continue;

            if (point == selectedPoint && selectablePoints.Contains(point))
                point.SetHighlight(GrapplePoint.HighlightState.Selected);
            else if (selectablePoints.Contains(point))
                point.SetHighlight(GrapplePoint.HighlightState.Available);
            else
                point.SetHighlight(GrapplePoint.HighlightState.Normal);
        }
    }

    void UpdatePreviewRope()
    {
        if (selectedPoint == null)
        {
            ropeLine.enabled = false;
            return;
        }

        ropeLine.enabled = true;
        ropeLine.startColor = previewRopeColor;
        ropeLine.endColor = previewRopeColor;
        hookVisual.position = selectedPoint.Anchor;
        UpdateRope(hookOrigin.position, selectedPoint.Anchor);
    }

    void ConfirmSelection()
    {
        GrappleSlowMotion.Exit();

        if (selectedPoint == null)
        {
            CancelSelection();
            return;
        }

        activePoint = selectedPoint;
        anchorPoint = activePoint.Anchor;
        hookTip = hookOrigin.position;
        phaseTimer = activePoint.SnapDuration;
        phase = GrapplePhase.Snapping;

        ropeLine.startColor = ropeColor;
        ropeLine.endColor = ropeColor;
        ResetPointHighlights();
    }

    void CancelSelection()
    {
        GrappleSlowMotion.Exit();
        phase = GrapplePhase.Idle;
        selectedPoint = null;
        selectablePoints.Clear();
        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = storedVelocity;
        ropeLine.enabled = false;
        hookVisual.gameObject.SetActive(false);
        ResetPointHighlights();
    }

    void ResetPointHighlights()
    {
        foreach (GrapplePoint point in GrapplePoint.ActivePoints)
            point?.SetHighlight(GrapplePoint.HighlightState.Normal);
    }

    void TickSnapping()
    {
        phaseTimer -= Time.fixedDeltaTime;

        float duration = activePoint != null ? activePoint.SnapDuration : 0.1f;
        float t = 1f - Mathf.Clamp01(phaseTimer / duration);
        hookTip = Vector2.Lerp(hookOrigin.position, anchorPoint, t);
        UpdateRope(hookOrigin.position, hookTip);
        hookVisual.position = hookTip;

        if (phaseTimer > 0f)
            return;

        BeginLaunch();
    }

    void BeginLaunch()
    {
        if (activePoint == null)
        {
            EndGrapple();
            return;
        }

        Vector2 direction = activePoint.LaunchDirection;
        float speed = Mathf.Max(activePoint.LaunchSpeed * launchSpeedMultiplier, minLaunchSpeed);
        launchVelocity = direction * speed;

        rb.linearVelocity = launchVelocity;
        rb.gravityScale = launchGravityScale;

        phase = GrapplePhase.Launching;
        phaseTimer = launchControlDuration;

        UpdateRope(hookOrigin.position, anchorPoint);
    }

    void TickLaunching()
    {
        phaseTimer -= Time.fixedDeltaTime;

        float alongLaunch = Vector2.Dot(rb.linearVelocity, launchVelocity.normalized);
        if (alongLaunch < launchVelocity.magnitude * 0.85f)
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, launchVelocity, 0.2f);

        UpdateRope(hookOrigin.position, anchorPoint);
        hookVisual.position = anchorPoint;

        if (phaseTimer > 0f)
            return;

        EndGrapple();
    }

    void EndGrapple()
    {
        phase = GrapplePhase.Idle;
        activePoint = null;
        selectedPoint = null;
        rb.gravityScale = defaultGravityScale;
        ropeLine.enabled = false;
        hookVisual.gameObject.SetActive(false);
        ResetPointHighlights();
    }

    void UpdateRope(Vector2 start, Vector2 end)
    {
        ropeLine.SetPosition(0, start);
        ropeLine.SetPosition(1, end);
    }
}
