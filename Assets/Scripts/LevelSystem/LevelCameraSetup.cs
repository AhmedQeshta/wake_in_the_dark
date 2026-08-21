using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class LevelCameraSetup : MonoBehaviour
{
    // ==================================================
    // MAIN UNITY CAMERA
    // ==================================================

    [Header("Main Camera")]
    [SerializeField] private Camera mainCamera;

    // ==================================================
    // CINEMACHINE CAMERAS
    // ==================================================

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera introCamera;
    [SerializeField] private CinemachineCamera gameplayCamera;

    // ==================================================
    // GAMEPLAY CONFINER
    // ==================================================

    [Header("Gameplay Camera Confiner")]
    [SerializeField] private CinemachineConfiner2D gameplayConfiner;
    [SerializeField] private Collider2D cameraBounds;

    // ==================================================
    // MAP FIT
    // ==================================================

    [Header("Map Fit")]
    [Tooltip("Bounds representing the entire level.")]
    [SerializeField] private Collider2D mapViewBounds;

    [Tooltip("Automatically resize the GameplayCamera using the map size.")]
    [SerializeField] private bool autoFitGameplayToMap = true;

    [Tooltip("Extra world-space room around the level.")]
    [SerializeField, Min(0f)] private float mapPadding = 0.35f;

    [Tooltip("1 = GameplayCamera shows the full map. Less than 1 = zooms gameplay in.")]
    [SerializeField, Range(0.5f, 1.2f)] private float gameplayMapSizeMultiplier = 1f;

    // ==================================================
    // PLAYER
    // ==================================================

    [Header("Player")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private PlayerMovement playerMovement;

    // ==================================================
    // PUBLIC VALUES
    // ==================================================

    public Camera MainCamera => mainCamera;
    public CinemachineCamera IntroCamera => introCamera;
    public CinemachineCamera GameplayCamera => gameplayCamera;
    public Transform PlayerTarget => playerTarget;
    public PlayerMovement PlayerMovement => playerMovement;
    public Collider2D CameraBounds => cameraBounds;
    public Collider2D MapViewBounds => mapViewBounds;

    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        BindScene();
    }

    // ==================================================
    // BIND SCENE
    // ==================================================

    public void BindScene()
    {
        ResolveReferences();

        if (mainCamera == null)
        {
            Debug.LogError("LevelCameraSetup: Main Camera is missing.", this);
            return;
        }

        if (playerTarget == null)
        {
            Debug.LogError("LevelCameraSetup: Player was not found.", this);
            return;
        }

        mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
        mainCamera.enabled = true;

        // IntroCamera does NOT follow Player.
        if (introCamera != null)
        {
            introCamera.Follow = null;
        }

        // GameplayCamera follows Player.
        if (gameplayCamera != null)
        {
            gameplayCamera.Follow = playerTarget;
        }

        if (autoFitGameplayToMap)
        {
            FitGameplayToMap();
        }

        ConfigureGameplayConfiner();

        Debug.Log("Level camera setup complete: " + gameObject.scene.name, this);
    }

    // ==================================================
    // MAP SIZE CALCULATION
    // ==================================================

    private float CalculateRequiredMapSize()
    {
        if (mapViewBounds == null) return -1f;

        Bounds bounds = mapViewBounds.bounds;
        float aspect = mainCamera != null ? mainCamera.aspect : 16f / 9f;
        if (aspect <= 0f) aspect = 16f / 9f;

        float verticalRequired = bounds.size.y * 0.5f;
        float horizontalRequired = bounds.size.x / (2f * aspect);

        float requiredSize = Mathf.Max(verticalRequired, horizontalRequired);
        requiredSize += mapPadding;

        return requiredSize;
    }

    // ==================================================
    // FIT GAMEPLAY CAMERA
    // ==================================================

    public void FitGameplayToMap()
    {
        if (gameplayCamera == null || mapViewBounds == null || mainCamera == null) return;

        float requiredSize = CalculateRequiredMapSize();
        if (requiredSize <= 0f) return;

        float gameplaySize = requiredSize * gameplayMapSizeMultiplier;

        LensSettings lens = gameplayCamera.Lens;
        lens.OrthographicSize = gameplaySize;
        gameplayCamera.Lens = lens;

        if (playerTarget != null)
        {
            gameplayCamera.Follow = playerTarget;
        }

        if (gameplayConfiner != null)
        {
            gameplayConfiner.InvalidateLensCache();
        }

        Debug.Log("GameplayCamera configured. Size: " + gameplaySize, this);
    }

    // ==================================================
    // GAMEPLAY CONFINER
    // ==================================================

    private void ConfigureGameplayConfiner()
    {
        if (gameplayConfiner == null || cameraBounds == null) return;

        gameplayConfiner.BoundingShape2D = cameraBounds;
        gameplayConfiner.InvalidateBoundingShapeCache();
        gameplayConfiner.InvalidateLensCache();
    }

    // ==================================================
    // RESOLVE REFERENCES
    // ==================================================

    private void ResolveReferences()
    {
        if (mainCamera == null) mainCamera = FindMainCameraInThisScene();
        if (playerMovement == null) playerMovement = FindComponentInMyScene<PlayerMovement>();

        if (playerTarget == null && playerMovement != null)
        {
            playerTarget = playerMovement.transform;
        }

        if (gameplayConfiner == null && gameplayCamera != null)
        {
            gameplayConfiner = gameplayCamera.GetComponent<CinemachineConfiner2D>();
        }
    }

    private Camera FindMainCameraInThisScene()
    {
        Scene scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
            foreach (Camera camera in cameras)
            {
                if (camera.CompareTag("MainCamera")) return camera;
            }
        }
        return null;
    }

    private T FindComponentInMyScene<T>() where T : Component
    {
        Scene scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null) return result;
        }
        return null;
    }

    private void OnValidate()
    {
        mapPadding = Mathf.Max(0f, mapPadding);
        gameplayMapSizeMultiplier = Mathf.Clamp(gameplayMapSizeMultiplier, 0.5f, 1.2f);
    }
}