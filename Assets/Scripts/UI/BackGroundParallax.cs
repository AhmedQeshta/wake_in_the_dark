using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
public class BackGroundParallax : MonoBehaviour
{
    // ==================================================
    // PARALLAX LAYER
    // ==================================================

    [Serializable]
    public class ParallaxLayer
    {
        [Header("Layer")]
        public Transform target;


        [Header("Horizontal Parallax")]

        [Tooltip(
            "0 = very far away / almost follows the camera.\n" +
            "1 = much stronger parallax movement."
        )]
        [Range(0f, 1f)]
        public float horizontalStrength = 0.1f;


        [Header("Vertical Parallax")]

        public bool useVerticalParallax = true;


        [Range(0f, 1f)]
        public float verticalStrength = 0.03f;


        [Header("Optional Pixel Snap")]

        [Tooltip(
            "Leave OFF when Pixel Perfect Camera is enabled."
        )]
        public bool snapTransformToPixelGrid = false;
    }


    // ==================================================
    // CAMERA
    // ==================================================

    [Header("Camera")]

    [Tooltip(
        "Normally leave assigned to this level's Main Camera."
    )]
    [SerializeField]
    private Camera mainCamera;


    [SerializeField]
    private bool autoFindCamera = true;


    // ==================================================
    // PIXEL ART
    // ==================================================

    [Header("Pixel Art")]

    [SerializeField, Min(1)]
    private int pixelsPerUnit = 128;


    // ==================================================
    // LAYERS
    // ==================================================

    [Header("Parallax Layers")]

    [SerializeField]
    private ParallaxLayer[] layers;


    // ==================================================
    // STATE
    // ==================================================

    private Vector3 cameraStartPosition;

    private Vector3[] layerStartPositions;

    private Camera currentlyBoundCamera;

    private bool initialized;

    /*
     * IMPORTANT:
     *
     * Levels 4-5 use an IntroCamera before GameplayCamera.
     * While the cinematic camera is moving, parallax must
     * be paused. Otherwise the large camera move is treated
     * like player movement and can push the backgrounds out
     * of the visible area.
     */
    private bool parallaxPaused;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        Initialize();
    }


    // ==================================================
    // INITIALIZE
    // ==================================================

    private void Initialize()
    {
        if (initialized)
            return;


        if (mainCamera == null &&
            autoFindCamera)
        {
            mainCamera =
                FindMainCameraInThisScene();
        }


        if (mainCamera == null)
        {
            Debug.LogError(
                "BackGroundParallax: Main Camera was not found in scene '" +
                gameObject.scene.name +
                "'.",
                this
            );

            return;
        }


        if (layers == null ||
            layers.Length == 0)
        {
            Debug.LogWarning(
                "BackGroundParallax: No layers configured.",
                this
            );

            return;
        }


        currentlyBoundCamera =
            mainCamera;


        CaptureCurrentPositionsAsNewOrigin();


        initialized =
            true;


        Debug.Log(
            "Parallax initialized. Scene: " +
            gameObject.scene.name +
            " | Camera: " +
            mainCamera.name +
            " | Layers: " +
            layers.Length,
            this
        );
    }


    // ==================================================
    // LATE UPDATE
    // ==================================================

    private void LateUpdate()
    {
        if (!initialized)
        {
            Initialize();

            if (!initialized)
                return;
        }


        // --------------------------------------------------
        // CAMERA VALIDATION
        // --------------------------------------------------

        if (mainCamera == null ||
            mainCamera.gameObject.scene !=
            gameObject.scene)
        {
            TryRebindCamera();
        }


        if (mainCamera == null)
            return;


        // --------------------------------------------------
        // CINEMATIC CAMERA MOVEMENT IS IGNORED
        // --------------------------------------------------

        if (parallaxPaused)
            return;


        Vector3 cameraDelta =
            mainCamera.transform.position -
            cameraStartPosition;


        // --------------------------------------------------
        // MOVE LAYERS
        // --------------------------------------------------

        for (int i = 0;
             i < layers.Length;
             i++)
        {
            ParallaxLayer layer =
                layers[i];


            if (layer == null ||
                layer.target == null)
            {
                continue;
            }


            Vector3 startPosition =
                layerStartPositions[i];


            Vector3 newPosition =
                startPosition;


            /*
             * A far background should follow most of the
             * camera movement, so it appears to move slowly
             * on screen.
             */
            float horizontalFollow =
                1f -
                Mathf.Clamp01(
                    layer.horizontalStrength
                );


            newPosition.x =
                startPosition.x +
                (
                    cameraDelta.x *
                    horizontalFollow
                );


            if (layer.useVerticalParallax)
            {
                float verticalFollow =
                    1f -
                    Mathf.Clamp01(
                        layer.verticalStrength
                    );


                newPosition.y =
                    startPosition.y +
                    (
                        cameraDelta.y *
                        verticalFollow
                    );
            }
            else
            {
                newPosition.y =
                    startPosition.y;
            }


            newPosition.z =
                startPosition.z;


            if (layer.snapTransformToPixelGrid)
            {
                newPosition.x =
                    SnapToPixelGrid(
                        newPosition.x
                    );


                newPosition.y =
                    SnapToPixelGrid(
                        newPosition.y
                    );
            }


            layer.target.position =
                newPosition;
        }
    }


    // ==================================================
    // PAUSE PARALLAX
    // ==================================================

    public void PauseParallax()
    {
        if (!initialized)
        {
            Initialize();
        }


        /*
         * Do not move layers while IntroCamera or a
         * door-focus camera is controlling Main Camera.
         */
        parallaxPaused =
            true;
    }


    // ==================================================
    // RESUME PARALLAX
    // ==================================================

    public void ResumeParallaxFromCurrentCamera()
    {
        if (!initialized)
        {
            Initialize();
        }


        if (!initialized ||
            mainCamera == null)
        {
            return;
        }


        /*
         * The GameplayCamera is now settled on the Player.
         *
         * Use the current camera position and current
         * background positions as the new parallax origin.
         *
         * This prevents the previous IntroCamera movement
         * from pushing the layers away.
         */
        CaptureCurrentPositionsAsNewOrigin();


        parallaxPaused =
            false;


        Debug.Log(
            "Parallax resumed from gameplay camera. Scene: " +
            gameObject.scene.name,
            this
        );
    }


    // ==================================================
    // PUBLIC RESET
    // ==================================================

    public void ResetParallaxOrigin()
    {
        ResumeParallaxFromCurrentCamera();
    }


    // ==================================================
    // CAPTURE ORIGIN
    // ==================================================

    private void CaptureCurrentPositionsAsNewOrigin()
    {
        if (mainCamera == null)
            return;


        cameraStartPosition =
            mainCamera.transform.position;


        if (layerStartPositions == null ||
            layerStartPositions.Length !=
            layers.Length)
        {
            layerStartPositions =
                new Vector3[layers.Length];
        }


        for (int i = 0;
             i < layers.Length;
             i++)
        {
            ParallaxLayer layer =
                layers[i];


            if (layer == null ||
                layer.target == null)
            {
                continue;
            }


            layerStartPositions[i] =
                layer.target.position;
        }
    }


    // ==================================================
    // CAMERA REBIND
    // ==================================================

    private void TryRebindCamera()
    {
        if (!autoFindCamera)
            return;


        Camera newCamera =
            FindMainCameraInThisScene();


        if (newCamera == null)
            return;


        if (newCamera ==
            currentlyBoundCamera)
        {
            mainCamera =
                newCamera;

            return;
        }


        mainCamera =
            newCamera;


        currentlyBoundCamera =
            newCamera;


        CaptureCurrentPositionsAsNewOrigin();


        Debug.Log(
            "Parallax rebound to Main Camera: " +
            newCamera.name,
            this
        );
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


        Camera firstCamera =
            null;


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


                if (firstCamera == null)
                {
                    firstCamera =
                        camera;
                }


                if (camera.CompareTag(
                        "MainCamera"))
                {
                    return camera;
                }
            }
        }


        return firstCamera;
    }


    // ==================================================
    // PIXEL GRID
    // ==================================================

    private float SnapToPixelGrid(
        float value)
    {
        float unitsPerPixel =
            1f /
            Mathf.Max(
                1,
                pixelsPerUnit
            );


        return
            Mathf.Round(
                value /
                unitsPerPixel
            ) *
            unitsPerPixel;
    }


    // ==================================================
    // VALIDATE
    // ==================================================

    private void OnValidate()
    {
        /*
         * Your current project assets use 128 PPU.
         */
        pixelsPerUnit =
            128;


        if (layers == null)
            return;


        foreach (ParallaxLayer layer in layers)
        {
            if (layer == null)
                continue;


            layer.horizontalStrength =
                Mathf.Clamp01(
                    layer.horizontalStrength
                );


            layer.verticalStrength =
                Mathf.Clamp01(
                    layer.verticalStrength
                );
        }
    }
}