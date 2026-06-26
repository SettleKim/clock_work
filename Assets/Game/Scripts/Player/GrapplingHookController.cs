using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClockWork.Game
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class GrapplingHookController : MonoBehaviour
    {
        [Header("Selection")]
        [SerializeField] Transform hookOrigin;
        [SerializeField] float selectionRadius = 14f;
        [SerializeField] float slowMotionScale = 0.16f;
        [SerializeField] float directionSelectThreshold = 0.2f;

        [Header("Anchor Attach")]
        [SerializeField] float anchorAttachDuration = 0.18f;
        [SerializeField] float anchorReleaseMoveSpeed = 8f;
        [SerializeField] float anchorReleaseJumpForce = 14f;

        [Header("Momentum Launch")]
        [SerializeField] float launchGravityScale = 0.12f;
        [SerializeField] float launchSpeedMultiplier = 1f;
        [SerializeField] float minLaunchSpeed = 24f;
        [SerializeField] float momentumPassRadius = 0.45f;
        [SerializeField] float momentumMaxDuration = 4f;
        [SerializeField] float momentumPreserveDuration = 0.45f;

        [Header("Visual")]
        [SerializeField] float ropeWidth = 0.07f;
        [SerializeField] Color ropeColor = new(0.82f, 0.85f, 0.9f, 0.95f);
        [SerializeField] Color previewRopeColor = new(0.45f, 0.95f, 1f, 0.75f);

        enum GrapplePhase { Idle, Selecting, Snapping, AnchorAttach, AnchorHold, MomentumLaunch }

        Rigidbody2D rb;
        PlayerInput playerInput;
        InputAction interactAction;
        InputAction moveAction;
        InputAction jumpAction;

        GrapplePhase phase = GrapplePhase.Idle;
        GrapplePoint activePoint;
        GrapplePoint selectedPoint;
        readonly List<GrapplePoint> selectablePoints = new();
        Vector2 ropeTarget;
        Vector2 hookTip;
        Vector2 attachStartPosition;
        Vector2 launchVelocity;
        Vector2 storedVelocity;
        Vector2 momentumAnchor;
        Vector2 momentumLaunchDirection;
        float phaseTimer;
        float momentumElapsed;
        bool momentumApproachingAnchor;
        float momentumPreserveTimer;
        float defaultGravityScale;
        LineRenderer ropeLine;

        public bool IsActive => phase != GrapplePhase.Idle;
        public bool IsSelecting => phase == GrapplePhase.Selecting;
        public bool ShouldPreserveLaunchMomentum => momentumPreserveTimer > 0f;

        public void ClearMomentumPreserve() => momentumPreserveTimer = 0f;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            playerInput = GetComponent<PlayerInput>();
            defaultGravityScale = rb.gravityScale;

            interactAction = playerInput.actions["Interact"];
            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];

            if (hookOrigin == null)
            {
                var origin = transform.Find("HookOrigin");
                hookOrigin = origin != null ? origin : transform;
            }

            CreateRopeVisual();
        }

        void OnDisable()
        {
            if (phase == GrapplePhase.Selecting)
                CancelSelection();
            else if (phase != GrapplePhase.Idle)
                EndGrapple();
        }

        void CreateRopeVisual()
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
        }

        void Update()
        {
            if (interactAction == null)
                return;

            switch (phase)
            {
                case GrapplePhase.Idle:
                    if (interactAction.WasPressedThisFrame())
                        TryEnterSelection();
                    break;

                case GrapplePhase.Selecting:
                    if (interactAction.WasPressedThisFrame())
                    {
                        ConfirmSelection();
                        break;
                    }

                    UpdateSelectionFromInput();
                    UpdateSelectionVisuals();
                    UpdatePreviewRope();
                    break;

                case GrapplePhase.AnchorHold:
                    if (ShouldReleaseFromAnchor())
                        ReleaseFromAnchorHold();
                    break;
            }
        }

        void FixedUpdate()
        {
            if (momentumPreserveTimer > 0f && phase == GrapplePhase.Idle)
                momentumPreserveTimer -= Time.fixedDeltaTime;

            switch (phase)
            {
                case GrapplePhase.Selecting:
                    rb.linearVelocity = Vector2.zero;
                    break;
                case GrapplePhase.Snapping:
                    TickSnapping();
                    break;
                case GrapplePhase.AnchorAttach:
                    TickAnchorAttach();
                    break;
                case GrapplePhase.AnchorHold:
                    TickAnchorHold();
                    break;
                case GrapplePhase.MomentumLaunch:
                    TickMomentumLaunch();
                    break;
            }
        }

        void TryEnterSelection()
        {
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

                float distance = Vector2.Distance(playerPos, point.Anchor);
                if (distance <= selectionRadius && distance <= point.UseRadius)
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
                float score = Vector2.Dot(pointDir, dir) - distance * 0.015f;

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
            Vector2 target = GetRopeTarget(selectedPoint);
            UpdateRope(hookOrigin.position, target);
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
            ropeTarget = GetRopeTarget(activePoint);
            hookTip = hookOrigin.position;
            phaseTimer = activePoint.RopeSnapDuration;
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

            float duration = activePoint != null ? activePoint.RopeSnapDuration : 0.1f;
            float t = 1f - Mathf.Clamp01(phaseTimer / duration);
            hookTip = Vector2.Lerp(hookOrigin.position, ropeTarget, t);
            UpdateRope(hookOrigin.position, hookTip);

            if (phaseTimer > 0f)
                return;

            BeginAfterSnap();
        }

        void BeginAfterSnap()
        {
            if (activePoint == null)
            {
                EndGrapple();
                return;
            }

            if (activePoint.IsAnchor)
            {
                attachStartPosition = transform.position;
                phaseTimer = anchorAttachDuration;
                phase = GrapplePhase.AnchorAttach;
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0f;
                UpdateRope(hookOrigin.position, ropeTarget);
                return;
            }

            Vector2 direction = GetMomentumLaunchDirection();
            float speed = Mathf.Max(storedVelocity.magnitude, activePoint.LaunchSpeed * launchSpeedMultiplier, minLaunchSpeed);
            launchVelocity = direction * speed;

            momentumAnchor = activePoint.Anchor;
            momentumLaunchDirection = direction;
            momentumApproachingAnchor = Vector2.Dot(momentumAnchor - (Vector2)transform.position, direction) > 0f;
            momentumElapsed = 0f;

            rb.linearVelocity = launchVelocity;
            rb.gravityScale = launchGravityScale;
            phase = GrapplePhase.MomentumLaunch;
            UpdateRope(hookOrigin.position, ropeTarget);
        }

        Vector2 GetMomentumLaunchDirection()
        {
            Vector2 anchor = activePoint.Anchor;
            Vector2 toAnchor = anchor - (Vector2)transform.position;

            if (toAnchor.sqrMagnitude < 0.0001f)
                return activePoint.LaunchDirection;

            return toAnchor.normalized;
        }

        bool ShouldReleaseFromAnchor()
        {
            if (jumpAction != null && jumpAction.WasPressedThisFrame())
                return true;

            Vector2 move = moveAction.ReadValue<Vector2>();
            return move.sqrMagnitude >= directionSelectThreshold * directionSelectThreshold;
        }

        void ReleaseFromAnchorHold()
        {
            Vector2 releaseVelocity = Vector2.zero;

            if (jumpAction != null && jumpAction.WasPressedThisFrame())
                releaseVelocity.y = anchorReleaseJumpForce;

            Vector2 move = moveAction.ReadValue<Vector2>();
            if (move.sqrMagnitude >= directionSelectThreshold * directionSelectThreshold)
                releaseVelocity += move.normalized * anchorReleaseMoveSpeed;

            phase = GrapplePhase.Idle;
            activePoint = null;
            selectedPoint = null;
            rb.gravityScale = defaultGravityScale;
            rb.linearVelocity = releaseVelocity;
            ropeLine.enabled = false;
            ResetPointHighlights();
        }

        void TickAnchorHold()
        {
            if (activePoint == null)
            {
                EndGrapple();
                return;
            }

            rb.MovePosition(activePoint.AttachPosition);
            rb.linearVelocity = Vector2.zero;
            UpdateRope(hookOrigin.position, ropeTarget);
        }

        void TickAnchorAttach()
        {
            phaseTimer -= Time.fixedDeltaTime;

            float duration = Mathf.Max(anchorAttachDuration, 0.01f);
            float t = 1f - Mathf.Clamp01(phaseTimer / duration);
            Vector2 nextPosition = Vector2.Lerp(attachStartPosition, activePoint.AttachPosition, t);
            rb.MovePosition(nextPosition);
            rb.linearVelocity = Vector2.zero;
            UpdateRope(hookOrigin.position, ropeTarget);

            if (phaseTimer > 0f)
                return;

            rb.linearVelocity = Vector2.zero;
            phase = GrapplePhase.AnchorHold;
        }

        void TickMomentumLaunch()
        {
            momentumElapsed += Time.fixedDeltaTime;
            UpdateRope(hookOrigin.position, ropeTarget);

            if (HasReachedMomentumAnchor() || momentumElapsed >= momentumMaxDuration)
                ReleaseMomentumAtAnchor();
        }

        bool HasReachedMomentumAnchor()
        {
            Vector2 toAnchor = momentumAnchor - rb.position;
            if (toAnchor.sqrMagnitude <= momentumPassRadius * momentumPassRadius)
                return true;

            return momentumApproachingAnchor && Vector2.Dot(toAnchor, momentumLaunchDirection) <= 0f;
        }

        void ReleaseMomentumAtAnchor()
        {
            momentumPreserveTimer = momentumPreserveDuration;
            phase = GrapplePhase.Idle;
            activePoint = null;
            selectedPoint = null;
            rb.gravityScale = defaultGravityScale;
            ropeLine.enabled = false;
            ResetPointHighlights();
        }

        void EndGrapple()
        {
            phase = GrapplePhase.Idle;
            activePoint = null;
            selectedPoint = null;
            rb.gravityScale = defaultGravityScale;
            ropeLine.enabled = false;
            ResetPointHighlights();
        }

        Vector2 GetRopeTarget(GrapplePoint point)
        {
            return point.IsAnchor ? point.AttachPosition : point.Anchor;
        }

        void UpdateRope(Vector2 start, Vector2 end)
        {
            ropeLine.SetPosition(0, start);
            ropeLine.SetPosition(1, end);
        }
    }
}
