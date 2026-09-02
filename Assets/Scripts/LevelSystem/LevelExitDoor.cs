using UnityEngine;

public class LevelExitDoor : MonoBehaviour
{
    // ==================================================
    // EXIT TYPE
    // ==================================================

    public enum ExitType
    {
        NextLevel,
        GameEnding
    }


    [Header("Exit")]

    [SerializeField]
    private ExitType exitType =
        ExitType.NextLevel;


    // ==================================================
    // NEXT LEVEL
    // ==================================================

    [Header("Next Level")]

    [Tooltip(
        "Used only when Exit Type = Next Level."
    )]
    [SerializeField]
    private string nextSceneName =
        "Level_02";


    // ==================================================
    // DOOR
    // ==================================================

    [Header("Door State")]

    [Tooltip(
        "Assign the HideableTilemap that represents the actual exit door."
    )]
    [SerializeField]
    private HideableTilemap doorTilemap;


    [Tooltip(
        "If enabled, the door must be hidden/open before the player can leave."
    )]
    [SerializeField]
    private bool requireDoorOpen =
        true;


    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]

    [SerializeField]
    private LevelLoader levelLoader;


    // ==================================================
    // PLAYER
    // ==================================================

    [Header("Player")]

    [SerializeField]
    private string playerTag =
        "Player";


    // ==================================================
    // STATE
    // ==================================================

    private bool transitionStarted;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        ResolveLoader();
    }


    // ==================================================
    // TRIGGER
    // ==================================================

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        TryEnterDoor(
            other
        );
    }


    private void OnTriggerStay2D(
        Collider2D other)
    {
        TryEnterDoor(
            other
        );
    }


    // ==================================================
    // TRY ENTER
    // ==================================================

    private void TryEnterDoor(
        Collider2D other)
    {
        if (transitionStarted)
            return;


        if (!IsPlayer(
                other))
        {
            return;
        }


        if (!IsDoorOpen())
            return;


        switch (exitType)
        {
            case ExitType.GameEnding:

                HandleGameEnding();

                break;


            case ExitType.NextLevel:
            default:

                HandleNextLevel();

                break;
        }
    }


    // ==================================================
    // NEXT LEVEL
    // ==================================================

    private void HandleNextLevel()
    {
        ResolveLoader();


        if (levelLoader == null)
        {
            Debug.LogError(
                "LevelExitDoor: LevelLoader was not found.",
                this
            );

            return;
        }


        if (string.IsNullOrWhiteSpace(
                nextSceneName))
        {
            Debug.LogError(
                "LevelExitDoor: Next Scene Name is empty.",
                this
            );

            return;
        }


        // ----------------------------------------------
        // UNLOCK NEXT LEVEL
        // ----------------------------------------------

        LevelProgressManager progress =
            LevelProgressManager.Instance;


        if (progress != null)
        {
            string currentSceneName =
                gameObject.scene.name;


            bool progressionAllowed =
                progress
                    .CompleteLevelAndUnlockNext(
                        currentSceneName,
                        nextSceneName
                    );


            if (!progressionAllowed)
            {
                Debug.LogError(
                    "LevelExitDoor: Progression rejected. " +
                    currentSceneName +
                    " -> " +
                    nextSceneName,
                    this
                );

                return;
            }
        }
        else
        {
            Debug.LogWarning(
                "LevelExitDoor: LevelProgressManager was not found. " +
                "Loading next level without saving progression.",
                this
            );
        }


        transitionStarted =
            true;


        // ----------------------------------------------
        // VIDEO BEFORE NEXT LEVEL
        // ----------------------------------------------

        LevelVideoIntroManager videoIntro =
            LevelVideoIntroManager.Instance;


        if (videoIntro != null)
        {
            videoIntro.RequestLevel(
                nextSceneName
            );

            return;
        }


        // ----------------------------------------------
        // SAFE FALLBACK
        // ----------------------------------------------

        Debug.LogWarning(
            "LevelExitDoor: LevelVideoIntroManager was not found. " +
            "Loading next level directly.",
            this
        );


        levelLoader.LoadLevel(
            nextSceneName
        );
    }


    // ==================================================
    // GAME ENDING
    // ==================================================

    private void HandleGameEnding()
    {
        transitionStarted =
            true;


        LevelVideoIntroManager videoIntro =
            LevelVideoIntroManager.Instance;


        if (videoIntro != null)
        {
            videoIntro.PlayGameEnding();

            return;
        }


        /*
         * Safe fallback:
         * if the video system is missing, return to the
         * Bootstrap main menu without an ending video.
         */
        Debug.LogWarning(
            "LevelExitDoor: LevelVideoIntroManager was not found. " +
            "Returning to the main menu directly.",
            this
        );


        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .ReturnToMainMenuAfterGameEnding();

            return;
        }


        transitionStarted =
            false;


        Debug.LogError(
            "LevelExitDoor: Neither LevelVideoIntroManager nor UIManager was found.",
            this
        );
    }


    // ==================================================
    // PLAYER
    // ==================================================

    private bool IsPlayer(
        Collider2D other)
    {
        if (other == null)
            return false;


        PlayerMovement movement =
            other.GetComponentInParent
                <PlayerMovement>();


        if (movement != null)
        {
            return
                string.IsNullOrWhiteSpace(
                    playerTag
                ) ||
                movement.CompareTag(
                    playerTag
                );
        }


        if (other.attachedRigidbody != null)
        {
            GameObject bodyObject =
                other.attachedRigidbody
                    .gameObject;


            if (bodyObject != null &&
                bodyObject.CompareTag(
                    playerTag
                ))
            {
                return true;
            }
        }


        return
            other.CompareTag(
                playerTag
            );
    }


    // ==================================================
    // LOADER
    // ==================================================

    private void ResolveLoader()
    {
        if (levelLoader != null)
            return;


        levelLoader =
            LevelLoader.Instance;


        if (levelLoader == null)
        {
            levelLoader =
                FindAnyObjectByType
                    <LevelLoader>();
        }
    }


    // ==================================================
    // DOOR OPEN
    // ==================================================

    private bool IsDoorOpen()
    {
        if (!requireDoorOpen)
        {
            return true;
        }


        if (doorTilemap != null)
        {
            return
                doorTilemap.IsHidden &&
                !doorTilemap.IsTransitioning;
        }


        Debug.LogWarning(
            "LevelExitDoor: Door Tilemap is not assigned.",
            this
        );


        return false;
    }


    // ==================================================
    // RESET
    // ==================================================

    public void ResetTransitionState()
    {
        transitionStarted =
            false;
    }


    // ==================================================
    // VALIDATE
    // ==================================================

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(
                playerTag))
        {
            playerTag =
                "Player";
        }
    }
}
