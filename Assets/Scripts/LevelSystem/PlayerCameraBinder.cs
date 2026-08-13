using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class PlayerCameraBinder : MonoBehaviour
{
    // ==================================================
    // UNITY CAMERA
    // ==================================================

    [Header("Unity Camera")]

    [SerializeField]
    private Camera unityCamera;


    // ==================================================
    // CINEMACHINE
    // ==================================================

    [Header("Gameplay Cinemachine Camera")]

    [SerializeField]
    private CinemachineCamera gameplayCamera;

    [SerializeField]
    private CinemachineConfiner2D confiner;


    // ==================================================
    // PLAYER
    // ==================================================

    [Header("Player")]

    [SerializeField]
    private Transform trackingTarget;

    [SerializeField]
    private PlayerMovement playerMovement;


    // ==================================================
    // PUBLIC
    // ==================================================

    public Camera UnityCamera =>
        unityCamera;


    public CinemachineCamera GameplayCamera =>
        gameplayCamera;


    public Transform TrackingTarget =>
        trackingTarget;


    public PlayerMovement PlayerMovement =>
        playerMovement;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        BindNow();
    }


    // ==================================================
    // BIND
    // ==================================================

    public void BindNow()
    {
        ResolveReferences();


        // ==================================================
        // UNITY CAMERA
        // ==================================================

        if (unityCamera != null)
        {
            unityCamera.rect =
                new Rect(
                    0f,
                    0f,
                    1f,
                    1f
                );


            unityCamera.enabled =
                true;
        }


        // ==================================================
        // PLAYER TARGET
        // ==================================================

        if (gameplayCamera != null &&
            trackingTarget != null)
        {
            gameplayCamera.Follow =
                trackingTarget;
        }


        // ==================================================
        // FIND THIS LEVEL'S CAMERA BOUNDS
        // ==================================================

        LevelCameraBounds levelBounds =
            FindComponentInMyScene<LevelCameraBounds>();


        if (levelBounds == null)
        {
            Debug.LogError(
                "PlayerCameraBinder: LevelCameraBounds " +
                "not found in " +
                gameObject.scene.name,
                this
            );

            return;
        }


        if (levelBounds.BoundingCollider == null)
        {
            Debug.LogError(
                "PlayerCameraBinder: Camera bounds collider is null.",
                levelBounds
            );

            return;
        }


        // ==================================================
        // CINEMACHINE CONFINER
        // ==================================================

        if (confiner == null)
        {
            Debug.LogError(
                "PlayerCameraBinder: CinemachineConfiner2D missing.",
                this
            );

            return;
        }


        confiner.BoundingShape2D =
            levelBounds.BoundingCollider;


        /*
         * Required because Level_01,
         * Level_02, Level_03 can all have
         * completely different polygons.
         */
        confiner.InvalidateBoundingShapeCache();


        Debug.Log(
            "Gameplay camera configured.\n" +
            "Scene: " +
            gameObject.scene.name +
            "\nPlayer: " +
            trackingTarget.name +
            "\nBounds: " +
            levelBounds.BoundingCollider.name,
            this
        );
    }


    // ==================================================
    // RESOLVE REFERENCES
    // ==================================================

    private void ResolveReferences()
    {
        // ----------------------------------------------
        // CAMERA
        // ----------------------------------------------

        if (unityCamera == null)
        {
            Camera[] cameras =
                GetComponentsInChildren<Camera>(
                    true
                );


            foreach (Camera camera in cameras)
            {
                if (camera != null &&
                    camera.CompareTag(
                        "MainCamera"
                    ))
                {
                    unityCamera =
                        camera;

                    break;
                }
            }


            if (unityCamera == null &&
                cameras.Length > 0)
            {
                unityCamera =
                    cameras[0];
            }
        }


        // ----------------------------------------------
        // GAMEPLAY CAMERA
        // ----------------------------------------------

        if (gameplayCamera == null)
        {
            gameplayCamera =
                GetComponentInChildren<CinemachineCamera>(
                    true
                );
        }


        // ----------------------------------------------
        // CONFINER
        // ----------------------------------------------

        if (confiner == null)
        {
            confiner =
                gameplayCamera != null
                    ? gameplayCamera
                        .GetComponent<CinemachineConfiner2D>()
                    : null;
        }


        // ----------------------------------------------
        // PLAYER
        // ----------------------------------------------

        if (playerMovement == null)
        {
            playerMovement =
                GetComponentInChildren<PlayerMovement>(
                    true
                );
        }


        if (trackingTarget == null &&
            playerMovement != null)
        {
            trackingTarget =
                playerMovement.transform;
        }
    }


    // ==================================================
    // FIND COMPONENT IN THIS SCENE ONLY
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


            T component =
                root.GetComponentInChildren<T>(
                    true
                );


            if (component != null)
            {
                return component;
            }
        }


        return null;
    }
}