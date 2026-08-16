using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class LevelCameraSetup : MonoBehaviour
{
    // ==================================================
    // MAIN UNITY CAMERA
    // ==================================================

    [Header("Main Camera")]
    [SerializeField]
    private Camera mainCamera;


    // ==================================================
    // CINEMACHINE CAMERAS
    // ==================================================

    [Header("Cinemachine Cameras")]

    [SerializeField]
    private CinemachineCamera introCamera;

    [SerializeField]
    private CinemachineCamera longShotCamera;

    [SerializeField]
    private CinemachineCamera gameplayCamera;


    // ==================================================
    // GAMEPLAY CONFINER
    // ==================================================

    [Header("Gameplay Camera Confiner")]

    [SerializeField]
    private CinemachineConfiner2D gameplayConfiner;

    [SerializeField]
    private Collider2D cameraBounds;


    // ==================================================
    // MAP FIT
    // ==================================================

    [Header("Map Fit")]

    [Tooltip(
        "Bounds representing the entire level that should " +
        "be visible by the LongShotCamera."
    )]
    [SerializeField]
    private Collider2D mapViewBounds;


    [Tooltip(
        "Automatically resize the LongShotCamera so the entire map fits."
    )]
    [SerializeField]
    private bool autoFitLongShotToMap = true;


    [Tooltip(
        "Automatically resize the GameplayCamera using the same map size."
    )]
    [SerializeField]
    private bool autoFitGameplayToMap = true;


    [Tooltip(
        "Extra world-space room around the level."
    )]
    [SerializeField, Min(0f)]
    private float mapPadding = 0.35f;


    [Tooltip(
        "1 = GameplayCamera shows the same full map as LongShotCamera.\n" +
        "Less than 1 = zooms gameplay in and makes Player-follow movement more visible."
    )]
    [SerializeField, Range(0.5f, 1.2f)]
    private float gameplayMapSizeMultiplier = 1f;


    // ==================================================
    // PLAYER
    // ==================================================

    [Header("Player")]

    [SerializeField]
    private Transform playerTarget;

    [SerializeField]
    private PlayerMovement playerMovement;


    // ==================================================
    // PUBLIC VALUES
    // ==================================================

    public Camera MainCamera =>
        mainCamera;

    public CinemachineCamera IntroCamera =>
        introCamera;

    public CinemachineCamera LongShotCamera =>
        longShotCamera;

    public CinemachineCamera GameplayCamera =>
        gameplayCamera;

    public Transform PlayerTarget =>
        playerTarget;

    public PlayerMovement PlayerMovement =>
        playerMovement;

    public Collider2D CameraBounds =>
        cameraBounds;

    public Collider2D MapViewBounds =>
        mapViewBounds;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        /*
         * This keeps the scene working if Level_01,
         * Level_02 or Level_03 is tested directly.
         *
         * When Bootstrap loads a level,
         * LevelLoader also calls BindScene().
         */
        BindScene();
    }


    // ==================================================
    // BIND SCENE
    // ==================================================

    public void BindScene()
    {
        ResolveReferences();


        // ----------------------------------------------
        // MAIN CAMERA
        // ----------------------------------------------

        if (mainCamera == null)
        {
            Debug.LogError(
                "LevelCameraSetup: Main Camera is missing.",
                this
            );

            return;
        }


        // ----------------------------------------------
        // PLAYER
        // ----------------------------------------------

        if (playerTarget == null)
        {
            Debug.LogError(
                "LevelCameraSetup: Player was not found.",
                this
            );

            return;
        }


        mainCamera.rect =
            new Rect(
                0f,
                0f,
                1f,
                1f
            );

        mainCamera.enabled =
            true;


        // ==================================================
        // INTRO CAMERA
        // ==================================================

        /*
         * IntroCamera stays wherever you placed it
         * in the scene.
         *
         * Example:
         * directly over ExitDoor.
         *
         * It does NOT follow Player.
         */

        if (introCamera != null)
        {
            introCamera.Follow =
                null;
        }


        // ==================================================
        // LONG SHOT CAMERA
        // ==================================================

        /*
         * LongShotCamera must remain fixed
         * on the entire map.
         */

        if (longShotCamera != null)
        {
            longShotCamera.Follow =
                null;
        }


        // ==================================================
        // GAMEPLAY CAMERA
        // ==================================================

        /*
         * GameplayCamera is our final camera.
         *
         * It has:
         *
         * wide map lens
         * +
         * Player tracking
         * +
         * Confiner
         */

        if (gameplayCamera != null)
        {
            gameplayCamera.Follow =
                playerTarget;
        }


        // ==================================================
        // FIT CAMERAS
        // ==================================================

        if (autoFitLongShotToMap)
        {
            FitLongShotToMap();
        }


        if (autoFitGameplayToMap)
        {
            FitGameplayToMap();
        }


        // ==================================================
        // CONFINER
        // ==================================================

        ConfigureGameplayConfiner();


        Debug.Log(
            "Level camera setup complete: " +
            gameObject.scene.name,
            this
        );
    }


    // ==================================================
    // MAP SIZE CALCULATION
    // ==================================================

    private float CalculateRequiredMapSize()
    {
        if (mapViewBounds == null)
        {
            return -1f;
        }


        Bounds bounds =
            mapViewBounds.bounds;


        // ----------------------------------------------
        // CAMERA ASPECT
        // ----------------------------------------------

        float aspect =
            mainCamera != null
                ? mainCamera.aspect
                : 16f / 9f;


        if (aspect <= 0f)
        {
            aspect =
                16f / 9f;
        }


        // ----------------------------------------------
        // VERTICAL REQUIREMENT
        // ----------------------------------------------

        float verticalRequired =
            bounds.size.y *
            0.5f;


        // ----------------------------------------------
        // HORIZONTAL REQUIREMENT
        // ----------------------------------------------

        float horizontalRequired =
            bounds.size.x /
            (
                2f *
                aspect
            );


        // ----------------------------------------------
        // USE BIGGER REQUIREMENT
        // ----------------------------------------------

        float requiredSize =
            Mathf.Max(
                verticalRequired,
                horizontalRequired
            );


        requiredSize +=
            mapPadding;


        return requiredSize;
    }


    // ==================================================
    // FIT LONG SHOT
    // ==================================================

    public void FitLongShotToMap()
    {
        if (longShotCamera == null ||
            mapViewBounds == null ||
            mainCamera == null)
        {
            return;
        }


        float requiredSize =
            CalculateRequiredMapSize();


        if (requiredSize <= 0f)
            return;


        // ----------------------------------------------
        // LENS
        // ----------------------------------------------

        LensSettings lens =
            longShotCamera.Lens;


        lens.OrthographicSize =
            requiredSize;


        longShotCamera.Lens =
            lens;


        // ----------------------------------------------
        // CENTER ON MAP
        // ----------------------------------------------

        Bounds bounds =
            mapViewBounds.bounds;


        Vector3 cameraPosition =
            longShotCamera
                .transform
                .position;


        cameraPosition.x =
            bounds.center.x;

        cameraPosition.y =
            bounds.center.y;


        /*
         * Keep the camera's original Z.
         */

        longShotCamera
            .transform
            .position =
            cameraPosition;


        /*
         * Absolutely no Player follow.
         */

        longShotCamera.Follow =
            null;


        Debug.Log(
            "LongShotCamera fitted to entire map.\n" +
            "Scene: " +
            gameObject.scene.name +
            "\nMap Size: " +
            bounds.size +
            "\nOrthographic Size: " +
            requiredSize,
            this
        );
    }


    // ==================================================
    // FIT GAMEPLAY CAMERA
    // ==================================================

    public void FitGameplayToMap()
    {
        if (gameplayCamera == null ||
            mapViewBounds == null ||
            mainCamera == null)
        {
            return;
        }


        float requiredSize =
            CalculateRequiredMapSize();


        if (requiredSize <= 0f)
            return;


        /*
         * 1.0:
         *
         * exactly the same view size
         * as LongShotCamera.
         *
         * Example:
         *
         * LongShot = 5.5
         * Gameplay = 5.5
         */

        float gameplaySize =
            requiredSize *
            gameplayMapSizeMultiplier;


        LensSettings lens =
            gameplayCamera.Lens;


        lens.OrthographicSize =
            gameplaySize;


        gameplayCamera.Lens =
            lens;


        // ----------------------------------------------
        // PLAYER FOLLOW
        // ----------------------------------------------

        if (playerTarget != null)
        {
            gameplayCamera.Follow =
                playerTarget;
        }


        // ----------------------------------------------
        // LENS CHANGED
        // REFRESH CONFINER
        // ----------------------------------------------

        if (gameplayConfiner != null)
        {
            gameplayConfiner
                .InvalidateLensCache();
        }


        Debug.Log(
            "GameplayCamera configured.\n" +
            "Scene: " +
            gameObject.scene.name +
            "\nOrthographic Size: " +
            gameplaySize +
            "\nFollowing: " +
            (
                playerTarget != null
                    ? playerTarget.name
                    : "NONE"
            ),
            this
        );
    }


    // ==================================================
    // GAMEPLAY CONFINER
    // ==================================================

    private void ConfigureGameplayConfiner()
    {
        if (gameplayConfiner == null)
        {
            Debug.LogWarning(
                "LevelCameraSetup: Gameplay Confiner is missing.",
                this
            );

            return;
        }


        if (cameraBounds == null)
        {
            Debug.LogWarning(
                "LevelCameraSetup: Camera Bounds are missing.",
                this
            );

            return;
        }


        gameplayConfiner.BoundingShape2D =
            cameraBounds;


        gameplayConfiner
            .InvalidateBoundingShapeCache();


        gameplayConfiner
            .InvalidateLensCache();
    }


    // ==================================================
    // RESET LONG SHOT
    // ==================================================

    public void ResetLongShotOverview()
    {
        if (longShotCamera == null)
            return;


        longShotCamera.Follow =
            null;


        FitLongShotToMap();
    }


    // ==================================================
    // LEGACY LONG SHOT FOLLOW
    // ==================================================

    /*
     * Kept so an old UnityEvent / old script reference
     * does not suddenly break.
     *
     * The new LevelCameraDirector DOES NOT use this
     * during normal gameplay.
     */

    public void EnableLongShotFollow()
    {
        ResolveReferences();


        if (longShotCamera == null ||
            playerTarget == null)
        {
            return;
        }


        longShotCamera.Follow =
            playerTarget;
    }


    public void DisableLongShotFollow()
    {
        if (longShotCamera == null)
            return;


        longShotCamera.Follow =
            null;
    }


    // ==================================================
    // RESOLVE REFERENCES
    // ==================================================

    private void ResolveReferences()
    {
        // ----------------------------------------------
        // MAIN CAMERA
        // ----------------------------------------------

        if (mainCamera == null)
        {
            mainCamera =
                FindMainCameraInThisScene();
        }


        // ----------------------------------------------
        // PLAYER MOVEMENT
        // ----------------------------------------------

        if (playerMovement == null)
        {
            playerMovement =
                FindComponentInMyScene
                    <PlayerMovement>();
        }


        // ----------------------------------------------
        // PLAYER TRANSFORM
        // ----------------------------------------------

        if (playerTarget == null &&
            playerMovement != null)
        {
            playerTarget =
                playerMovement.transform;
        }


        // ----------------------------------------------
        // GAMEPLAY CONFINER
        // ----------------------------------------------

        if (gameplayConfiner == null &&
            gameplayCamera != null)
        {
            gameplayConfiner =
                gameplayCamera
                    .GetComponent
                    <CinemachineConfiner2D>();
        }
    }


    // ==================================================
    // FIND MAIN CAMERA
    // ==================================================

    private Camera FindMainCameraInThisScene()
    {
        Scene scene =
            gameObject.scene;


        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            return null;
        }


        GameObject[] roots =
            scene.GetRootGameObjects();


        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;


            Camera[] cameras =
                root.GetComponentsInChildren<Camera>(
                    true
                );


            foreach (Camera camera in cameras)
            {
                if (camera == null)
                    continue;


                if (camera.CompareTag(
                        "MainCamera"))
                {
                    return camera;
                }
            }
        }


        return null;
    }


    // ==================================================
    // FIND COMPONENT IN THIS LEVEL ONLY
    // ==================================================

    private T FindComponentInMyScene<T>()
        where T : Component
    {
        Scene scene =
            gameObject.scene;


        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            return null;
        }


        GameObject[] roots =
            scene.GetRootGameObjects();


        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;


            T result =
                root.GetComponentInChildren<T>(
                    true
                );


            if (result != null)
            {
                return result;
            }
        }


        return null;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        mapPadding =
            Mathf.Max(
                0f,
                mapPadding
            );


        gameplayMapSizeMultiplier =
            Mathf.Clamp(
                gameplayMapSizeMultiplier,
                0.5f,
                1.2f
            );
    }
}