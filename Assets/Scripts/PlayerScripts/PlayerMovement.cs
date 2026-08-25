using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    // ==================================================
    // MOVEMENT
    // ==================================================

    [Header("Movement")]

    [SerializeField]
    private float speed = 5f;


    // ==================================================
    // JUMP
    // ==================================================

    [Header("Jump")]

    [SerializeField]
    private float jumpForce = 15f;


    [SerializeField]
    [Range(0.1f, 1f)]
    private float jumpCutMultiplier = 0.5f;


    // ==================================================
    // GROUND CHECK
    // ==================================================

    [Header("Ground Check")]

    [SerializeField]
    private Transform groundCheck;


    [SerializeField]
    private float groundCheckRadius = 0.2f;


    [SerializeField]
    private LayerMask groundLayer;


    // ==================================================
    // CONTROLS
    // ==================================================

    [Header("Controls")]

    [Tooltip(
        "When false, player input is ignored. " +
        "Timeline Signals can call EnableControls() / DisableControls()."
    )]
    [SerializeField]
    private bool controlsEnabled = true;


    // ==================================================
    // COMPONENTS
    // ==================================================

    private Rigidbody2D rb;

    private Animator animator;


    // ==================================================
    // ANIMATOR HASHES
    // ==================================================

    private static readonly int IsRunningHash =
        Animator.StringToHash(
            "IsRunning"
        );


    private static readonly int IsJumpingHash =
        Animator.StringToHash(
            "IsJumping"
        );


    private static readonly int IsGroundedHash =
        Animator.StringToHash(
            "IsGrounded"
        );


    // ==================================================
    // STATE
    // ==================================================

    private float horizontal;

    private int facingDirection = 1;

    private bool isGrounded;

    private bool jumpRequested;

    private bool jumpQueued;

    private bool jumpCutApplied;


    // ==================================================
    // PUBLIC STATE
    // ==================================================

    public bool ControlsEnabled =>
        controlsEnabled;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();


        animator =
            GetComponent<Animator>();
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        CheckGround();


        if (controlsEnabled)
        {
            ReadInput();

            HandleJumpInput();

            HandleFlip();
        }
        else
        {
            /*
             * Make sure old input does not stay active
             * while a Timeline cinematic is playing.
             */
            horizontal =
                0f;
        }


        UpdateAnimations();
    }


    // ==================================================
    // FIXED UPDATE
    // ==================================================

    private void FixedUpdate()
    {
        if (controlsEnabled &&
            jumpQueued)
        {
            ExecuteJump();
        }


        jumpQueued =
            false;


        MovePlayer();
    }


    // ==================================================
    // INPUT
    // ==================================================

    private void ReadInput()
    {
        horizontal =
            Input.GetAxisRaw(
                "Horizontal"
            );
    }


    // ==================================================
    // GROUND CHECK
    // ==================================================

    private void CheckGround()
    {
        if (groundCheck == null)
        {
            Debug.LogError(
                "PlayerMovement: groundCheck is not assigned.",
                this
            );


            isGrounded =
                false;


            return;
        }


        bool touchingGround =
            Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );


        isGrounded =
            touchingGround &&
            rb.linearVelocity.y <= 0.1f;


        if (isGrounded)
        {
            jumpRequested =
                false;


            jumpCutApplied =
                false;
        }
    }


    // ==================================================
    // JUMP INPUT
    // ==================================================

    private void HandleJumpInput()
    {
        if (!controlsEnabled)
            return;


        if (Input.GetButtonDown(
                "Jump"
            ) &&
            isGrounded)
        {
            jumpRequested =
                true;


            isGrounded =
                false;


            jumpCutApplied =
                false;


            jumpQueued =
                true;
        }


        // ----------------------------------------------
        // VARIABLE JUMP HEIGHT
        // ----------------------------------------------

        if (Input.GetButtonUp(
                "Jump"
            ) &&
            !isGrounded &&
            rb.linearVelocity.y > 0f &&
            !jumpCutApplied)
        {
            rb.linearVelocity =
                new Vector2(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y *
                    jumpCutMultiplier
                );


            jumpCutApplied =
                true;
        }
    }


    // ==================================================
    // EXECUTE JUMP
    // ==================================================

    private void ExecuteJump()
    {
        if (!controlsEnabled)
            return;


        rb.linearVelocity =
            new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );


        isGrounded =
            false;
    }


    // ==================================================
    // MOVEMENT
    // ==================================================

    private void MovePlayer()
    {
        float targetHorizontal =
            controlsEnabled
                ? horizontal * speed
                : 0f;


        /*
         * Disable only player-controlled horizontal motion.
         *
         * Keep Y velocity so gravity still works normally.
         */
        rb.linearVelocity =
            new Vector2(
                targetHorizontal,
                rb.linearVelocity.y
            );
    }


    // ==================================================
    // FLIP
    // ==================================================

    private void HandleFlip()
    {
        if (!controlsEnabled)
            return;


        if (
            (
                horizontal > 0.1f &&
                facingDirection < 0
            ) ||
            (
                horizontal < -0.1f &&
                facingDirection > 0
            )
        )
        {
            Flip();
        }
    }


    private void Flip()
    {
        facingDirection *=
            -1;


        Vector3 scale =
            transform.localScale;


        scale.x =
            Mathf.Abs(
                scale.x
            ) *
            facingDirection;


        transform.localScale =
            scale;
    }


    // ==================================================
    // ANIMATIONS
    // ==================================================

    private void UpdateAnimations()
    {
        bool isRunning =
            controlsEnabled &&
            isGrounded &&
            Mathf.Abs(
                horizontal
            ) > 0.1f;


        bool isJumping =
            jumpRequested ||
            !isGrounded;


        animator.SetBool(
            IsRunningHash,
            isRunning
        );


        animator.SetBool(
            IsJumpingHash,
            isJumping
        );


        animator.SetBool(
            IsGroundedHash,
            isGrounded
        );
    }


    // ==================================================
    // TIMELINE CONTROL
    // ==================================================

    public void DisableControls()
    {
        if (!controlsEnabled)
            return;

        controlsEnabled = false;

        /*
         * Remove any input that was active on the frame
         * the Timeline signal fired.
         */
        horizontal = 0f;


        jumpQueued = false;


        jumpRequested = false;


        jumpCutApplied = false;


        /*
         * Stop horizontal movement immediately.
         *
         * Y velocity is preserved so gravity continues
         * working and the player can settle on the ground.
         */
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }


        /*
         * Stop running animation immediately.
         */
        if (animator != null)
        {
            animator.SetBool(IsRunningHash, false);
        }


        Debug.Log("Player controls disabled.", this);
    }


    public void EnableControls()
    {
        if (controlsEnabled)
            return;

        horizontal = 0f;


        jumpQueued = false;


        jumpRequested = false;


        jumpCutApplied = false;


        controlsEnabled = true;


        Debug.Log("Player controls enabled.", this);
    }


    // ==================================================
    // GIZMOS
    // ==================================================

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;


        Gizmos.color =
            Color.yellow;


        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}