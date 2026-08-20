using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
public class BackGroundParallax : MonoBehaviour
{
    // ==================================================
    // CAMERA
    // ==================================================

    [Header("Camera")]

    [Tooltip(
        "Normally assign this level's Main Camera."
    )]
    [SerializeField]
    private Camera mainCamera;


    [Tooltip(
        "If Main Camera is not assigned, try to find it automatically."
    )]
    [SerializeField]
    private bool autoFindCamera = true;


    // ==================================================
    // PIXEL ART
    // ==================================================

    [Header("Pixel Art")]

    [Tooltip(
        "Pixels Per Unit used only when manual pixel snapping is enabled."
    )]
    [SerializeField, Min(1)]
    private int pixelsPerUnit = 128;


    // ==================================================
    // PARALLAX LAYERS
    // ==================================================

    [Header("Parallax Layers")]

    [SerializeField]
    private ParallaxLayer[] layers;


    [Tooltip(
        "If Layers is empty, automatically create the layer list " +
        "from this GameObject's children."
    )]
    [SerializeField]
    private bool autoBuildLayersFromChildren = true;


    // ==================================================
    // RELOAD / CINEMATIC SAFETY
    // ==================================================

    [Header("Reload Safety")]

    [Tooltip(
        "Start with parallax paused until the Gameplay Camera " +
        "has finished moving into position. " +
        "Recommended for levels with IntroCamera."
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

    private bool parallaxPaused;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        /*
         * Cinemachine may move the physical Main Camera
         * significantly during the first frames after
         * loading or reloading a level.
         *
         * Starting paused prevents that movement from
         * being interpreted as normal gameplay parallax.
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
         * If this scene has no LevelCameraDirector,
         * there is nobody else to resume the parallax.
         *
         * In that case start it immediately.
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


        ResolveCamera();


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


        EnsureLayersConfigured();


        if (layers == null ||
            layers.Length == 0)
        {
            Debug.LogWarning(
                "BackGroundParallax: No parallax layers are configured.",
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
            "BackGroundParallax initialized." +
            "\nScene: " +
            gameObject.scene.name +
            "\nCamera: " +
            mainCamera.name +
            "\nLayers: " +
            layers.Length,
            this
        );
    }


    // ==================================================
    // RESOLVE CAMERA
    // ==================================================

    private void ResolveCamera()
    {
        if (mainCamera != null)
            return;


        if (!autoFindCamera)
            return;


        mainCamera =
            FindMainCameraInThisScene();
    }


    // ==================================================
    // AUTO BUILD LAYERS
    // ==================================================

    private void EnsureLayersConfigured()
    {
        /*
         * Never overwrite a valid Inspector setup.
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


        ParallaxLayer[] detectedLayers =
            new ParallaxLayer[
                childCount
            ];


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


            detectedLayers[validCount] =
                ParallaxLayerDefaults.Create(
                    child
                );


            validCount++;
        }


        if (validCount <= 0)
            return;


        /*
         * If for some reason a child was skipped,
         * resize the array so it contains only
         * valid entries.
         */
        if (validCount !=
            detectedLayers.Length)
        {
            Array.Resize(
                ref detectedLayers,
                validCount
            );
        }


        layers =
            detectedLayers;


        Debug.Log(
            "BackGroundParallax: Auto-built " +
            layers.Length +
            " layer(s) from Background children.",
            this
        );
    }


    // ==================================================
    // LATE UPDATE
    // ==================================================

    private void LateUpdate()
    {
        // ----------------------------------------------
        // INITIALIZATION
        // ----------------------------------------------

        if (!initialized)
        {
            Initialize();


            if (!initialized)
                return;
        }


        // ----------------------------------------------
        // CAMERA VALIDATION
        // ----------------------------------------------

        if (mainCamera == null ||
            mainCamera.gameObject.scene !=
            gameObject.scene)
        {
            TryRebindCamera();
        }


        if (mainCamera == null)
            return;


        // ----------------------------------------------
        // PAUSED
        // ----------------------------------------------

        if (parallaxPaused)
            return;


        // ----------------------------------------------
        // CAMERA MOVEMENT
        // ----------------------------------------------

        Vector3 cameraDelta =
            mainCamera.transform.position -
            cameraStartPosition;


        // ----------------------------------------------
        // UPDATE LAYERS
        // ----------------------------------------------

        UpdateLayers(
            cameraDelta
        );
    }


    // ==================================================
    // UPDATE LAYERS
    // ==================================================

    private void UpdateLayers(
        Vector3 cameraDelta)
    {
        if (layers == null ||
            layerStartPositions == null)
        {
            return;
        }


        int count =
            Mathf.Min(
                layers.Length,
                layerStartPositions.Length
            );


        for (int i = 0;
             i < count;
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
                CalculateLayerPosition(
                    layer,
                    startPosition,
                    cameraDelta
                );


            layer.target.position =
                newPosition;
        }
    }


    // ==================================================
    // CALCULATE LAYER POSITION
    // ==================================================

    private Vector3 CalculateLayerPosition(
        ParallaxLayer layer,
        Vector3 startPosition,
        Vector3 cameraDelta)
    {
        Vector3 newPosition =
            startPosition;


        // ----------------------------------------------
        // HORIZONTAL
        // ----------------------------------------------

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


        // ----------------------------------------------
        // VERTICAL
        // ----------------------------------------------

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


        // ----------------------------------------------
        // KEEP ORIGINAL Z
        // ----------------------------------------------

        newPosition.z =
            startPosition.z;


        // ----------------------------------------------
        // OPTIONAL PIXEL SNAP
        // ----------------------------------------------

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


        return newPosition;
    }


    // ==================================================
    // PAUSE PARALLAX
    // ==================================================

    public void PauseParallax()
    {
        /*
         * The LevelCameraDirector can call this during
         * IntroCamera / cinematic movement.
         */
        if (!initialized)
        {
            Initialize();
        }


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
         * Treat the current camera position and current
         * background positions as the new starting origin.
         *
         * This prevents IntroCamera movement from affecting
         * the gameplay parallax.
         */
        CaptureCurrentPositionsAsNewOrigin();


        parallaxPaused =
            false;


        Debug.Log(
            "BackGroundParallax resumed." +
            "\nScene: " +
            gameObject.scene.name,
            this
        );
    }


    // ==================================================
    // RESET ORIGIN
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
        if (mainCamera == null ||
            layers == null)
        {
            return;
        }


        cameraStartPosition =
            mainCamera.transform.position;


        if (layerStartPositions == null ||
            layerStartPositions.Length !=
            layers.Length)
        {
            layerStartPositions =
                new Vector3[
                    layers.Length
                ];
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


        mainCamera =
            newCamera;


        /*
         * Same camera as before.
         */
        if (newCamera ==
            currentlyBoundCamera)
        {
            return;
        }


        currentlyBoundCamera =
            newCamera;


        /*
         * A newly-bound camera must become
         * the new parallax origin.
         */
        CaptureCurrentPositionsAsNewOrigin();


        Debug.Log(
            "BackGroundParallax rebound to Main Camera: " +
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
                root.GetComponentsInChildren
                    <Camera>(
                        true
                    );


            foreach (Camera camera in cameras)
            {
                if (camera == null)
                    continue;


                /*
                 * Keep the first camera as a fallback.
                 */
                if (firstCamera == null)
                {
                    firstCamera =
                        camera;
                }


                /*
                 * Prefer a camera tagged MainCamera.
                 */
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
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        pixelsPerUnit =
            Mathf.Max(
                1,
                pixelsPerUnit
            );


        if (layers == null)
            return;


        foreach (ParallaxLayer layer in layers)
        {
            ParallaxLayerDefaults.Clamp(
                layer
            );
        }
    }
}