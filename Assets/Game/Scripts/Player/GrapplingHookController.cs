using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClockWork.Game
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class GrapplingHookController : MonoBehaviour
    {
        public enum GrappleSelectionMode
        {
            Normal,
            SlowMotion
        }

        [Header("Selection")]
        [SerializeField] Transform hookOrigin;
        [SerializeField] float selectionRadius = 14f;
        [SerializeField] float slowMotionScale = 0.16f;
        [SerializeField] float directionSelectThreshold = 0.2f;

        [Header("Selection Slow-Mo")]
        [SerializeField] GrappleSelectionMode selectionMode = GrappleSelectionMode.Normal;
        [SerializeField] bool slowMoUnlockedByFeature;
        [SerializeField] Key debugToggleSlowMoKey = Key.G;

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
        [Tooltip("앵커 통과 직후 full-speed 유지 시간. 0이면 즉시 감쇠 시작.")]
        [SerializeField] float momentumPreserveDuration = 0.12f;
        [Tooltip("관성 수평 속도가 walk 속도까지 줄어드는 속도 (units/s).")]
        [SerializeField] float momentumBleedRate = 14f;
        [Tooltip("walk 속도 대비 여유 비율. 이 이하로 내려오면 일반 이동으로 핸드오프.")]
        [SerializeField] float momentumHandoffEpsilon = 0.12f;
        [Tooltip("발사 중력 → 기본 중력 복구 시간.")]
        [SerializeField] float gravityRestoreDuration = 0.35f;

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
        InputAction cancelAction;

        GrapplePhase phase = GrapplePhase.Idle;
        GrapplePoint activePoint;
        GrapplePoint selectedPoint;
        readonly List<GrapplePoint> selectablePoints = new();
        Vector2 ropeTarget;
        Vector2 hookTip;
        Vector2 attachStartPosition;
        Vector2 launchVelocity;
        Vector2 momentumAnchor;
        Vector2 momentumLaunchDirection;
        float phaseTimer;
        float momentumElapsed;
        bool momentumApproachingAnchor;
        float carriedHorizontalSpeed;
        bool carriedMomentumActive;
        float momentumCoastTimer;
        float gravityBlendTimer;
        bool gravityBlendActive;
        bool selectionSlowMoActive;
        float defaultGravityScale;
        LineRenderer ropeLine;

        public bool IsActive => phase != GrapplePhase.Idle;
        public bool IsSelecting => phase == GrapplePhase.Selecting;
        public bool BlocksPlayerMovement =>
            phase == GrapplePhase.Snapping ||
            phase == GrapplePhase.AnchorAttach ||
            phase == GrapplePhase.AnchorHold ||
            phase == GrapplePhase.MomentumLaunch;
        public bool AllowsPlayerJump =>
            phase == GrapplePhase.Selecting ||
            phase == GrapplePhase.Snapping ||
            phase == GrapplePhase.MomentumLaunch;
        public bool ShouldPreserveLaunchMomentum => carriedMomentumActive;
        public bool HasCarriedMomentum => carriedMomentumActive;
        public GrappleSelectionMode CurrentSelectionMode => selectionMode;

        public void ClearMomentumPreserve() => EndCarriedMomentum();

        /// <summary>
        /// 그랩 관성 수평 속도 감쇠. PlayerMovement FixedUpdate에서 호출.
        /// </summary>
        public float BleedHorizontalMomentum(
            float moveSpeed,
            float horizontalInput,
            float deltaTime,
            float airAcceleration,
            float groundAcceleration,
            float groundDeceleration,
            bool isGrounded)
        {
            if (!carriedMomentumActive)
                return rb.linearVelocity.x;

            float vx = rb.linearVelocity.x;

            if (Mathf.Abs(horizontalInput) > 0.05f && Mathf.Abs(vx) > 0.01f
                && Mathf.Sign(horizontalInput) != Mathf.Sign(vx))
            {
                EndCarriedMomentum();
                return vx;
            }

            if (momentumCoastTimer > 0f)
            {
                momentumCoastTimer -= deltaTime;
                carriedHorizontalSpeed = vx;

                if (Mathf.Abs(horizontalInput) > 0.05f && Mathf.Sign(horizontalInput) == Mathf.Sign(vx))
                {
                    float targetSpeed = horizontalInput * moveSpeed;
                    float accelerationRate = isGrounded
                        ? Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration
                        : airAcceleration;
                    float velocityChange = (targetSpeed - vx) * accelerationRate * deltaTime;
                    if (Mathf.Abs(vx + velocityChange) > Mathf.Abs(vx))
                        vx += velocityChange;
                }

                carriedHorizontalSpeed = vx;
                return vx;
            }

            float travelDirection = Mathf.Sign(carriedHorizontalSpeed);
            if (travelDirection == 0f)
                travelDirection = Mathf.Sign(vx);
            if (travelDirection == 0f)
                travelDirection = 1f;

            float walkSpeedAlongTravel = travelDirection * moveSpeed;
            carriedHorizontalSpeed = Mathf.MoveTowards(
                carriedHorizontalSpeed, walkSpeedAlongTravel, momentumBleedRate * deltaTime);

            vx = carriedHorizontalSpeed;

            if (Mathf.Abs(horizontalInput) > 0.05f && Mathf.Sign(horizontalInput) == travelDirection)
            {
                float targetSpeed = horizontalInput * moveSpeed;
                float accelerationRate = isGrounded
                    ? Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration
                    : airAcceleration;
                float velocityChange = (targetSpeed - vx) * accelerationRate * deltaTime;

                if (Mathf.Abs(vx) > moveSpeed)
                {
                    if (Mathf.Abs(vx + velocityChange) > Mathf.Abs(vx))
                        vx += velocityChange;
                }
                else
                {
                    vx += velocityChange;
                }

                carriedHorizontalSpeed = vx;
            }

            float handoffThreshold = moveSpeed * (1f + momentumHandoffEpsilon);
            if (Mathf.Abs(carriedHorizontalSpeed) <= handoffThreshold)
                EndCarriedMomentum();

            return vx;
        }

        void EndCarriedMomentum()
        {
            carriedMomentumActive = false;
            carriedHorizontalSpeed = 0f;
            momentumCoastTimer = 0f;
        }

        void BeginGravityBlend()
        {
            gravityBlendTimer = 0f;
            gravityBlendActive = true;
        }

        void TickGravityBlend()
        {
            if (!gravityBlendActive)
                return;

            gravityBlendTimer += Time.fixedDeltaTime;
            float duration = Mathf.Max(gravityRestoreDuration, 0.01f);
            float t = Mathf.Clamp01(gravityBlendTimer / duration);
            rb.gravityScale = Mathf.Lerp(launchGravityScale, defaultGravityScale, t);

            if (t >= 1f)
            {
                rb.gravityScale = defaultGravityScale;
                gravityBlendActive = false;
            }
        }

        /// <summary>연산 가속 등 기능 해금 시 호출. 선택 슬로모 모드로 전환.</summary>
        public void UnlockSelectionSlowMotion()
        {
            slowMoUnlockedByFeature = true;
            selectionMode = GrappleSelectionMode.SlowMotion;
        }

        public void SetSelectionMode(GrappleSelectionMode mode) => selectionMode = mode;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            playerInput = GetComponent<PlayerInput>();
            defaultGravityScale = rb.gravityScale;

            interactAction = playerInput.actions["Interact"];
            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
            cancelAction = playerInput.actions.FindAction("GrappleCancel", false);

            if (slowMoUnlockedByFeature)
                selectionMode = GrappleSelectionMode.SlowMotion;

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

            HandleDebugSlowMoToggle();

            switch (phase)
            {
                case GrapplePhase.Idle:
                    if (interactAction.WasPressedThisFrame())
                        TryEnterSelection();
                    break;

                case GrapplePhase.Selecting:
                    if (cancelAction != null && cancelAction.WasPressedThisFrame())
                    {
                        CancelSelection();
                        break;
                    }

                    if (interactAction.WasReleasedThisFrame())
                    {
                        ConfirmSelection();
                        break;
                    }

                    UpdateSelectionPoints();
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

        void HandleDebugSlowMoToggle()
        {
            if (slowMoUnlockedByFeature)
                return;

            if (Keyboard.current == null)
                return;

            if (Keyboard.current[debugToggleSlowMoKey].wasPressedThisFrame)
            {
                selectionMode = selectionMode == GrappleSelectionMode.Normal
                    ? GrappleSelectionMode.SlowMotion
                    : GrappleSelectionMode.Normal;
            }
        }

        bool ShouldUseSelectionSlowMo() => selectionMode == GrappleSelectionMode.SlowMotion;

        void EnterSelectionSlowMo()
        {
            if (!ShouldUseSelectionSlowMo() || selectionSlowMoActive)
                return;

            GrappleSlowMotion.Enter(slowMotionScale);
            selectionSlowMoActive = true;
        }

        void ExitSelectionSlowMo()
        {
            if (!selectionSlowMoActive)
                return;

            GrappleSlowMotion.Exit();
            selectionSlowMoActive = false;
        }

        void FixedUpdate()
        {
            TickGravityBlend();

            switch (phase)
            {
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

            selectedPoint = FindNearestPoint();
            phase = GrapplePhase.Selecting;

            EnterSelectionSlowMo();
            ropeLine.enabled = true;
            UpdateSelectionVisuals();
            UpdatePreviewRope();
        }

        void UpdateSelectionPoints()
        {
            GatherSelectablePoints();

            if (selectedPoint != null && selectablePoints.Contains(selectedPoint))
                return;

            selectedPoint = selectablePoints.Count > 0 ? FindNearestPoint() : null;
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
            ExitSelectionSlowMo();

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
            ExitSelectionSlowMo();
            phase = GrapplePhase.Idle;
            selectedPoint = null;
            selectablePoints.Clear();
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
            float speed = Mathf.Max(rb.linearVelocity.magnitude, activePoint.LaunchSpeed * launchSpeedMultiplier, minLaunchSpeed);
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
            carriedHorizontalSpeed = rb.linearVelocity.x;
            carriedMomentumActive = true;
            momentumCoastTimer = momentumPreserveDuration;
            BeginGravityBlend();

            phase = GrapplePhase.Idle;
            activePoint = null;
            selectedPoint = null;
            ropeLine.enabled = false;
            ResetPointHighlights();
        }

        void EndGrapple()
        {
            ExitSelectionSlowMo();
            EndCarriedMomentum();
            gravityBlendActive = false;
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
