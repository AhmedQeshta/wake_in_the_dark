using System.Collections.Generic;
using UnityEngine;

public class LevelExitDoor : MonoBehaviour
{
    // EXIT TYPE
    public enum ExitType { NextLevel, GameEnding }


    [Header("Exit")]
    [SerializeField] private ExitType exitType = ExitType.NextLevel;


    // NEXT LEVEL
    [Header("Next Level")]
    [Tooltip("Used only when Exit Type = Next Level.")]
    [SerializeField] private string nextSceneName = "Level_02";


    // DOOR

    [Header("Door State")]
    [Tooltip("Assign the HideableTilemap that represents the actual exit door.")]
    [SerializeField] private HideableTilemap doorTilemap;


    [Tooltip("If enabled, the door must be hidden/open before the player can leave.")]
    [SerializeField] private bool requireDoorOpen = true;

    // COMPANION REQUIREMENT
    [Header("Companion Requirement")]
    [Tooltip("If enabled, Player 1 AND Wife / Player 2 must both be inside this exit trigger before the level transition can start.")]
    [SerializeField] private bool requireWife = false;


    [Tooltip("Tag used by Wife / Player 2. Your current Wife object uses the tag 'wife'.")]
    [SerializeField] private string wifeTag = "wife";


    // REFERENCES
    [Header("References")]
    [SerializeField] private LevelLoader levelLoader;


    // PLAYER
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";


    // STATE
    private bool transitionStarted;


    /*
     * HashSets are used because Player 1 and Wife may each have more than one Collider2D.
     * This prevents the door from thinking a character left when only one of several colliders exited the trigger.
     */
    private readonly HashSet<Collider2D> playerCollidersInside = new HashSet<Collider2D>();

    private readonly HashSet<Collider2D> wifeCollidersInside = new HashSet<Collider2D>();


    // PUBLIC STATE
    public bool IsPlayerInside => HasValidCollider(playerCollidersInside);
    public bool IsWifeInside => HasValidCollider(wifeCollidersInside);
    public bool RequiresWife => requireWife;


    // AWAKE

    private void Awake()
    {
        ResolveLoader();
    }


    // TRIGGER ENTER
    private void OnTriggerEnter2D(Collider2D other)
    {
        RegisterCollider(other);


        TryEnterDoor();
    }


    // TRIGGER STAY
    private void OnTriggerStay2D(Collider2D other)
    {
        /*
         * Register again as a safety measure. This is useful if a collider/component became enabled
         * while already overlapping the exit trigger.
         */
        RegisterCollider(other);

        TryEnterDoor();
    }


    // TRIGGER EXIT
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null)
            return;

        playerCollidersInside.Remove(other);
        wifeCollidersInside.Remove(other);
    }


    // REGISTER CHARACTER COLLIDER
    private void RegisterCollider(Collider2D other)
    {
        if (other == null)
            return;


        if (IsPlayer(other))
        {
            playerCollidersInside.Add(other);
            return;
        }


        if (IsWife(other))
            wifeCollidersInside.Add(other);
    }


    // TRY ENTER
    private void TryEnterDoor()
    {
        if (transitionStarted)
            return;

        CleanupColliderSet(playerCollidersInside);
        CleanupColliderSet(wifeCollidersInside);

        // PLAYER 1 IS ALWAYS REQUIRED OR OPTIONAL WIFE REQUIREMENT or DOOR MUST BE OPEN IF REQUIRED
        if (!IsPlayerInside || (requireWife && !IsWifeInside) || !IsDoorOpen())
            return;


        // EXIT
        switch (exitType)
        {
            case ExitType.GameEnding:
                HandleGameEnding();
                break;

            default:
                HandleNextLevel();
                break;
        }
    }


    // NEXT LEVEL
    private void HandleNextLevel()
    {
        ResolveLoader();


        if (levelLoader == null || string.IsNullOrWhiteSpace(nextSceneName))
            return;



        // UNLOCK NEXT LEVEL
        LevelProgressManager progress = LevelProgressManager.Instance;


        if (progress != null)
        {
            string currentSceneName = gameObject.scene.name;
            bool progressionAllowed = progress.CompleteLevelAndUnlockNext(currentSceneName, nextSceneName);


            if (!progressionAllowed)
                return;
        }

        transitionStarted = true;


        // VIDEO BEFORE NEXT LEVEL
        LevelVideoIntroManager videoIntro = LevelVideoIntroManager.Instance;

        if (videoIntro != null)
        {
            videoIntro.RequestLevel(nextSceneName);
            return;
        }


        // SAFE FALLBACK
        levelLoader.LoadLevel(nextSceneName);
    }


    // GAME ENDING
    private void HandleGameEnding()
    {
        transitionStarted = true;
        LevelVideoIntroManager videoIntro = LevelVideoIntroManager.Instance;

        if (videoIntro != null)
        {
            videoIntro.PlayGameEnding();
            return;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ReturnToMainMenuAfterGameEnding();
            return;
        }


        transitionStarted = false;
    }


    // PLAYER
    private bool IsPlayer(Collider2D other)
    {
        if (other == null)
            return false;


        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();


        if (movement != null)
            return string.IsNullOrWhiteSpace(playerTag) || movement.CompareTag(playerTag);



        if (other.attachedRigidbody != null)
        {
            GameObject bodyObject = other.attachedRigidbody.gameObject;


            if (bodyObject != null && bodyObject.CompareTag(playerTag))
                return true;
        }


        return other.CompareTag(playerTag);
    }


    // WIFE / PLAYER 2
    private bool IsWife(Collider2D other)
    {
        if (other == null)
            return false;


        /*
         * First check the Rigidbody2D GameObject.
         * This is useful when the collider is on a child.
         */
        if (other.attachedRigidbody != null)
        {
            GameObject bodyObject = other.attachedRigidbody.gameObject;

            if (bodyObject != null && bodyObject.CompareTag(wifeTag))
                return true;
        }


        /*
         * Check the collider itself.
         */
        if (other.CompareTag(wifeTag))
            return true;


        /*
         * Final safety check:
         * walk up the hierarchy and check the parent/root
         * that owns CompanionFollower2D.
         */
        CompanionFollower2D companion = other.GetComponentInParent<CompanionFollower2D>();


        if (companion != null)
            return companion.CompareTag(wifeTag);


        return false;
    }


    // COLLIDER SET HELPERS
    private bool HasValidCollider(HashSet<Collider2D> colliders)
    {
        CleanupColliderSet(colliders);

        return colliders.Count > 0;
    }


    private void CleanupColliderSet(HashSet<Collider2D> colliders)
    {
        if (colliders == null || colliders.Count == 0)
            return;

        colliders.RemoveWhere(collider => collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy);
    }


    // LOADER
    private void ResolveLoader()
    {
        if (levelLoader != null)
            return;


        levelLoader = LevelLoader.Instance;


        if (levelLoader == null)
            levelLoader = FindAnyObjectByType<LevelLoader>();
    }


    // DOOR OPEN

    private bool IsDoorOpen()
    {
        if (!requireDoorOpen)
            return true;

        if (doorTilemap != null)
            return doorTilemap.IsHidden && !doorTilemap.IsTransitioning;

        return false;
    }


    // RESET

    public void ResetTransitionState()
    {
        transitionStarted = false;
        playerCollidersInside.Clear();
        wifeCollidersInside.Clear();
    }


    // VALIDATE

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
            playerTag = "Player";


        if (string.IsNullOrWhiteSpace(wifeTag))
            wifeTag = "wife";
    }
}
