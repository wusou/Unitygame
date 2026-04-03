using UnityEngine;
using UnityEngine.InputSystem;

namespace SwiftRunner
{
    public sealed class SwiftRunnerPlayerController : MonoBehaviour
    {
        private const string InputAssetResourcePath = "Input/SwiftRunnerInput";
        private const string InputMapName = "Runner";

        [SerializeField] private float horizontalAcceleration = 48f;
        [SerializeField] private float laneChangeSpeed = 18f;
        [SerializeField] private float gravity = -36f;
        [SerializeField] private float jumpVelocity = 13.6f;
        [SerializeField] private float doubleJumpVelocity = 12.8f;
        [SerializeField] private float stompVelocity = -24f;
        [SerializeField] private float stompBounceVelocity = 13.4f;
        [SerializeField] private float slideDuration = 0.46f;
        [SerializeField] private float slashReach = 2.45f;
        [SerializeField] private float minimumSlowdownSpeed = 3.35f;
        [SerializeField] private float maximumBoostSpeedOffset = 2.8f;
        [SerializeField] private float sprintBoost = 4.5f;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer afterImageRenderer;
        [SerializeField] private SpriteRenderer landingMarkerRenderer;
        [SerializeField] private Rigidbody2D runnerBody;
        [SerializeField] private CapsuleCollider2D bodyCollider;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite walkASprite;
        [SerializeField] private Sprite walkBSprite;
        [SerializeField] private Sprite jumpSprite;
        [SerializeField] private Sprite duckSprite;
        [SerializeField] private Sprite hitSprite;

        private SwiftRunnerGameController controller;
        private InputActionAsset inputActions;
        private InputActionMap runnerActionMap;
        private PlayerInput playerInput;

        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction stompAction;
        private InputAction sprintHoldAction;
        private InputAction sprintToggleAction;

        private float forwardX;
        private float currentLaneY;
        private float verticalOffset;
        private float verticalVelocity;
        private float currentSpeed;
        private float currentHorizontalVelocity;

        private int currentLaneIndex;
        private int targetLaneIndex;
        private float previousVerticalInput;
        private float walkAnimationTimer;
        private float slideTimer;
        private float slashTimer;
        private bool stompQueued;
        private bool grounded = true;
        private bool alive = true;
        private bool canDoubleJump;
        private bool sprintToggled;

        public int CurrentLaneIndex => currentLaneIndex;
        public float ForwardX => forwardX;
        public float CurrentSpeed => Mathf.Abs(currentHorizontalVelocity);
        public float VerticalOffset => verticalOffset;
        public bool IsSliding => slideTimer > 0f;
        public bool IsSlashing => slashTimer > 0f;
        public bool IsAlive => alive;
        public bool IsDescending => !grounded && verticalVelocity <= 0f;
        public float HitboxHalfWidth => 0.45f;
        public float VerticalVelocity => verticalVelocity;

        public string ControlSummary => ResolveControlSummary();

        public void Initialize(SwiftRunnerGameController gameController, int startLaneIndex)
        {
            controller = gameController;
            currentLaneIndex = startLaneIndex;
            targetLaneIndex = startLaneIndex;
            currentLaneY = controller.GetLaneY(startLaneIndex);
            forwardX = controller.StartX;
            currentSpeed = controller.ResolveTargetRunSpeed(forwardX);
            currentHorizontalVelocity = 0f;
            walkAnimationTimer = 0f;
            canDoubleJump = true;
            sprintToggled = false;
            bodyRenderer ??= GetComponent<SpriteRenderer>();
            runnerBody ??= GetComponent<Rigidbody2D>();
            bodyCollider ??= GetComponent<CapsuleCollider2D>();
            landingMarkerRenderer ??= transform.Find("LandingMarker")?.GetComponent<SpriteRenderer>();
            ApplyVisualState();
        }

        public void ConfigureVisuals(
            SpriteRenderer primaryRenderer,
            SpriteRenderer trailRenderer,
            SpriteRenderer landingMarker,
            Sprite idle,
            Sprite walkA,
            Sprite walkB,
            Sprite jump,
            Sprite duck,
            Sprite hit)
        {
            bodyRenderer = primaryRenderer;
            afterImageRenderer = trailRenderer;
            landingMarkerRenderer = landingMarker;
            idleSprite = idle;
            walkASprite = walkA;
            walkBSprite = walkB;
            jumpSprite = jump;
            duckSprite = duck;
            hitSprite = hit;

            if (bodyRenderer != null && idleSprite != null)
            {
                bodyRenderer.sprite = idleSprite;
                bodyRenderer.color = Color.white;
            }

            if (afterImageRenderer != null)
            {
                afterImageRenderer.sprite = idleSprite;
            }

            if (landingMarkerRenderer != null)
            {
                landingMarkerRenderer.enabled = false;
            }
        }

        public void ConfigurePhysics(Rigidbody2D physicsBody, CapsuleCollider2D collisionBody)
        {
            runnerBody = physicsBody;
            bodyCollider = collisionBody;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            inputActions = Resources.Load<InputActionAsset>(InputAssetResourcePath);
            if (inputActions == null)
            {
                Debug.LogError($"[SwiftRunner] Missing Input Actions asset at Resources/{InputAssetResourcePath}.inputactions", this);
                enabled = false;
                return;
            }

            playerInput ??= GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.actions = inputActions;
                playerInput.defaultActionMap = InputMapName;
                playerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;
            }

            runnerActionMap = inputActions.FindActionMap(InputMapName, throwIfNotFound: true);
            moveAction = runnerActionMap.FindAction("Move", throwIfNotFound: true);
            jumpAction = runnerActionMap.FindAction("Jump", throwIfNotFound: true);
            stompAction = runnerActionMap.FindAction("Stomp", throwIfNotFound: true);
            sprintHoldAction = runnerActionMap.FindAction("SprintHold", throwIfNotFound: true);
            sprintToggleAction = runnerActionMap.FindAction("SprintToggle", throwIfNotFound: true);
            runnerActionMap.Enable();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            runnerActionMap?.Disable();
            moveAction = null;
            jumpAction = null;
            stompAction = null;
            sprintHoldAction = null;
            sprintToggleAction = null;
            runnerActionMap = null;
            inputActions = null;
        }

        private static string GetPrimaryBindingLabel(InputAction action, string bindingGroup)
        {
            if (action == null)
            {
                return "?";
            }

            for (var index = 0; index < action.bindings.Count; index++)
            {
                var binding = action.bindings[index];
                if (binding.isComposite || binding.isPartOfComposite)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(binding.groups) && binding.groups.Contains(bindingGroup))
                {
                    return NormalizeBindingLabel(action.GetBindingDisplayString(index), bindingGroup);
                }
            }

            return action.bindings.Count > 0 ? NormalizeBindingLabel(action.GetBindingDisplayString(0), bindingGroup) : "?";
        }

        private string ResolveControlSummary()
        {
            var controlScheme = playerInput != null && !string.IsNullOrWhiteSpace(playerInput.currentControlScheme)
                ? playerInput.currentControlScheme
                : "Keyboard&Mouse";

            if (controlScheme == "Gamepad")
            {
                return $"手柄: 跳跃 {GetPrimaryBindingLabel(jumpAction, controlScheme)}  下踩/滑铲 {GetPrimaryBindingLabel(stompAction, controlScheme)}  加速 {GetPrimaryBindingLabel(sprintHoldAction, controlScheme)}  常驻 {GetPrimaryBindingLabel(sprintToggleAction, controlScheme)}";
            }

            return $"键盘: 跳跃 {GetPrimaryBindingLabel(jumpAction, controlScheme)}  下踩/滑铲 {GetPrimaryBindingLabel(stompAction, controlScheme)}  加速 {GetPrimaryBindingLabel(sprintHoldAction, controlScheme)}  常驻 {GetPrimaryBindingLabel(sprintToggleAction, controlScheme)}";
        }

        private static string NormalizeBindingLabel(string label, string bindingGroup)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return "?";
            }

            if (bindingGroup != "Gamepad")
            {
                return label;
            }

            return label
                .Replace("Button South", "A")
                .Replace("Button East", "B")
                .Replace("Button West", "X")
                .Replace("Button North", "Y")
                .Replace("Left Shoulder", "LB")
                .Replace("Right Shoulder", "RB")
                .Replace("Left Stick", "LS");
        }

        private void Update()
        {
            if (controller == null || controller.GameEnded || !alive)
            {
                return;
            }

            var moveVector = moveAction.ReadValue<Vector2>();
            HandleLaneInput(moveVector.y);

            if (sprintToggleAction.WasPressedThisFrame())
            {
                sprintToggled = !sprintToggled;
            }

            if (jumpAction.WasPressedThisFrame())
            {
                HandleJump();
            }

            if (stompAction.WasPressedThisFrame())
            {
                HandleStomp();
            }

            slideTimer = Mathf.Max(0f, slideTimer - Time.deltaTime);
            slashTimer = Mathf.Max(0f, slashTimer - Time.deltaTime);

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                controller.ResolveTargetRunSpeed(forwardX),
                controller.ResolveRecoverySpeed() * Time.deltaTime);

            var sprintBoost = ResolveSprintBoost();
            var targetHorizontalVelocity = moveVector.x * (currentSpeed + sprintBoost);
            currentHorizontalVelocity = Mathf.MoveTowards(currentHorizontalVelocity, targetHorizontalVelocity, horizontalAcceleration * Time.deltaTime);
            forwardX += currentHorizontalVelocity * Time.deltaTime;
            walkAnimationTimer += Time.deltaTime * Mathf.Max(0f, Mathf.Abs(currentHorizontalVelocity) * 0.48f);
            currentLaneY = Mathf.MoveTowards(currentLaneY, controller.GetLaneY(targetLaneIndex), laneChangeSpeed * Time.deltaTime);

            if (!grounded)
            {
                verticalVelocity += gravity * Time.deltaTime;
                if (stompQueued)
                {
                    verticalVelocity = Mathf.Min(verticalVelocity, stompVelocity);
                }

                verticalOffset += verticalVelocity * Time.deltaTime;
                if (verticalOffset <= 0f)
                {
                    verticalOffset = 0f;
                    verticalVelocity = 0f;
                    grounded = true;
                    stompQueued = false;
                    canDoubleJump = true;
                }
            }

            var nextPosition = new Vector3(forwardX, currentLaneY + verticalOffset, 0f);
            if (runnerBody != null)
            {
                runnerBody.MovePosition(nextPosition);
            }
            else
            {
                transform.position = nextPosition;
            }

            ApplyVisualState();
            UpdateLandingMarker();

            if (stompQueued && controller.TryResolveStomp(this, out _))
            {
                BounceFromStomp();
            }

            controller.TickRunner(this, Time.deltaTime);
        }

        public void ApplySlowdown(float penalty)
        {
            currentSpeed = Mathf.Max(minimumSlowdownSpeed, currentSpeed - penalty);
        }

        public void ApplySpeedBoost(float bonusSpeed)
        {
            var targetCap = controller.ResolveTargetRunSpeed(forwardX) + maximumBoostSpeedOffset;
            currentSpeed = Mathf.Min(targetCap, currentSpeed + Mathf.Max(0f, bonusSpeed));
        }

        public void HandleDeath()
        {
            alive = false;
            transform.localScale = new Vector3(1.5f, 0.3f, 1f);
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponent<SpriteRenderer>();
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.color = new Color(0.75f, 0.54f, 0.42f, 1f);
                if (hitSprite != null)
                {
                    bodyRenderer.sprite = hitSprite;
                }
            }
        }

        public bool IsOverlappingX(float x, float halfWidth)
        {
            return Mathf.Abs(forwardX - x) <= HitboxHalfWidth + halfWidth;
        }

        public bool IsOverlappingCollider(Collider2D collider)
        {
            if (collider == null)
            {
                return false;
            }

            return GetBodyBounds().Intersects(collider.bounds);
        }

        public bool IsAboveColliderTop(Collider2D collider, float tolerance)
        {
            if (collider == null)
            {
                return false;
            }

            return GetBodyBounds().min.y >= collider.bounds.max.y - tolerance;
        }

        public bool IsLaneCompatible(int laneIndex)
        {
            return laneIndex < 0 || laneIndex == currentLaneIndex;
        }

        public bool ClearsHeight(float requiredHeight)
        {
            return verticalOffset >= requiredHeight;
        }

        public bool IsWithinLandingWindow(float contactHeight, float tolerance)
        {
            if (grounded)
            {
                return Mathf.Abs(verticalOffset - contactHeight) <= tolerance;
            }

            return IsDescending &&
                   verticalVelocity <= 0f &&
                   verticalOffset <= contactHeight + tolerance &&
                   verticalOffset >= contactHeight - tolerance;
        }

        private void HandleLaneInput(float verticalInput)
        {
            if (verticalInput > 0.5f && previousVerticalInput <= 0.5f)
            {
                targetLaneIndex = Mathf.Min(targetLaneIndex + 1, controller.LaneCenters.Count - 1);
            }
            else if (verticalInput < -0.5f && previousVerticalInput >= -0.5f)
            {
                targetLaneIndex = Mathf.Max(targetLaneIndex - 1, 0);
            }

            if (Mathf.Abs(currentLaneY - controller.GetLaneY(targetLaneIndex)) <= 0.02f)
            {
                currentLaneIndex = targetLaneIndex;
            }

            previousVerticalInput = verticalInput;
        }

        private void HandleJump()
        {
            if (!grounded)
            {
                if (!canDoubleJump)
                {
                    return;
                }

                canDoubleJump = false;
                stompQueued = false;
                verticalVelocity = doubleJumpVelocity;
                verticalOffset = Mathf.Max(verticalOffset, 0.18f);
                return;
            }

            if (controller.TryQuickSlash(this, slashReach))
            {
                slashTimer = 0.18f;
                return;
            }

            grounded = false;
            verticalVelocity = jumpVelocity;
            verticalOffset = Mathf.Max(verticalOffset, 0.12f);
            stompQueued = false;
            canDoubleJump = true;
        }

        private void HandleStomp()
        {
            if (!grounded)
            {
                stompQueued = true;
                canDoubleJump = false;
                return;
            }

            slideTimer = slideDuration;
        }

        private void BounceFromStomp()
        {
            grounded = false;
            stompQueued = false;
            verticalOffset = Mathf.Max(verticalOffset, 0.25f);
            verticalVelocity = stompBounceVelocity;
            canDoubleJump = true;
        }

        private float ResolveSprintBoost()
        {
            return (sprintToggled || sprintHoldAction.IsPressed()) ? sprintBoost : 0f;
        }

        private void ApplyVisualState()
        {
            var scaleY = 1f;
            if (IsSliding)
            {
                scaleY = 0.48f;
            }
            else if (!grounded)
            {
                scaleY = 1.12f;
            }
            else if (IsSlashing)
            {
                scaleY = 0.94f;
            }

            transform.localScale = new Vector3(1f, scaleY, 1f);
            UpdateSprites();
        }

        private void UpdateLandingMarker()
        {
            if (landingMarkerRenderer == null)
            {
                return;
            }

            if (grounded || !alive)
            {
                landingMarkerRenderer.enabled = false;
                return;
            }

            var markerPosition = new Vector3(forwardX, controller.GetLaneY(targetLaneIndex) - 0.92f, 0f);
            var markerColor = stompQueued
                ? new Color(1f, 0.78f, 0.26f, 0.9f)
                : new Color(0.98f, 0.96f, 0.62f, 0.65f);

            if (stompQueued && controller.TryGetStompPreview(this, out var previewEnemy))
            {
                markerPosition = new Vector3(previewEnemy.ForwardX, controller.GetLaneY(previewEnemy.LaneIndex) - 0.92f, 0f);
                markerColor = new Color(1f, 0.55f, 0.22f, 0.95f);
            }

            landingMarkerRenderer.enabled = true;
            landingMarkerRenderer.transform.position = markerPosition;
            landingMarkerRenderer.color = markerColor;
        }

        private Bounds GetBodyBounds()
        {
            if (bodyCollider != null)
            {
                return bodyCollider.bounds;
            }

            return new Bounds(transform.position, new Vector3(0.9f, 1.4f, 0f));
        }

        private void UpdateSprites()
        {
            if (bodyRenderer == null)
            {
                return;
            }

            Sprite targetSprite = idleSprite;
            if (!alive && hitSprite != null)
            {
                targetSprite = hitSprite;
            }
            else if (IsSliding && duckSprite != null)
            {
                targetSprite = duckSprite;
            }
            else if (!grounded && jumpSprite != null)
            {
                targetSprite = jumpSprite;
            }
            else if (walkASprite != null && walkBSprite != null)
            {
                targetSprite = Mathf.Repeat(walkAnimationTimer, 0.32f) < 0.16f
                    ? walkASprite
                    : walkBSprite;
            }

            if (targetSprite != null)
            {
                bodyRenderer.sprite = targetSprite;
            }

            if (alive)
            {
                bodyRenderer.color = Color.white;
                bodyRenderer.flipX = currentHorizontalVelocity < -0.05f;
            }

            if (afterImageRenderer == null)
            {
                return;
            }

            afterImageRenderer.sprite = bodyRenderer.sprite;
            afterImageRenderer.flipX = bodyRenderer.flipX;
            afterImageRenderer.color = alive
                ? new Color(1f, 0.82f, 0.22f, IsSliding ? 0.1f : 0.18f)
                : new Color(0.42f, 0.3f, 0.24f, 0.06f);

            if (Mathf.Abs(currentHorizontalVelocity) <= 0.12f && grounded && !IsSliding && !IsSlashing && idleSprite != null)
            {
                bodyRenderer.sprite = idleSprite;
                afterImageRenderer.sprite = idleSprite;
            }
        }
    }
}
