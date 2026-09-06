using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class CompanionFollower2D : MonoBehaviour
{
    private enum CompanionMode
    {
        Disabled,
        WaitingForActivation,
        Following,
        Waypoint
    }

    [Header("Target")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Rigidbody2D playerRigidbody;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Activation")]
    [Tooltip("For controlled sections keep this OFF and use CompanionFollowZone or Timeline.")]
    [SerializeField] private bool startFollowingEnabled = false;
    [SerializeField, Min(0f)] private float activationDistance = 4f;

    [Header("Following")]
    [SerializeField, Min(0f)] private float moveSpeed = 3.5f;
    [SerializeField, Min(0f)] private float followDistance = 1.6f;
    [SerializeField, Min(0f)] private float stopDistance = 1.2f;
    [SerializeField, Min(0f)] private float maxDistance = 7f;
    [SerializeField, Min(0f)] private float catchUpSpeed = 4.5f;
    [SerializeField] private bool followOnlyWhenPlayerMoves = true;
    [SerializeField] private bool catchUpWhenPlayerStops = false;
    [SerializeField, Min(0f)] private float playerMoveThreshold = 0.05f;

    [Header("Jump")]
    [Tooltip("Press = jump, hold = full jump, early release = shorter jump.")]
    [SerializeField] private bool mirrorPlayerJump = true;
    [SerializeField, Min(0f)] private float jumpForce = 9f;
    [SerializeField, Range(0.1f, 1f)] private float jumpCutMultiplier = 0.48f;
    [SerializeField] private bool jumpOnlyWhileFollowing = true;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string movingParameter = "IsMoving";
    [SerializeField] private string groundedParameter = "IsGrounded";
    [SerializeField] private string jumpingParameter = "IsJumping";

    [Header("Facing")]
    [Tooltip("Only the visual sprite is flipped. Physics objects and child transforms are not flipped.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool spriteFacesRightByDefault = true;
    [SerializeField] private bool facePlayerWhileFollowing = true;
    [SerializeField] private bool faceMovementDirection = true;
    [SerializeField, Min(0f)] private float facingDeadZone = 0.05f;

    [Header("Waypoint / Timeline")]
    [SerializeField] private Transform assignedWaypoint;
    [SerializeField, Min(0f)] private float waypointMoveSpeed = 3.5f;
    [SerializeField, Min(0.01f)] private float waypointStopDistance = 0.1f;
    [SerializeField] private bool resumeFollowingAfterWaypoint = false;

    private Rigidbody2D rb;
    private CompanionMode mode = CompanionMode.Disabled;

    private bool followPermission;
    private bool activated;
    private bool moving;
    private bool grounded;

    private bool jumpQueued;
    private bool jumpCutQueued;
    private bool jumpCutApplied;

    private int movingHash;
    private int groundedHash;
    private int jumpingHash;

    public bool IsFollowing => mode == CompanionMode.Following;
    public bool IsGrounded => grounded;
    public bool IsActivated => activated;
    public Transform PlayerTarget => playerTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        CacheAnimatorHashes();
        ResolvePlayer();

        followPermission = startFollowingEnabled;
        mode = followPermission
            ? CompanionMode.WaitingForActivation
            : CompanionMode.Disabled;
    }

    private void Update()
    {
        if (playerTarget == null && autoFindPlayer)
            ResolvePlayer();

        UpdateGroundedState();
        HandleMirroredJumpInput();
        UpdateFacing();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        ProcessQueuedJump();
        ProcessQueuedJumpCut();

        switch (mode)
        {
            case CompanionMode.Disabled:
                StopHorizontalMovement();
                break;

            case CompanionMode.WaitingForActivation:
                HandleWaitingForActivation();
                break;

            case CompanionMode.Following:
                HandleFollowing();
                break;

            case CompanionMode.Waypoint:
                HandleWaypointMovement();
                break;
        }
    }

    private void ResolvePlayer()
    {
        if (playerTarget == null)
        {
            try
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

                if (playerObject != null)
                    playerTarget = playerObject.transform;
            }
            catch (UnityException)
            {
                return;
            }
        }

        if (playerTarget != null && playerRigidbody == null)
            playerRigidbody = playerTarget.GetComponent<Rigidbody2D>();
    }

    private void HandleWaitingForActivation()
    {
        StopHorizontalMovement();

        if (!followPermission || playerTarget == null)
            return;

        float horizontalDistance =
            Mathf.Abs(playerTarget.position.x - transform.position.x);

        if (horizontalDistance <= activationDistance)
        {
            activated = true;
            mode = CompanionMode.Following;
        }
    }

    private void HandleFollowing()
    {
        if (!followPermission || playerTarget == null)
        {
            StopHorizontalMovement();
            return;
        }

        float deltaX =
            playerTarget.position.x - transform.position.x;

        float distance =
            Mathf.Abs(deltaX);

        bool playerMoving =
            IsPlayerMovingHorizontally();

        if (followOnlyWhenPlayerMoves &&
            !playerMoving &&
            !(catchUpWhenPlayerStops && distance > maxDistance))
        {
            StopHorizontalMovement();
            return;
        }

        if (!moving)
        {
            if (distance <= followDistance)
            {
                StopHorizontalMovement();
                return;
            }
        }
        else if (distance <= stopDistance)
        {
            StopHorizontalMovement();
            return;
        }

        float speed =
            distance > maxDistance
                ? catchUpSpeed
                : moveSpeed;

        MoveHorizontally(
            Mathf.Sign(deltaX),
            speed
        );
    }

    private bool IsPlayerMovingHorizontally()
    {
        if (playerRigidbody == null)
            return false;

        return Mathf.Abs(playerRigidbody.linearVelocity.x) > playerMoveThreshold;
    }

    private void MoveHorizontally(
        float direction,
        float speed)
    {
        if (Mathf.Abs(direction) < 0.01f)
        {
            StopHorizontalMovement();
            return;
        }

        rb.linearVelocity =
            new Vector2(
                direction * speed,
                rb.linearVelocity.y
            );

        moving = true;

        if (faceMovementDirection &&
            mode == CompanionMode.Waypoint)
        {
            FaceDirection(direction);
        }
    }

    private void StopHorizontalMovement()
    {
        if (rb == null)
            return;

        rb.linearVelocity =
            new Vector2(
                0f,
                rb.linearVelocity.y
            );

        moving = false;
    }

    private void HandleMirroredJumpInput()
    {
        if (!mirrorPlayerJump)
            return;

        if (jumpOnlyWhileFollowing &&
            mode != CompanionMode.Following)
        {
            ClearPendingJumpInput();
            return;
        }

        if (!followPermission)
        {
            ClearPendingJumpInput();
            return;
        }

        if (Input.GetButtonDown("Jump") &&
            grounded)
        {
            jumpQueued = true;
            jumpCutApplied = false;
        }

        if (Input.GetButtonUp("Jump") &&
            !grounded &&
            rb.linearVelocity.y > 0f &&
            !jumpCutApplied)
        {
            jumpCutQueued = true;
        }

        if (grounded)
            jumpCutApplied = false;
    }

    private void ProcessQueuedJump()
    {
        if (!jumpQueued)
            return;

        jumpQueued = false;

        if (!grounded)
            return;

        ExecuteJump();
    }

    private void ProcessQueuedJumpCut()
    {
        if (!jumpCutQueued)
            return;

        jumpCutQueued = false;

        if (grounded ||
            rb.linearVelocity.y <= 0f ||
            jumpCutApplied)
        {
            return;
        }

        rb.linearVelocity =
            new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier
            );

        jumpCutApplied = true;
    }

    private void ExecuteJump()
    {
        rb.linearVelocity =
            new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

        grounded = false;
    }

    public void JumpNow()
    {
        if (rb == null || !grounded)
            return;

        jumpCutApplied = false;
        ExecuteJump();
    }

    private void ClearPendingJumpInput()
    {
        jumpQueued = false;
        jumpCutQueued = false;
    }

    private void UpdateGroundedState()
    {
        if (groundCheck == null)
        {
            grounded = false;
            return;
        }

        bool touchingGround =
            Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            ) != null;

        grounded =
            touchingGround &&
            rb.linearVelocity.y <= 0.1f;

        if (grounded)
            jumpCutApplied = false;
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null)
            return;

        if (mode == CompanionMode.Following &&
            facePlayerWhileFollowing &&
            playerTarget != null)
        {
            float deltaX =
                playerTarget.position.x - transform.position.x;

            if (Mathf.Abs(deltaX) > facingDeadZone)
            {
                FaceDirection(
                    Mathf.Sign(deltaX)
                );
            }

            return;
        }

        if (mode == CompanionMode.Waypoint &&
            faceMovementDirection &&
            assignedWaypoint != null)
        {
            float deltaX =
                assignedWaypoint.position.x - transform.position.x;

            if (Mathf.Abs(deltaX) > facingDeadZone)
            {
                FaceDirection(
                    Mathf.Sign(deltaX)
                );
            }
        }
    }

    private void FaceDirection(
        float direction)
    {
        if (spriteRenderer == null ||
            Mathf.Abs(direction) < 0.01f)
        {
            return;
        }

        bool wantsRight =
            direction > 0f;

        spriteRenderer.flipX =
            spriteFacesRightByDefault
                ? !wantsRight
                : wantsRight;
    }

    private void CacheAnimatorHashes()
    {
        movingHash = Animator.StringToHash(movingParameter);
        groundedHash = Animator.StringToHash(groundedParameter);
        jumpingHash = Animator.StringToHash(jumpingParameter);
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool(
            movingHash,
            moving && grounded
        );

        animator.SetBool(
            groundedHash,
            grounded
        );

        animator.SetBool(
            jumpingHash,
            !grounded
        );
    }

    public void EnableFollowing()
    {
        followPermission = true;
        activated = false;
        mode = CompanionMode.WaitingForActivation;

        ClearPendingJumpInput();
        StopHorizontalMovement();
    }

    public void DisableFollowing()
    {
        followPermission = false;
        activated = false;
        mode = CompanionMode.Disabled;

        ClearPendingJumpInput();
        StopHorizontalMovement();
    }

    public void ResumeFollowing()
    {
        followPermission = true;
        activated = true;
        mode = CompanionMode.Following;

        ClearPendingJumpInput();
        StopHorizontalMovement();
    }

    public void StopImmediately()
    {
        ClearPendingJumpInput();
        StopHorizontalMovement();
    }

    public void MoveToAssignedWaypoint()
    {
        if (assignedWaypoint == null)
        {
            Debug.LogWarning(
                "CompanionFollower2D: Assigned Waypoint is missing.",
                this
            );
            return;
        }

        activated = true;
        mode = CompanionMode.Waypoint;

        ClearPendingJumpInput();
        StopHorizontalMovement();
    }

    public void MoveToWaypoint(
        Transform waypoint)
    {
        if (waypoint == null)
            return;

        assignedWaypoint = waypoint;
        activated = true;
        mode = CompanionMode.Waypoint;

        ClearPendingJumpInput();
        StopHorizontalMovement();
    }

    private void HandleWaypointMovement()
    {
        if (assignedWaypoint == null)
        {
            StopHorizontalMovement();
            mode = CompanionMode.Disabled;
            return;
        }

        float deltaX =
            assignedWaypoint.position.x - transform.position.x;

        float distance =
            Mathf.Abs(deltaX);

        if (distance <= waypointStopDistance)
        {
            StopHorizontalMovement();

            if (resumeFollowingAfterWaypoint)
                ResumeFollowing();
            else
                mode = CompanionMode.Disabled;

            return;
        }

        MoveHorizontally(
            Mathf.Sign(deltaX),
            waypointMoveSpeed
        );
    }

    public void TimelineEnableFollowing() => EnableFollowing();
    public void TimelineDisableFollowing() => DisableFollowing();
    public void TimelineResumeFollowing() => ResumeFollowing();
    public void TimelineMoveToAssignedWaypoint() => MoveToAssignedWaypoint();
    public void TimelineJump() => JumpNow();
    public void TimelineStop() => StopImmediately();

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }
    }

    private void OnValidate()
    {
        activationDistance = Mathf.Max(0f, activationDistance);

        moveSpeed = Mathf.Max(0f, moveSpeed);
        followDistance = Mathf.Max(0f, followDistance);
        stopDistance = Mathf.Clamp(stopDistance, 0f, followDistance);
        maxDistance = Mathf.Max(followDistance, maxDistance);
        catchUpSpeed = Mathf.Max(moveSpeed, catchUpSpeed);

        jumpForce = Mathf.Max(0f, jumpForce);
        jumpCutMultiplier = Mathf.Clamp(jumpCutMultiplier, 0.1f, 1f);

        groundCheckRadius = Mathf.Max(0.01f, groundCheckRadius);
        facingDeadZone = Mathf.Max(0f, facingDeadZone);

        waypointMoveSpeed = Mathf.Max(0f, waypointMoveSpeed);
        waypointStopDistance = Mathf.Max(0.01f, waypointStopDistance);

        if (string.IsNullOrWhiteSpace(playerTag))
            playerTag = "Player";

        if (string.IsNullOrWhiteSpace(movingParameter))
            movingParameter = "IsMoving";

        if (string.IsNullOrWhiteSpace(groundedParameter))
            groundedParameter = "IsGrounded";

        if (string.IsNullOrWhiteSpace(jumpingParameter))
            jumpingParameter = "IsJumping";

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }
}