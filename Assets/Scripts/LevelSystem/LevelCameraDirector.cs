using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class LevelCameraDirector : MonoBehaviour
{
    public enum CameraSequenceMode
    {
        Auto = 0,
        DoorMapGameplay = 1,
        DoorGameplay = 2
    }


    // ==================================================
    // MODE
    // ==================================================

    [Header("Camera Sequence")]

    [Tooltip(
        "Auto: uses Door -> Map -> Gameplay when a LongShot + MapViewBounds exist, " +
        "otherwise Door -> Gameplay."
    )]
    [SerializeField]
    private CameraSequenceMode sequenceMode =
        CameraSequenceMode.Auto;


    // ==================================================
    // CAMERA REFERENCES
    // ==================================================

    [Header("Camera References")]

    [Tooltip(
        "Used by Levels 1-3. Optional for Levels 4-5."
    )]
    [SerializeField]
    private LevelCameraSetup cameraSetup;


    [Tooltip(
        "Door / intro camera. Existing Level 4/5 references are kept."
    )]
    [SerializeField]
    private CinemachineCamera introCamera;


    [Tooltip(
        "Gameplay camera. Can be found automatically from PlayerCameraBinder."
    )]
    [SerializeField]
    private CinemachineCamera gameplayCamera;


    [Tooltip(
        "Legacy / Level 4-5 camera binder. Kept so existing scene references continue working."
    )]
    [SerializeField]
    private PlayerCameraBinder playerCameraBinder;


    // ==================================================
    // PRIORITIES
    // ==================================================

    [Header("Camera Priorities")]

    [SerializeField]
    private int inactivePriority = 0;

    [SerializeField]
    private int gameplayPriority = 10;

    [SerializeField]
    private int longShotPriority = 30;

    [SerializeField]
    private int introPriority = 60;


    // ==================================================
    // LEVEL INTRO
    // ==================================================

    [Header("Level Intro")]

    [SerializeField]
    private bool playOnLevelEnter = true;

    [SerializeField]
    private bool playOnReload = false;


    [Tooltip(
        "How long the camera stays on the door."
    )]
    [FormerlySerializedAs("introHoldDuration")]
    [SerializeField, Min(0f)]
    private float doorHoldDuration = 1.2f;


    [Tooltip(
        "Levels 1-3 only: how long the whole-map camera stays visible."
    )]
    [SerializeField, Min(0f)]
    private float wholeMapHoldDuration = 1.5f;


    [Tooltip(
        "How long to wait after switching to GameplayCamera before player control returns. " +
        "Useful while Cinemachine blends from the door to the player."
    )]
    [FormerlySerializedAs("blendDuration")]
    [SerializeField, Min(0f)]
    private float gameplayBlendWait = 0.6f;


    // ==================================================
    // PLAYER
    // ==================================================

    [Header("Player")]

    [SerializeField]
    private bool freezePlayerDuringIntro = true;


    // ==================================================
    // LEVER / DOOR FOCUS
    // ==================================================

    [Header("Lever Door Focus")]

    [SerializeField, Min(0f)]
    private float defaultDoorFocusDuration = 3f;

    [SerializeField, Min(0f)]
    private float doorReturnBlendWait = 0.8f;

    [SerializeField]
    private bool freezePlayerDuringDoorFocus = true;


    // ==================================================
    // STATE
    // ==================================================

    private bool introPrepared;

    private bool introPlaying;

    private bool doorFocusPlaying;

    private bool previousMovementEnabled =
        true;

    private bool playerFrozen;

    private Coroutine doorFocusRoutine;


    private BackGroundParallax backgroundParallax;


    // ==================================================
    // PUBLIC
    // ==================================================

    public bool PlayOnLevelEnter =>
        playOnLevelEnter;

    public bool PlayOnReload =>
        playOnReload;

    public CameraSequenceMode EffectiveMode =>
        ResolveEffectiveMode();


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        /*
         * IMPORTANT:
         *
         * Do not call LevelCameraSetup.BindScene() here.
         * Bootstrap loads level scenes additively.
         * LevelLoader waits until the scene is ready.
         */
        ResolveReferences();
    }


    // ==================================================
    // START - DIRECT SCENE TEST FALLBACK
    // ==================================================

    private IEnumerator Start()
    {
        /*
         * When testing Level_04 or Level_05 directly
         * without Bootstrap, still play the intro.
         *
         * When Bootstrap is present, LevelLoader owns
         * the intro timing, so we do nothing here.
         */
        yield return null;


        if (LevelLoader.Instance == null &&
            playOnLevelEnter)
        {
            PrepareIntro();

            yield return StartCoroutine(
                PlayIntroRoutine()
            );
        }
    }


    // ==================================================
    // PREPARE INTRO
    // ==================================================

    public void PrepareIntro()
    {
        ResolveReferences();


        CameraSequenceMode mode =
            ResolveEffectiveMode();


        // --------------------------------------------------
        // LEVELS 1-3
        // --------------------------------------------------

        if (cameraSetup != null)
        {
            cameraSetup.BindScene();
        }


        // --------------------------------------------------
        // VALIDATE
        // --------------------------------------------------

        if (!CanPlayIntro(mode))
        {
            Debug.LogWarning(
                "LevelCameraDirector: Required cameras are missing. " +
                "Skipping intro in " +
                gameObject.scene.name +
                ". Mode: " +
                mode,
                this
            );

            SkipToGameplayImmediate();

            return;
        }


        // --------------------------------------------------
        // LEVELS 1-3: PREPARE WHOLE MAP
        // --------------------------------------------------

        if (mode ==
            CameraSequenceMode.DoorMapGameplay)
        {
            cameraSetup.ResetLongShotOverview();

            cameraSetup.FitGameplayToMap();
        }


        // --------------------------------------------------
        // LEVELS 4-5:
        // DO NOT TOUCH GAMEPLAY LENS.
        //
        // Pixel Perfect Camera +
        // Cinemachine Pixel Perfect remain in control.
        // --------------------------------------------------

        BindGameplayTarget();


        /*
         * Levels 4-5 use parallax.
         *
         * Do not let the cinematic IntroCamera movement
         * count as gameplay parallax movement.
         */
        PauseBackgroundParallax();


        FreezePlayer(
            freezePlayerDuringIntro
        );


        SetIntroLive();


        introPrepared =
            true;

        introPlaying =
            false;


        Debug.Log(
            "Camera intro prepared. Scene: " +
            gameObject.scene.name +
            " | Mode: " +
            mode,
            this
        );
    }


    // ==================================================
    // PLAY INTRO
    // ==================================================

    public IEnumerator PlayIntroRoutine()
    {
        if (introPlaying)
            yield break;


        if (!introPrepared)
        {
            PrepareIntro();
        }


        CameraSequenceMode mode =
            ResolveEffectiveMode();


        if (!CanPlayIntro(mode))
        {
            SkipToGameplayImmediate();

            yield break;
        }


        introPlaying =
            true;


        // ==================================================
        // STAGE 1
        // DOOR
        // ==================================================

        Debug.Log(
            "Camera Stage 1: Door",
            this
        );


        SetIntroLive();


        if (doorHoldDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    doorHoldDuration
                );
        }


        // ==================================================
        // STAGE 2 - LEVELS 1-3 ONLY
        // WHOLE MAP
        // ==================================================

        if (mode ==
            CameraSequenceMode.DoorMapGameplay)
        {
            Debug.Log(
                "Camera Stage 2: Whole Map",
                this
            );


            SetLongShotLive();


            if (wholeMapHoldDuration > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        wholeMapHoldDuration
                    );
            }
        }


        // ==================================================
        // FINAL STAGE
        // GAMEPLAY CAMERA + PLAYER FOLLOW
        // ==================================================

        Debug.Log(
            "Camera Final Stage: GameplayCamera -> Player",
            this
        );


        SetGameplayLive();


        /*
         * Keep the player frozen while Cinemachine
         * finishes the door/map -> player blend.
         */
        if (gameplayBlendWait > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    gameplayBlendWait
                );
        }


        /*
         * Gameplay camera is now at the Player.
         * Make this the new parallax origin, then enable
         * parallax movement.
         */
        ResumeBackgroundParallax();


        RestorePlayer();


        introPrepared =
            false;

        introPlaying =
            false;
    }


    // ==================================================
    // INTRO LIVE
    // ==================================================

    private void SetIntroLive()
    {
        ResolveReferences();


        SetPriority(
            gameplayCamera,
            inactivePriority
        );


        if (cameraSetup != null)
        {
            SetPriority(
                cameraSetup.LongShotCamera,
                inactivePriority
            );
        }


        SetPriority(
            introCamera,
            introPriority
        );


        if (introCamera != null)
        {
            introCamera.Prioritize();
        }
    }


    // ==================================================
    // LONG SHOT LIVE
    // LEVELS 1-3 ONLY
    // ==================================================

    private void SetLongShotLive()
    {
        if (cameraSetup == null ||
            cameraSetup.LongShotCamera == null)
        {
            SetGameplayLive();

            return;
        }


        cameraSetup.DisableLongShotFollow();


        SetPriority(
            introCamera,
            inactivePriority
        );


        SetPriority(
            gameplayCamera,
            inactivePriority
        );


        SetPriority(
            cameraSetup.LongShotCamera,
            longShotPriority
        );


        cameraSetup
            .LongShotCamera
            .Prioritize();
    }


    // ==================================================
    // GAMEPLAY LIVE
    // ==================================================

    private void SetGameplayLive()
    {
        ResolveReferences();


        if (gameplayCamera == null)
        {
            Debug.LogError(
                "LevelCameraDirector: GameplayCamera is missing in " +
                gameObject.scene.name +
                ".",
                this
            );

            return;
        }


        if (cameraSetup != null)
        {
            cameraSetup.DisableLongShotFollow();
        }


        BindGameplayTarget();


        SetPriority(
            introCamera,
            inactivePriority
        );


        if (cameraSetup != null)
        {
            SetPriority(
                cameraSetup.LongShotCamera,
                inactivePriority
            );
        }


        SetPriority(
            gameplayCamera,
            gameplayPriority
        );


        gameplayCamera.Prioritize();
    }


    // ==================================================
    // DOOR FOCUS
    // ==================================================

    public void PlayDoorFocus()
    {
        PlayDoorFocus(
            defaultDoorFocusDuration
        );
    }


    public void PlayDoorFocus(
        float duration)
    {
        ResolveReferences();


        if (!CanFocusDoor())
            return;


        if (introPlaying)
            return;


        if (doorFocusPlaying ||
            doorFocusRoutine != null)
        {
            return;
        }


        doorFocusRoutine =
            StartCoroutine(
                DoorFocusRoutine(
                    Mathf.Max(
                        0f,
                        duration
                    )
                )
            );
    }


    // ==================================================
    // DOOR FOCUS ROUTINE
    // ==================================================

    private IEnumerator DoorFocusRoutine(
        float duration)
    {
        doorFocusPlaying =
            true;


        FreezePlayer(
            freezePlayerDuringDoorFocus
        );


        /*
         * Freeze the background layers while the camera
         * leaves the Player and looks at the door.
         */
        PauseBackgroundParallax();


        // --------------------------------------------------
        // GAMEPLAY -> DOOR
        // --------------------------------------------------

        SetPriority(
            gameplayCamera,
            gameplayPriority
        );


        if (cameraSetup != null)
        {
            SetPriority(
                cameraSetup.LongShotCamera,
                inactivePriority
            );
        }


        SetPriority(
            introCamera,
            introPriority
        );


        introCamera.Prioritize();


        if (duration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    duration
                );
        }


        // --------------------------------------------------
        // DOOR -> GAMEPLAY
        // --------------------------------------------------

        SetGameplayLive();


        if (doorReturnBlendWait > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    doorReturnBlendWait
                );
        }


        ResumeBackgroundParallax();


        RestorePlayer();


        doorFocusPlaying =
            false;

        doorFocusRoutine =
            null;
    }


    // ==================================================
    // SKIP INTRO
    // ==================================================

    public void SkipToGameplayImmediate()
    {
        ResolveReferences();


        CameraSequenceMode mode =
            ResolveEffectiveMode();


        /*
         * Levels 1-3 use LevelCameraSetup.
         */
        if (cameraSetup != null)
        {
            cameraSetup.BindScene();


            if (mode ==
                CameraSequenceMode.DoorMapGameplay)
            {
                cameraSetup.FitGameplayToMap();
            }
        }


        /*
         * Levels 4-5 intentionally do NOT call
         * FitGameplayToMap().
         *
         * Their Pixel Perfect Camera owns the gameplay
         * camera size.
         */
        BindGameplayTarget();


        SetGameplayLive();


        ResumeBackgroundParallax();


        RestorePlayer();


        introPrepared =
            false;

        introPlaying =
            false;
    }


    // ==================================================
    // EFFECTIVE MODE
    // ==================================================

    private CameraSequenceMode ResolveEffectiveMode()
    {
        if (sequenceMode !=
            CameraSequenceMode.Auto)
        {
            return sequenceMode;
        }


        /*
         * If this level has the complete wide-map setup,
         * use the 3-stage sequence.
         *
         * Otherwise use the 2-stage pixel-perfect sequence.
         */
        if (cameraSetup != null &&
            cameraSetup.LongShotCamera != null &&
            cameraSetup.MapViewBounds != null)
        {
            return
                CameraSequenceMode
                    .DoorMapGameplay;
        }


        return
            CameraSequenceMode
                .DoorGameplay;
    }


    // ==================================================
    // BIND GAMEPLAY TARGET
    // ==================================================

    private void BindGameplayTarget()
    {
        ResolveReferences();


        if (gameplayCamera == null)
            return;


        Transform target =
            GetPlayerTarget();


        if (target != null)
        {
            gameplayCamera.Follow =
                target;
        }
    }


    // ==================================================
    // PLAYER TARGET
    // ==================================================

    private Transform GetPlayerTarget()
    {
        if (cameraSetup != null &&
            cameraSetup.PlayerTarget != null)
        {
            return
                cameraSetup.PlayerTarget;
        }


        if (playerCameraBinder != null &&
            playerCameraBinder.TrackingTarget != null)
        {
            return
                playerCameraBinder.TrackingTarget;
        }


        PlayerMovement movement =
            GetPlayerMovement();


        if (movement != null)
        {
            return
                movement.transform;
        }


        return null;
    }


    // ==================================================
    // PLAYER MOVEMENT
    // ==================================================

    private PlayerMovement GetPlayerMovement()
    {
        if (cameraSetup != null &&
            cameraSetup.PlayerMovement != null)
        {
            return
                cameraSetup.PlayerMovement;
        }


        if (playerCameraBinder != null &&
            playerCameraBinder.PlayerMovement != null)
        {
            return
                playerCameraBinder.PlayerMovement;
        }


        return
            FindComponentInMyScene
                <PlayerMovement>();
    }


    // ==================================================
    // PRIORITY
    // ==================================================

    private void SetPriority(
        CinemachineCamera camera,
        int priority)
    {
        if (camera == null)
            return;


        camera.Priority =
            priority;
    }


    // ==================================================
    // FREEZE PLAYER
    // ==================================================

    private void FreezePlayer(
        bool shouldFreeze)
    {
        if (!shouldFreeze ||
            playerFrozen)
        {
            return;
        }


        PlayerMovement movement =
            GetPlayerMovement();


        if (movement == null)
            return;


        previousMovementEnabled =
            movement.enabled;


        movement.enabled =
            false;


        playerFrozen =
            true;


        Rigidbody2D rb =
            movement.GetComponent<Rigidbody2D>();


        if (rb != null)
        {
            rb.linearVelocity =
                new Vector2(
                    0f,
                    rb.linearVelocity.y
                );
        }
    }


    // ==================================================
    // RESTORE PLAYER
    // ==================================================

    private void RestorePlayer()
    {
        if (!playerFrozen)
            return;


        PlayerMovement movement =
            GetPlayerMovement();


        if (movement != null)
        {
            movement.enabled =
                previousMovementEnabled;
        }


        playerFrozen =
            false;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private bool CanPlayIntro(
        CameraSequenceMode mode)
    {
        if (introCamera == null ||
            gameplayCamera == null)
        {
            return false;
        }


        if (mode ==
            CameraSequenceMode.DoorMapGameplay)
        {
            return
                cameraSetup != null &&
                cameraSetup.LongShotCamera != null &&
                cameraSetup.MapViewBounds != null;
        }


        return true;
    }


    private bool CanFocusDoor()
    {
        return
            introCamera != null &&
            gameplayCamera != null;
    }


    // ==================================================
    // REFERENCES
    // ==================================================

    private void ResolveReferences()
    {
        // --------------------------------------------------
        // CAMERA SETUP - LEVELS 1-3
        // --------------------------------------------------

        if (cameraSetup == null)
        {
            cameraSetup =
                FindComponentInMyScene
                    <LevelCameraSetup>();
        }


        // --------------------------------------------------
        // PLAYER CAMERA BINDER - LEVELS 4-5
        // --------------------------------------------------

        if (playerCameraBinder == null)
        {
            playerCameraBinder =
                FindComponentInMyScene
                    <PlayerCameraBinder>();
        }


        // --------------------------------------------------
        // PARALLAX - LEVELS 4-5
        // --------------------------------------------------

        if (backgroundParallax == null)
        {
            backgroundParallax =
                FindComponentInMyScene
                    <BackGroundParallax>();
        }


        // --------------------------------------------------
        // INTRO CAMERA
        // --------------------------------------------------

        if (introCamera == null)
        {
            if (cameraSetup != null &&
                cameraSetup.IntroCamera != null)
            {
                introCamera =
                    cameraSetup.IntroCamera;
            }
            else
            {
                introCamera =
                    FindCinemachineCameraByName(
                        "IntroCamera"
                    );
            }
        }


        // --------------------------------------------------
        // GAMEPLAY CAMERA
        // --------------------------------------------------

        if (gameplayCamera == null)
        {
            if (cameraSetup != null &&
                cameraSetup.GameplayCamera != null)
            {
                gameplayCamera =
                    cameraSetup.GameplayCamera;
            }
            else if (playerCameraBinder != null &&
                     playerCameraBinder.GameplayCamera != null)
            {
                gameplayCamera =
                    playerCameraBinder.GameplayCamera;
            }
            else
            {
                gameplayCamera =
                    FindCinemachineCameraByName(
                        "GameplayCamera"
                    );
            }
        }
    }


    // ==================================================
    // FIND CINEMACHINE CAMERA BY NAME
    // ==================================================

    private CinemachineCamera
        FindCinemachineCameraByName(
            string objectName)
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


            CinemachineCamera[] cameras =
                root.GetComponentsInChildren
                    <CinemachineCamera>(
                        true
                    );


            foreach (
                CinemachineCamera camera
                in cameras
            )
            {
                if (camera == null)
                    continue;


                if (string.Equals(
                        camera.gameObject.name,
                        objectName,
                        System.StringComparison
                            .OrdinalIgnoreCase))
                {
                    return camera;
                }
            }
        }


        return null;
    }


    // ==================================================
    // FIND COMPONENT IN THIS LEVEL
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
    // PARALLAX CONTROL
    // ==================================================

    private void PauseBackgroundParallax()
    {
        ResolveReferences();


        if (backgroundParallax == null)
            return;


        backgroundParallax.PauseParallax();
    }


    private void ResumeBackgroundParallax()
    {
        ResolveReferences();


        if (backgroundParallax == null)
            return;


        backgroundParallax
            .ResumeParallaxFromCurrentCamera();
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDisable()
    {
        if (doorFocusRoutine != null)
        {
            StopCoroutine(
                doorFocusRoutine
            );


            doorFocusRoutine =
                null;
        }


        doorFocusPlaying =
            false;


        RestorePlayer();
    }


    // ==================================================
    // VALIDATE
    // ==================================================

    private void OnValidate()
    {
        doorHoldDuration =
            Mathf.Max(
                0f,
                doorHoldDuration
            );


        wholeMapHoldDuration =
            Mathf.Max(
                0f,
                wholeMapHoldDuration
            );


        gameplayBlendWait =
            Mathf.Max(
                0f,
                gameplayBlendWait
            );


        defaultDoorFocusDuration =
            Mathf.Max(
                0f,
                defaultDoorFocusDuration
            );


        doorReturnBlendWait =
            Mathf.Max(
                0f,
                doorReturnBlendWait
            );
    }
}