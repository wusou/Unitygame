using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移动参数")]
    [SerializeField] private float moveSpeed = 6.5f;
    [SerializeField] private float jumpForce = 11f;
    [SerializeField] private int maxJumps = 2;

    [Header("地面检测")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Input System")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("边界检测")]
    [SerializeField] private bool useHorizontalBounds = false;
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
    private int facing = 1;
    private bool outOfBoundsTriggered;

    private float moveInput;
    private bool jumpQueued;

    [HideInInspector] public float bonusSpeed;

    public int Facing => facing;

    private void Awake()
    {
        if (ShouldDisableAsDuplicate())
        {
            enabled = false;
            return;
        }

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        jumpsLeft = maxJumps;
    }

    private void OnEnable()
    {
        moveAction?.action?.Enable();
        jumpAction?.action?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action?.Disable();
        jumpAction?.action?.Disable();
    }

    private void Update()
    {
        GroundCheck();

        moveInput = ReadMoveInput();
        if (ReadJumpPressed())
        {
            jumpQueued = true;
        }

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

        rb.velocity = new Vector2(moveInput * (moveSpeed + bonusSpeed), rb.velocity.y);

        if (jumpQueued && jumpsLeft > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpsLeft--;
        }

        jumpQueued = false;
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
        int layerMask = groundLayer.value == 0 ? Physics2D.DefaultRaycastLayers : groundLayer.value;

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
        if (useHorizontalBounds)
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

        return Input.GetAxisRaw("Horizontal");
    }

    private bool ReadJumpPressed()
    {
        if (jumpAction != null && jumpAction.action != null)
        {
            return jumpAction.action.WasPressedThisFrame();
        }

        return Input.GetButtonDown("Jump");
    }

    private void UpdateAnimation()
    {
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VelocityY", rb.velocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (useHorizontalBounds)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(minX, transform.position.y - 5f), new Vector3(minX, transform.position.y + 5f));
            Gizmos.DrawLine(new Vector3(maxX, transform.position.y - 5f), new Vector3(maxX, transform.position.y + 5f));
        }
    }
}

