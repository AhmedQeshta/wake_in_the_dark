using UnityEngine;

public class LevelExitDoor : MonoBehaviour
{
    // ==================================================
    // NEXT LEVEL
    // ==================================================

    [Header("Next Level")]

    [SerializeField]
    private string nextSceneName = "Level_02";


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
    private bool requireDoorOpen = true;


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
    private string playerTag = "Player";


    // ==================================================
    // STATE
    // ==================================================

    private bool transitionStarted;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        if (levelLoader == null)
        {
            levelLoader =
                FindAnyObjectByType<LevelLoader>();
        }
    }


    // ==================================================
    // ENTER
    // ==================================================

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        TryEnterDoor(other);
    }


    /*
     * Important:
     *
     * If the player is already standing inside
     * the trigger when the lever opens the door,
     * OnTriggerEnter2D will not happen again.
     *
     * OnTriggerStay2D handles that situation.
     */
    private void OnTriggerStay2D(
        Collider2D other)
    {
        TryEnterDoor(other);
    }


    // ==================================================
    // TRY ENTER
    // ==================================================

    private void TryEnterDoor(
        Collider2D other)
    {
        if (transitionStarted)
            return;


        // ----------------------------------------------
        // FIND PLAYER ROOT
        // ----------------------------------------------

        GameObject player =
            other.attachedRigidbody != null
                ? other.attachedRigidbody.gameObject
                : other.gameObject;


        // ----------------------------------------------
        // CHECK PLAYER
        // ----------------------------------------------

        if (!player.CompareTag(playerTag))
            return;


        // ----------------------------------------------
        // CHECK DOOR STATE
        // ----------------------------------------------

        if (!IsDoorOpen())
            return;


        // ----------------------------------------------
        // CHECK LOADER
        // ----------------------------------------------

        if (levelLoader == null)
        {
            levelLoader =
                FindAnyObjectByType<LevelLoader>();
        }


        if (levelLoader == null)
        {
            Debug.LogError(
                "LevelExitDoor: LevelLoader was not found.",
                this
            );

            return;
        }


        // ----------------------------------------------
        // CHECK NEXT SCENE
        // ----------------------------------------------

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError(
                "LevelExitDoor: Next Scene Name is empty.",
                this
            );

            return;
        }


        transitionStarted = true;


        Debug.Log(
            "Exit door entered. Loading: " +
            nextSceneName,
            this
        );


        levelLoader.LoadLevel(
            nextSceneName
        );
    }


    // ==================================================
    // DOOR OPEN CHECK
    // ==================================================

    private bool IsDoorOpen()
    {
        /*
         * Door doesn't require unlocking.
         */
        if (!requireDoorOpen)
        {
            return true;
        }


        /*
         * HideableTilemap.Hide() makes
         * IsHidden true after your lever opens it.
         */
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
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
        {
            playerTag = "Player";
        }
    }
}