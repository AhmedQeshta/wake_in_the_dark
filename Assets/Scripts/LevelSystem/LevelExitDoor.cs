using UnityEngine;

public class LevelExitDoor : MonoBehaviour
{
    // ==================================================
    // NEXT LEVEL
    // ==================================================

    [Header("Next Level")]

    [SerializeField]
    private string nextSceneName =
        "Level_02";


    // ==================================================
    // DOOR
    // ==================================================

    [Header("Door State")]

    [Tooltip(
        "Assign the HideableTilemap that represents " +
        "the actual exit door."
    )]
    [SerializeField]
    private HideableTilemap doorTilemap;


    [Tooltip(
        "If enabled, the door must be hidden/open " +
        "before the player can leave."
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


        GameObject player =
            other.attachedRigidbody != null
                ? other.attachedRigidbody.gameObject
                : other.gameObject;


        if (!player.CompareTag(
                playerTag))
        {
            return;
        }


        if (!IsDoorOpen())
            return;


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
        // UNLOCK NEXT LEVEL FIRST
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
        // NEW: VIDEO BEFORE NEXT LEVEL
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


        /*
         * Safe fallback:
         * if the video system is missing, keep the old behavior.
         */
        Debug.LogWarning(
            "LevelExitDoor: LevelVideoIntroManager was not found. " +
            "Loading the next level directly.",
            this
        );


        levelLoader.LoadLevel(
            nextSceneName
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
