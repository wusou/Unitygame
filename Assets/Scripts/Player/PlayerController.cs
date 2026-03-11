using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移动参数")]
    [SerializeField] private float moveSpeed = 6.5f;
    [SerializeField] private float crouchMoveMultiplier = 0.45f;
    [SerializeField] private float jumpForce = 11f;
    [SerializeField] private int maxJumps = 2;

    [Header("地面检测")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("单向平台下落")]
    [SerializeField] private LayerMask oneWayPlatformLayer;
    [SerializeField] private float dropDownDuration = 0.3f;
    [SerializeField] private bool autoDetectOneWayByEffector = true;
    [SerializeField, Min(0.5f)] private float dropDownInitialSpeed = 2f;

    [Header("梯子攀爬")]
    [SerializeField] private LayerMask ladderLayer;
    [SerializeField] private float climbSpeed = 4.2f;
    [SerializeField, Min(0f)] private float horizontalWhileClimbingMultiplier = 0.45f;
    [SerializeField] private bool autoDetectLadderByAuthoring = true;

    [Header("Input System")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference crouchAction;

    [Header("边界检测")]
    [SerializeField] private bool useHorizontalBounds;
    [SerializeField] private float minX = -50f;
    [SerializeField] private float maxX = 50f;
    [SerializeField] private bool useKillY = true;
    [SerializeField] private float killY = -15f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;

    private int jumpsLeft;
    private bool isGrounded;
    private bool isCrouching;
    private int facing = 1;
    private bool outOfBoundsTriggered;

    private float moveInput;
    private float verticalInput;
    private bool jumpQueued;
    private Coroutine dropRoutine;
    private bool isDroppingThroughOneWay;
    private readonly List<Collider2D> oneWayContacts = new();

    private int ladderContacts;
    private bool isClimbing;
    private float defaultGravityScale;

    [HideInInspector] public float bonusSpeed;

    public int Facing => facing;
    public bool IsCrouching => isCrouching;
    public bool IsClimbing => isClimbing;

    private void Awake()
    {
        if (ShouldDisableAsDuplicate())
        {
            enabled = false;
            return;
        }

        EnsureRuntimeComponents();

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        jumpsLeft = maxJumps;
        defaultGravityScale = rb != null ? rb.gravityScale : 1f;
    }

    private void Start()
    {
        InventoryUIBootstrap.EnsureUI(GetComponent<PlayerWeaponInventory>());
    }

    private void OnEnable()
    {
        moveAction?.action?.Enable();
        jumpAction?.action?.Enable();
        crouchAction?.action?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action?.Disable();
        jumpAction?.action?.Disable();
        crouchAction?.action?.Disable();

        StopClimbing(resetVelocity: false);
    }

    private void Update()
    {
        GroundCheck();

        moveInput = ReadMoveInput();
        verticalInput = ReadVerticalInput();

        if (ReadJumpPressed())
        {
            jumpQueued = true;
        }

        UpdateClimbState();
        isCrouching = !isClimbing && ReadCrouchHeld();

        HandleFacing();
        HandleBounds();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (!enabled)
        {
            return;
        }

        if (isClimbing)
        {
            HandleClimbingMovement();
            return;
        }

        var speedMul = isCrouching ? crouchMoveMultiplier : 1f;
        rb.velocity = new Vector2(moveInput * (moveSpeed + bonusSpeed) * speedMul, rb.velocity.y);

        if (!jumpQueued)
        {
            return;
        }

        jumpQueued = false;

        // 蹲下+跳跃：仅在脚下是单向平台时穿下去。
        if (isCrouching && isGrounded && TryGetStandingOneWayPlatforms(oneWayContacts))
        {
            if (dropRoutine == null)
            {
                dropRoutine = StartCoroutine(DropDownFromOneWayPlatform(oneWayContacts.ToArray()));
            }

            return;
        }

        if (jumpsLeft <= 0)
        {
            return;
        }

        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        jumpsLeft--;
    }

    private void HandleClimbingMovement()
    {
        var climbHorizontalMul = Mathf.Clamp(horizontalWhileClimbingMultiplier, 0f, 1f);
        var horizontal = moveInput * (moveSpeed + bonusSpeed) * climbHorizontalMul;
        var vertical = verticalInput * climbSpeed;

        rb.gravityScale = 0f;
        rb.velocity = new Vector2(horizontal, vertical);
        jumpsLeft = maxJumps;

        if (!jumpQueued)
        {
            return;
        }

        jumpQueued = false;
        StopClimbing(resetVelocity: false);

        rb.velocity = new Vector2(horizontal, jumpForce);
        jumpsLeft = Mathf.Max(0, maxJumps - 1);
    }

    private void UpdateClimbState()
    {
        if (ladderContacts <= 0)
        {
            StopClimbing(resetVelocity: true);
            return;
        }

        if (!isClimbing && Mathf.Abs(verticalInput) > 0.12f)
        {
            StartClimbing();
        }
    }

    private void StartClimbing()
    {
        if (isClimbing)
        {
            return;
        }

        isClimbing = true;
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(rb.velocity.x * 0.2f, 0f);
        jumpsLeft = maxJumps;
    }

    private void StopClimbing(bool resetVelocity)
    {
        if (!isClimbing && rb != null)
        {
            rb.gravityScale = defaultGravityScale;
            return;
        }

        isClimbing = false;

        if (rb == null)
        {
            return;
        }

        rb.gravityScale = defaultGravityScale;

        if (resetVelocity)
        {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Min(0f, rb.velocity.y));
        }
    }

    private IEnumerator DropDownFromOneWayPlatform(Collider2D[] oneWayColliders)
    {
        isDroppingThroughOneWay = true;
        var playerLayer = gameObject.layer;

        if (bodyCollider != null && oneWayColliders != null)
        {
            for (var i = 0; i < oneWayColliders.Length; i++)
            {
                var col = oneWayColliders[i];
                if (col != null)
                {
                    Physics2D.IgnoreCollision(bodyCollider, col, true);
                }
            }
        }

        if (oneWayPlatformLayer.value != 0)
        {
            for (var layer = 0; layer < 32; layer++)
            {
                if ((oneWayPlatformLayer.value & (1 << layer)) != 0)
                {
                    Physics2D.IgnoreLayerCollision(playerLayer, layer, true);
                }
            }
        }

        rb.velocity = new Vector2(rb.velocity.x, -Mathf.Max(0.5f, dropDownInitialSpeed));

        yield return new WaitForSeconds(Mathf.Max(0.05f, dropDownDuration));

        if (bodyCollider != null && oneWayColliders != null)
        {
            for (var i = 0; i < oneWayColliders.Length; i++)
            {
                var col = oneWayColliders[i];
                if (col != null)
                {
                    Physics2D.IgnoreCollision(bodyCollider, col, false);
                }
            }
        }

        if (oneWayPlatformLayer.value != 0)
        {
            for (var layer = 0; layer < 32; layer++)
            {
                if ((oneWayPlatformLayer.value & (1 << layer)) != 0)
                {
                    Physics2D.IgnoreLayerCollision(playerLayer, layer, false);
                }
            }
        }

        isDroppingThroughOneWay = false;
        dropRoutine = null;
    }

    private void EnsureRuntimeComponents()
    {
        if (GetComponent<PlayerWeaponInventory>() == null)
        {
            gameObject.AddComponent<PlayerWeaponInventory>();
        }

        if (GetComponent<PlayerInteractor>() == null)
        {
            gameObject.AddComponent<PlayerInteractor>();
        }

        if (GetComponent<PlayerWallet>() == null)
        {
            gameObject.AddComponent<PlayerWallet>();
        }

        if (GetComponent<PlayerCombat>() == null)
        {
            gameObject.AddComponent<PlayerCombat>();
        }

        if (GetComponent<PlayerWeaponVisual>() == null)
        {
            gameObject.AddComponent<PlayerWeaponVisual>();
        }
    }

    private bool ShouldDisableAsDuplicate()
    {
        var controllers = GetComponents<PlayerController>();
        if (controllers.Length <= 1)
        {
            return false;
        }

        var best = this;
        var bestScore = ScoreController(this);

        foreach (var controller in controllers)
        {
            var score = ScoreController(controller);
            if (score > bestScore)
            {
                bestScore = score;
                best = controller;
            }
        }

        return best != this;
    }

    private static int ScoreController(PlayerController controller)
    {
        var score = 0;
        if (controller.groundCheck != null) score += 2;
        if (controller.moveAction != null) score += 1;
        if (controller.jumpAction != null) score += 1;
        if (controller.groundLayer.value != 0) score += 1;
        return score;
    }

    private void GroundCheck()
    {
        if (isClimbing)
        {
            isGrounded = true;
            jumpsLeft = maxJumps;
            return;
        }

        if (isDroppingThroughOneWay)
        {
            isGrounded = false;
            return;
        }

        var layerMask = ResolveGroundMask();
        if (layerMask == 0)
        {
            isGrounded = false;
            return;
        }

        var overlapGrounded = false;
        if (groundCheck != null)
        {
            overlapGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, layerMask);
        }

        var colliderGrounded = bodyCollider != null && bodyCollider.IsTouchingLayers(layerMask);
        isGrounded = overlapGrounded || colliderGrounded;

        if (isGrounded)
        {
            jumpsLeft = maxJumps;
        }
    }

    private int ResolveGroundMask()
    {
        var configuredMask = groundLayer.value | oneWayPlatformLayer.value;
        return configuredMask == 0 ? Physics2D.DefaultRaycastLayers : configuredMask;
    }

    private bool TryGetStandingOneWayPlatforms(List<Collider2D> results)
    {
        results.Clear();
        if (bodyCollider == null)
        {
            return false;
        }

        var checkPosition = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)bodyCollider.bounds.center;
        var checkRadius = Mathf.Max(0.05f, groundCheckRadius + 0.03f);
        var overlaps = Physics2D.OverlapCircleAll(checkPosition, checkRadius);

        for (var i = 0; i < overlaps.Length; i++)
        {
            var col = overlaps[i];
            if (col == null || col == bodyCollider || col.isTrigger)
            {
                continue;
            }

            if (!bodyCollider.IsTouching(col))
            {
                continue;
            }

            if (!IsOneWayPlatformCollider(col))
            {
                continue;
            }

            results.Add(col);
        }

        if (results.Count > 0)
        {
            return true;
        }

        return oneWayPlatformLayer.value != 0 && bodyCollider.IsTouchingLayers(oneWayPlatformLayer.value);
    }

    private bool IsOneWayPlatformCollider(Collider2D col)
    {
        if (col == null)
        {
            return false;
        }

        if (oneWayPlatformLayer.value != 0 && ((oneWayPlatformLayer.value & (1 << col.gameObject.layer)) != 0))
        {
            return true;
        }

        if (!autoDetectOneWayByEffector)
        {
            return false;
        }

        return HasOneWayEffector(col);
    }

    private static bool HasOneWayEffector(Collider2D col)
    {
        if (!col.usedByEffector)
        {
            return false;
        }

        var effector = col.GetComponent<PlatformEffector2D>();
        if (effector == null)
        {
            effector = col.GetComponentInParent<PlatformEffector2D>();
        }

        return effector != null && effector.useOneWay;
    }

    private bool IsLadderCollider(Collider2D col)
    {
        if (col == null)
        {
            return false;
        }

        if (ladderLayer.value != 0 && ((ladderLayer.value & (1 << col.gameObject.layer)) != 0))
        {
            return true;
        }

        if (!autoDetectLadderByAuthoring)
        {
            return false;
        }

        return col.GetComponent<LadderAuthoring>() != null || col.GetComponentInParent<LadderAuthoring>() != null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsLadderCollider(other))
        {
            return;
        }

        ladderContacts++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsLadderCollider(other))
        {
            return;
        }

        ladderContacts = Mathf.Max(0, ladderContacts - 1);
        if (ladderContacts == 0)
        {
            StopClimbing(resetVelocity: true);
        }
    }

    private void HandleFacing()
    {
        if (Mathf.Abs(moveInput) <= Mathf.Epsilon)
        {
            return;
        }

        facing = moveInput > 0 ? 1 : -1;
        spriteRenderer.flipX = facing < 0;
    }

    private void HandleBounds()
    {
        if (useHorizontalBounds && maxX > minX + 0.01f)
        {
            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            transform.position = pos;
        }

        if (!useKillY || outOfBoundsTriggered || transform.position.y > killY)
        {
            return;
        }

        outOfBoundsTriggered = true;
        var health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(999999);
        }
        else
        {
            GameManager.Instance?.Respawn();
        }
    }

    private float ReadMoveInput()
    {
        if (moveAction != null && moveAction.action != null)
        {
            var move = moveAction.action.ReadValue<Vector2>();
            return move.x;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return 0f;
        }

        var x = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            x -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            x += 1f;
        }

        return Mathf.Clamp(x, -1f, 1f);
    }

    private float ReadVerticalInput()
    {
        if (moveAction != null && moveAction.action != null)
        {
            var move = moveAction.action.ReadValue<Vector2>();
            return Mathf.Clamp(move.y, -1f, 1f);
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return 0f;
        }

        var y = 0f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            y += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            y -= 1f;
        }

        return Mathf.Clamp(y, -1f, 1f);
    }

    private bool ReadJumpPressed()
    {
        if (jumpAction != null && jumpAction.action != null)
        {
            return jumpAction.action.WasPressedThisFrame();
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.spaceKey.wasPressedThisFrame ||
               keyboard.wKey.wasPressedThisFrame ||
               keyboard.upArrowKey.wasPressedThisFrame;
    }

    private bool ReadCrouchHeld()
    {
        if (crouchAction != null && crouchAction.action != null && crouchAction.action.IsPressed())
        {
            return true;
        }

        if (moveAction != null && moveAction.action != null)
        {
            var move = moveAction.action.ReadValue<Vector2>();
            if (move.y < -0.5f)
            {
                return true;
            }
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.leftCtrlKey.isPressed || keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
    }

    private void UpdateAnimation()
    {
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VelocityY", rb.velocity.y);
        animator.SetBool("IsCrouching", isCrouching);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (useHorizontalBounds && maxX > minX + 0.01f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(minX, transform.position.y - 5f), new Vector3(minX, transform.position.y + 5f));
            Gizmos.DrawLine(new Vector3(maxX, transform.position.y - 5f), new Vector3(maxX, transform.position.y + 5f));
        }
    }
}
