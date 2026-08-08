using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField][Range(0.1f, 1f)] private float jumpCutMultiplier = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator animator;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    private float horizontal;
    private int facingDirection = 1;
    private bool isGrounded;
    private bool jumpRequested;
    private bool jumpQueued;
    private bool jumpCutApplied;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        ReadInput();
        CheckGround();
        HandleJumpInput();
        HandleFlip();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (jumpQueued)
        {
            ExecuteJump();
            jumpQueued = false;
        }

        MovePlayer();
    }

    private void ReadInput()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
    }

    private void CheckGround()
    {
        if (groundCheck == null)
        {
            Debug.LogError("PlayerMovement: groundCheck is not assigned.", this);
            isGrounded = false;
            return;
        }

        bool touchingGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isGrounded = touchingGround && rb.linearVelocity.y <= 0.1f;

        if (isGrounded)
        {
            jumpRequested = false;
            jumpCutApplied = false;
        }
    }

    private void HandleJumpInput()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpRequested = true;
            isGrounded = false;
            jumpCutApplied = false;
            jumpQueued = true;
        }

        // Variable jump height: cut upward velocity on early release
        if (Input.GetButtonUp("Jump") && !isGrounded && rb.linearVelocity.y > 0f && !jumpCutApplied)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            jumpCutApplied = true;
        }
    }

    private void ExecuteJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        isGrounded = false;
    }

    private void MovePlayer()
    {
        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
    }

    private void HandleFlip()
    {
        if ((horizontal > 0.1f && facingDirection < 0) || (horizontal < -0.1f && facingDirection > 0))
        {
            facingDirection *= -1;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facingDirection;
            transform.localScale = scale;
        }
    }

    private void UpdateAnimations()
    {
        bool isRunning = isGrounded && Mathf.Abs(horizontal) > 0.1f;
        bool isJumping = jumpRequested || !isGrounded;

        animator.SetBool(IsRunningHash, isRunning);
        animator.SetBool(IsJumpingHash, isJumping);
        animator.SetBool(IsGroundedHash, isGrounded);
    }

    private void Flip()
    {
        facingDirection *= -1;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDirection;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
