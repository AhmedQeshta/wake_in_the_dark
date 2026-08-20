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


    [Tooltip(
        "If the scene has no serialized layer list, automatically build it " +
        "from the Background object's children. This fixes old Level_05 " +
        "scene data after script changes/reloads."
    )]
    [SerializeField]
    private bool autoBuildLayersFromChildren = true;


    [Header("Reload Safety")]

    [Tooltip(
        "Keep parallax frozen from Awake until LevelCameraDirector says " +
        "the GameplayCamera has finished moving into place. " +
        "Keep this ON for Level_04 and Level_05."
    )]
    [SerializeField]
    private bool startPausedUntilGameplayCameraReady = true;


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
    // AWAKE
    // ==================================================

    private void Awake()
    {
        /*
         * CRITICAL RELOAD FIX
         * -------------------
         *
         * On Reset Level, Cinemachine can move the physical
         * Main Camera a very large distance during the first
         * LateUpdate of the newly-loaded scene.
         *
         * BackGroundParallax runs late (execution order 1000),
         * so without this guard it sees that Cinemachine jump
         * as normal gameplay movement and moves every background
         * plane far off-screen BEFORE LevelCameraDirector gets
         * a chance to pause it.
         *
         * Start paused immediately, before any LateUpdate.
         * LevelCameraDirector will call
         * ResumeParallaxFromCurrentCamera() after GameplayCamera
         * has settled.
         */
        parallaxPaused =
            startPausedUntilGameplayCameraReady;
    }


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        Initialize();


        /*
         * Normal Level_04 / Level_05:
         * LevelCameraDirector exists and will resume parallax
         * when GameplayCamera is ready.
         *
         * Fallback:
         * if this script is used in a scene with no director,
         * allow it to work normally.
         */
        if (parallaxPaused &&
            !HasCameraDirectorInThisScene())
        {
            ResumeParallaxFromCurrentCamera();
        }
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


        /*
         * Level_05 was saved with the old field:
         *
         *     parallaxSpeed: []
         *
         * so its new 'layers' array can deserialize empty.
         * Rebuild it from Background children when needed.
         */
        EnsureLayersConfigured();


        if (layers == null ||
            layers.Length == 0)
        {
            Debug.LogWarning(
                "BackGroundParallax: No layers configured and no " +
                "background children could be auto-detected.",
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
    // AUTO BUILD LAYERS
    // ==================================================

    private void EnsureLayersConfigured()
    {
        /*
         * Never replace a valid Inspector setup.
         *
         * Level_04 already has its 5 serialized layers,
         * so this method leaves Level_04 untouched.
         */
        if (layers != null &&
            layers.Length > 0)
        {
            return;
        }


        if (!autoBuildLayersFromChildren)
            return;


        int childCount =
            transform.childCount;


        if (childCount <= 0)
            return;


        ParallaxLayer[] detected =
            new ParallaxLayer[childCount];


        int validCount =
            0;


        for (int i = 0;
             i < childCount;
             i++)
        {
            Transform child =
                transform.GetChild(i);


            if (child == null)
                continue;


            ParallaxLayer layer =
                CreateDefaultLayer(
                    child
                );


            detected[validCount] =
                layer;


            validCount++;
        }


        if (validCount <= 0)
            return;


        if (validCount !=
            detected.Length)
        {
            Array.Resize(
                ref detected,
                validCount
            );
        }


        layers =
            detected;


        Debug.Log(
            "BackGroundParallax: Auto-built " +
            layers.Length +
            " layer(s) from Background children in " +
            gameObject.scene.name +
            ".",
            this
        );
    }


    // ==================================================
    // DEFAULT LAYER PROFILE
    // ==================================================

    private ParallaxLayer CreateDefaultLayer(
        Transform child)
    {
        ParallaxLayer layer =
            new ParallaxLayer();


        layer.target =
            child;


        /*
         * These values match the working Level_04 setup.
         *
         * Plane_1 / Plane_1_other:
         * very far background.
         *
         * Plane_4:
         * closest / strongest parallax.
         */
        switch (child.name)
        {
            case "Plane_1_other":
            case "Plane_1":

                layer.horizontalStrength =
                    0.005f;

                layer.verticalStrength =
                    0.002f;

                break;


            case "Plane_2":

                layer.horizontalStrength =
                    0.08f;

                layer.verticalStrength =
                    0.025f;

                break;


            case "Plane_3":

                layer.horizontalStrength =
                    0.14f;

                layer.verticalStrength =
                    0.04f;

                break;


            case "Plane_4":

                layer.horizontalStrength =
                    0.22f;

                layer.verticalStrength =
                    0.06f;

                break;


            default:

                /*
                 * Safe fallback for any extra background child.
                 */
                layer.horizontalStrength =
                    0.1f;

                layer.verticalStrength =
                    0.03f;

                break;
        }


        layer.useVerticalParallax =
            true;


        /*
         * Pixel Perfect Camera already handles snapping.
         */
        layer.snapTransformToPixelGrid =
            false;


        return layer;
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
    // CAMERA DIRECTOR CHECK
    // ==================================================

    private bool HasCameraDirectorInThisScene()
    {
        Scene scene =
            gameObject.scene;


        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            return false;
        }


        GameObject[] roots =
            scene.GetRootGameObjects();


        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;


            LevelCameraDirector director =
                root.GetComponentInChildren
                    <LevelCameraDirector>(
                        true
                    );


            if (director != null)
            {
                return true;
            }
        }


        return false;
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