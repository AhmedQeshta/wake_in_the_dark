using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    // ==================================================
    // MOVEMENT
    // ==================================================

    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Climbing")]
    [SerializeField] private float climbSpeed = 4f;

    // ==================================================
    // JUMP
    // ==================================================

    [Header("Jump")]
    [SerializeField] private float jumpForce = 11f;
    [SerializeField, Range(0.1f, 1f)] private float jumpCutMultiplier = 0.35f;

    // ==================================================
    // GROUND CHECK
    // ==================================================

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // ==================================================
    // CONTROLS
    // ==================================================

    [Header("Controls")]
    [Tooltip("When false, normal gameplay input is ignored.")]
    [SerializeField] private bool controlsEnabled = true;

    // ==================================================
    // CUTSCENE CONTROL
    // ==================================================

    [Header("Cutscene Control")]

    [Tooltip("Makes the Rigidbody2D Kinematic while Timeline owns the Player. This prevents gravity and physics from fighting Timeline animation.")]
    [SerializeField] private bool useKinematicBodyDuringCutscene = true;

    [Tooltip("Wait for the current rendered frame to finish before physics is restored. Recommended when EndCutsceneControl is called from a Timeline Signal.")]
    [SerializeField] private bool waitForEndOfFrameBeforeRestoringPhysics = true;


    // ==================================================
    // COMPONENTS
    // ==================================================
    private Rigidbody2D rb;
    private Animator animator;


    // ==================================================
    // NORMAL GAMEPLAY PHYSICS
    // ==================================================
    private float defaultGravity;
    private RigidbodyType2D defaultBodyType;
    private RigidbodyInterpolation2D defaultInterpolation;
    private bool defaultSimulated;

    // ==================================================
    // INPUT / MOVEMENT STATE
    // ==================================================

    private float horizontal;
    private float vertical;

    private int facingDirection = 1;

    private bool isGrounded;
    private bool jumpRequested;
    private bool jumpQueued;
    private bool jumpCutApplied;

    // ==================================================
    // CLIMBING STATE
    // ==================================================

    private bool isTouchingLadder;
    private bool isClimbing;
    private Transform currentLadder;

    // ==================================================
    // CUTSCENE STATE
    // ==================================================

    private bool cutsceneControlActive;
    private bool controlsWereEnabledBeforeCutscene;
    private Coroutine endCutsceneRoutine;

    // ==================================================
    // ANIMATOR HASHES
    // ==================================================

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");

    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");

    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    private static readonly int IsClimbingHash = Animator.StringToHash("IsClimbing");

    private static readonly int ClimbSpeedYHash = Animator.StringToHash("ClimbSpeedY");

    // ==================================================
    // PUBLIC STATE
    // ==================================================

    public bool ControlsEnabled => controlsEnabled;
    public bool CutsceneControlActive => cutsceneControlActive;

    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        defaultGravity = rb.gravityScale;
        defaultBodyType = rb.bodyType;
        defaultInterpolation = rb.interpolation;
        defaultSimulated = rb.simulated;
    }

    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        if (cutsceneControlActive)
            return;

        CheckGround();

        if (controlsEnabled)
        {
            ReadInput();
            HandleClimbState();
            HandleJumpInput();
            HandleFlip();
        }
        else
        {
            horizontal = 0f;
            vertical = 0f;
        }

        UpdateAnimations();
    }

    // ==================================================
    // FIXED UPDATE
    // ==================================================

    private void FixedUpdate()
    {
        if (cutsceneControlActive)
            return;

        if (controlsEnabled && jumpQueued)
            ExecuteJump();

        jumpQueued = false;

        MovePlayer();
    }

    // ==================================================
    // INPUT
    // ==================================================

    private void ReadInput()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
    }

    // ==================================================
    // CLIMBING
    // ==================================================

    private void HandleClimbState()
    {
        if (isTouchingLadder && Mathf.Abs(vertical) > 0.1f && !isClimbing)
        {
            isClimbing = true;

            if (currentLadder != null)
            {
                transform.position = new Vector3(currentLadder.position.x, transform.position.y, transform.position.z);

                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                horizontal = 0f;
            }
        }
    }

    // ==================================================
    // JUMP INPUT
    // ==================================================

    private void HandleJumpInput()
    {
        if (!controlsEnabled)
            return;

        if (Input.GetButtonDown("Jump") && (isGrounded || isClimbing))
        {
            jumpRequested = true;
            isGrounded = false;
            jumpCutApplied = false;
            jumpQueued = true;
            isClimbing = false;
        }

        if (Input.GetButtonUp("Jump") && !isGrounded && rb.linearVelocity.y > 0f && !jumpCutApplied)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            jumpCutApplied = true;
        }
    }

    // ==================================================
    // EXECUTE JUMP
    // ==================================================

    private void ExecuteJump()
    {
        if (!controlsEnabled || cutsceneControlActive)
            return;

        // Safety: make sure Timeline did not leave physics
        // in Kinematic mode or with Gravity Scale = 0.
        RestoreGameplayPhysics();

        isClimbing = false;
        rb.gravityScale = defaultGravity;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        isGrounded = false;
    }

    // ==================================================
    // MOVEMENT
    // ==================================================

    private void MovePlayer()
    {
        if (cutsceneControlActive)
            return;

        float targetHorizontal = controlsEnabled ? horizontal * speed : 0f;

        if (isClimbing)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(targetHorizontal, vertical * climbSpeed);
        }
        else
        {
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(targetHorizontal, rb.linearVelocity.y);
        }
    }

    // ==================================================
    // LADDER TRIGGERS
    // ==================================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isTouchingLadder = true;
            currentLadder = collision.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isTouchingLadder = false;
            isClimbing = false;
            currentLadder = null;
        }
    }

    // ==================================================
    // GROUND CHECK
    // ==================================================

    private void CheckGround()
    {
        if (groundCheck == null)
            return;

        bool touchingGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        isGrounded = touchingGround && rb.linearVelocity.y <= 0.1f;

        if (isGrounded)
        {
            jumpRequested = false;
            jumpCutApplied = false;

            if (!isTouchingLadder || vertical <= 0f)
                isClimbing = false;
        }
    }

    // ==================================================
    // FLIP
    // ==================================================

    private void HandleFlip()
    {
        if (!controlsEnabled || cutsceneControlActive)
            return;

        if ((horizontal > 0.1f && facingDirection < 0) || (horizontal < -0.1f && facingDirection > 0))
            Flip();
    }

    private void Flip()
    {
        facingDirection *= -1;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDirection;
        transform.localScale = scale;
    }

    // ==================================================
    // ANIMATION
    // ==================================================

    private void UpdateAnimations()
    {
        if (cutsceneControlActive)
            return;

        bool isRunning = controlsEnabled && isGrounded && Mathf.Abs(horizontal) > 0.1f;
        bool isJumping = jumpRequested || (!isGrounded && !isClimbing);

        animator.SetBool(IsRunningHash, isRunning);
        animator.SetBool(IsJumpingHash, isJumping);
        animator.SetBool(IsGroundedHash, isGrounded);
        animator.SetBool(IsClimbingHash, isClimbing);
        animator.SetFloat(ClimbSpeedYHash, isClimbing ? Mathf.Abs(vertical) : 0f);
    }

    // ==================================================
    // NORMAL CONTROL ENABLE / DISABLE
    // ==================================================

    public void DisableControls()
    {
        if (!controlsEnabled)
            return;

        controlsEnabled = false;
        ClearInputState();
        isClimbing = false;

        if (rb != null && !cutsceneControlActive)
        {
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (animator != null)
        {
            animator.SetBool(IsRunningHash, false);

            animator.SetBool(IsClimbingHash, false);

            animator.SetFloat(ClimbSpeedYHash, 0f);
        }
    }

    public void EnableControls()
    {
        if (cutsceneControlActive)
            return;

        if (controlsEnabled)
            return;

        ClearInputState();
        RestoreGameplayPhysics();
        controlsEnabled = true;
    }

    // ==================================================
    // BEGIN CUTSCENE CONTROL
    // ==================================================
    public void BeginCutsceneControl()
    {
        if (cutsceneControlActive)
            return;

        if (endCutsceneRoutine != null)
        {
            StopCoroutine(endCutsceneRoutine);
            endCutsceneRoutine = null;
        }

        controlsWereEnabledBeforeCutscene = controlsEnabled;

        cutsceneControlActive = true;
        controlsEnabled = false;

        ClearInputState();

        isClimbing = false;

        if (animator != null)
        {
            animator.SetBool(IsRunningHash, false);
            animator.SetBool(IsClimbingHash, false);
            animator.SetFloat(ClimbSpeedYHash, 0f);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.None;

            rb.simulated = true;

            if (useKinematicBodyDuringCutscene)
                rb.bodyType = RigidbodyType2D.Kinematic;

            rb.WakeUp();
            Physics2D.SyncTransforms();
        }

    }

    // ==================================================
    // END CUTSCENE CONTROL
    // ==================================================

    public void EndCutsceneControl()
    {
        if (!cutsceneControlActive)
            return;

        if (endCutsceneRoutine != null)
            StopCoroutine(endCutsceneRoutine);

        endCutsceneRoutine = StartCoroutine(EndCutsceneControlRoutine());
    }

    private IEnumerator EndCutsceneControlRoutine()
    {
        if (waitForEndOfFrameBeforeRestoringPhysics)
            yield return new WaitForEndOfFrame();

        // Capture Timeline's final Player position.
        Vector3 finalPosition = transform.position;
        float finalRotation = transform.eulerAngles.z;

        if (rb != null)
        {
            rb.position = new Vector2(finalPosition.x, finalPosition.y);
            rb.rotation = finalRotation;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            Physics2D.SyncTransforms();
        }

        // Timeline stops owning gameplay movement.
        cutsceneControlActive = false;

        // Restore gameplay physics immediately.
        RestoreGameplayPhysics();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Let physics process the restored Dynamic body once.
        yield return new WaitForFixedUpdate();

        // Restore again after the first physics step.
        RestoreGameplayPhysics();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        ClearInputState();
        isClimbing = false;
        CheckGround();
        controlsEnabled = controlsWereEnabledBeforeCutscene;
        UpdateAnimations();
        endCutsceneRoutine = null;

    }

    // ==================================================
    // RESTORE GAMEPLAY PHYSICS
    // ==================================================

    private void RestoreGameplayPhysics()
    {
        if (rb == null)
            return;

        rb.simulated = true;
        rb.bodyType = defaultBodyType == RigidbodyType2D.Dynamic ? defaultBodyType : RigidbodyType2D.Dynamic;
        rb.gravityScale = defaultGravity;
        rb.interpolation = defaultInterpolation;
        rb.angularVelocity = 0f;
        rb.WakeUp();
        Physics2D.SyncTransforms();
    }

    // ==================================================
    // CLEAR INPUT STATE
    // ==================================================

    private void ClearInputState()
    {
        horizontal = 0f;
        vertical = 0f;
        jumpQueued = false;
        jumpRequested = false;
        jumpCutApplied = false;
    }

    // ==================================================
    // FORCE END CUTSCENE
    // ==================================================

    public void ForceEndCutsceneControl()
    {
        if (endCutsceneRoutine != null)
        {
            StopCoroutine(endCutsceneRoutine);
            endCutsceneRoutine = null;
        }

        cutsceneControlActive = false;

        RestoreGameplayPhysics();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        ClearInputState();
        isClimbing = false;
        controlsEnabled = true;
        CheckGround();
        UpdateAnimations();
    }

    // ==================================================
    // GIZMOS
    // ==================================================
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}