using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class LevelCameraSetup : MonoBehaviour
{
    [Header("Main Camera")]
    [SerializeField] private Camera mainCamera;

    [Header("Gameplay Camera")]
    [SerializeField] private CinemachineCamera gameplayCamera;

    [Header("Gameplay Camera Confiner")]
    [SerializeField] private CinemachineConfiner2D gameplayConfiner;
    [SerializeField] private Collider2D cameraBounds;

    [Header("Map Fit")]
    [SerializeField] private Collider2D mapViewBounds;
    [SerializeField] private bool autoFitGameplayToMap = true;
    [SerializeField, Min(0f)] private float mapPadding = 0.35f;
    [SerializeField, Range(0.5f, 1.2f)] private float gameplayMapSizeMultiplier = 1f;

    [Header("Player")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private PlayerMovement playerMovement;

    public Camera MainCamera => mainCamera;
    public CinemachineCamera GameplayCamera => gameplayCamera;
    public Transform PlayerTarget => playerTarget;
    public PlayerMovement PlayerMovement => playerMovement;
    public Collider2D CameraBounds => cameraBounds;
    public Collider2D MapViewBounds => mapViewBounds;

    private void Start()
    {
        BindScene();
    }

    public void BindScene()
    {
        ResolveReferences();

        if (mainCamera == null || gameplayCamera == null || playerTarget == null)
            return;

        mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
        mainCamera.enabled = true;

        // Timeline owns the intro cameras.
        gameplayCamera.Follow = playerTarget;

        if (autoFitGameplayToMap)
            FitGameplayToMap();

        ConfigureGameplayConfiner();

    }

    private float CalculateRequiredMapSize()
    {
        if (mapViewBounds == null)
            return -1f;

        Bounds bounds = mapViewBounds.bounds;
        float aspect = mainCamera != null ? mainCamera.aspect : 16f / 9f;

        if (aspect <= 0f)
            aspect = 16f / 9f;

        float verticalRequired = bounds.size.y * 0.5f;
        float horizontalRequired = bounds.size.x / (2f * aspect);
        float requiredSize = Mathf.Max(verticalRequired, horizontalRequired);

        requiredSize += mapPadding;

        return requiredSize;
    }

    public void FitGameplayToMap()
    {
        ResolveReferences();

        if (gameplayCamera == null || mapViewBounds == null || mainCamera == null)
            return;

        float requiredSize = CalculateRequiredMapSize();

        if (requiredSize <= 0f)
            return;

        float gameplaySize = requiredSize * gameplayMapSizeMultiplier;

        LensSettings lens = gameplayCamera.Lens;
        lens.OrthographicSize = gameplaySize;
        gameplayCamera.Lens = lens;

        if (playerTarget != null)
            gameplayCamera.Follow = playerTarget;

        if (gameplayConfiner != null)
            gameplayConfiner.InvalidateLensCache();

    }

    private void ConfigureGameplayConfiner()
    {
        if (gameplayConfiner == null || cameraBounds == null)
            return;

        gameplayConfiner.BoundingShape2D = cameraBounds;
        gameplayConfiner.InvalidateBoundingShapeCache();
        gameplayConfiner.InvalidateLensCache();
    }

    private void ResolveReferences()
    {
        if (mainCamera == null)
            mainCamera = FindMainCameraInThisScene();

        if (playerMovement == null)
            playerMovement = FindComponentInMyScene<PlayerMovement>();

        if (playerTarget == null && playerMovement != null)
            playerTarget = playerMovement.transform;

        if (gameplayCamera == null)
            gameplayCamera = FindCinemachineCameraByName("GameplayCamera");

        if (gameplayConfiner == null && gameplayCamera != null)
            gameplayConfiner = gameplayCamera.GetComponent<CinemachineConfiner2D>();
    }

    private Camera FindMainCameraInThisScene()
    {
        Scene scene = gameObject.scene;

        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);

            foreach (Camera camera in cameras)
            {
                if (camera != null && camera.CompareTag("MainCamera"))
                    return camera;
            }
        }

        return null;
    }

    private CinemachineCamera FindCinemachineCameraByName(string objectName)
    {
        Scene scene = gameObject.scene;

        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CinemachineCamera[] cameras = root.GetComponentsInChildren<CinemachineCamera>(true);

            foreach (CinemachineCamera camera in cameras)
            {
                if (camera == null)
                    continue;

                if (string.Equals(camera.gameObject.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                    return camera;
            }
        }

        return null;
    }

    private T FindComponentInMyScene<T>() where T : Component
    {
        Scene scene = gameObject.scene;

        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);

            if (result != null)
                return result;
        }

        return null;
    }

    private void OnValidate()
    {
        mapPadding = Mathf.Max(0f, mapPadding);
        gameplayMapSizeMultiplier = Mathf.Clamp(gameplayMapSizeMultiplier, 0.5f, 1.2f);
    }
}
