using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class LevelCameraDirector : MonoBehaviour
{
    // ==================================================
    // TIMELINE CONFIGURATION
    // ==================================================

    [Header("Timeline Sequence")]
    [Tooltip("PlayableDirector configured with the intro Timeline asset.")]
    [SerializeField] private PlayableDirector introPlayableDirector;

    [SerializeField] private bool playOnLevelEnter = true;

    [SerializeField] private bool playOnReload = false;
    public bool PlayOnReload => playOnReload;

    [SerializeField] private bool freezePlayerDuringIntro = true;

    // ==================================================
    // CAMERA REFERENCES
    // ==================================================

    [Header("Camera References")]
    [Tooltip("The final camera the player uses after the intro.")]
    [SerializeField] private CinemachineCamera gameplayCamera;

    [Tooltip("Used for mid-gameplay focuses (e.g., pulling a lever). Assign DoorCamera here.")]
    [SerializeField] private CinemachineCamera focusCamera;

    [SerializeField] private PlayerCameraBinder playerCameraBinder;

    // ==================================================
    // PRIORITIES
    // ==================================================

    [Header("Camera Priorities")]
    [SerializeField] private int inactivePriority = 0;
    [SerializeField] private int gameplayPriority = 10;
    [SerializeField] private int focusPriority = 60;

    // ==================================================
    // MID-GAMEPLAY FOCUS (LEVERS/DOORS)
    // ==================================================

    [Header("Mid-Gameplay Focus")]
    [SerializeField, Min(0f)] private float defaultFocusDuration = 1.2f;
    [SerializeField, Min(0f)] private float focusReturnBlendWait = 0.8f;
    [SerializeField] private bool freezePlayerDuringFocus = true;

    // ==================================================
    // INTERNAL STATE
    // ==================================================

    private bool introPlaying;
    private bool focusPlaying;
    private bool playerFrozen;
    private bool previousMovementEnabled = true;
    private Coroutine focusRoutine;
    private BackGroundParallax backgroundParallax;

    public bool PlayOnLevelEnter => playOnLevelEnter;

    private Vector3 frozenPlayerPosition;
    private RigidbodyType2D previousPlayerBodyType;

    // ==================================================
    // LIFECYCLE
    // ==================================================

    private void Awake()
    {
        ResolveReferences();
        HookTimelineEvents();
    }

    private IEnumerator Start()
    {
        yield return null;

        if (LevelLoader.Instance == null && playOnLevelEnter)
        {
            PlayIntroTimeline();
        }
    }

    private void OnDestroy()
    {
        UnhookTimelineEvents();
    }

    // ==================================================
    // TIMELINE INTEGRATION
    // ==================================================

    private void HookTimelineEvents()
    {
        if (introPlayableDirector != null)
        {
            introPlayableDirector.stopped += OnTimelineFinished;
        }
    }

    private void UnhookTimelineEvents()
    {
        if (introPlayableDirector != null)
        {
            introPlayableDirector.stopped -= OnTimelineFinished;
        }
    }

    public void PlayIntroTimeline()
    {
        ResolveReferences();

        if (introPlayableDirector == null || introPlayableDirector.playableAsset == null)
        {
            SkipToGameplayImmediate();
            return;
        }

        BindGameplayTarget();

        // 1. FORCE THE FREEZE INSTANTLY
        // We do this here instead of waiting for the event so the player 
        // doesn't fall or move for a single frame before the camera starts.
        introPlaying = true;
        PauseBackgroundParallax();
        FreezePlayer(freezePlayerDuringIntro);

        // 2. Start the Timeline
        introPlayableDirector.time = 0;
        introPlayableDirector.Play();
    }


    private void OnTimelineFinished(PlayableDirector director)
    {
        SetGameplayLive();
        ResumeBackgroundParallax();
        RestorePlayer();
        introPlaying = false;
    }

    // ==================================================
    // IMMEDIATE TRANSITION & RELOAD
    // ==================================================

    public void SkipToGameplayImmediate()
    {
        ResolveReferences();

        if (introPlayableDirector != null && introPlayableDirector.state == PlayState.Playing)
        {
            introPlayableDirector.Stop();
        }

        BindGameplayTarget();
        SetGameplayLive();
        ResumeBackgroundParallax();
        RestorePlayer();

        introPlaying = false;
    }

    private void SetGameplayLive()
    {
        ResolveReferences();

        if (gameplayCamera == null) return;

        if (!gameplayCamera.gameObject.activeSelf)
        {
            gameplayCamera.gameObject.SetActive(true);
        }

        SetPriority(focusCamera, inactivePriority);
        SetPriority(gameplayCamera, gameplayPriority);
        gameplayCamera.Prioritize();
    }

    // ==================================================
    // MID-LEVEL FOCUS ROUTINE
    // ==================================================

    public void PlayFocus(float duration = -1f)
    {
        if (introPlaying || focusPlaying) return;

        float targetDuration = duration > 0f ? duration : defaultFocusDuration;
        focusRoutine = StartCoroutine(FocusRoutine(targetDuration));
    }

    private IEnumerator FocusRoutine(float duration)
    {
        focusPlaying = true;
        FreezePlayer(freezePlayerDuringFocus);
        PauseBackgroundParallax();

        SetPriority(gameplayCamera, inactivePriority);
        SetPriority(focusCamera, focusPriority);
        if (focusCamera != null) focusCamera.Prioritize();

        yield return new WaitForSecondsRealtime(duration);

        SetGameplayLive();

        if (focusReturnBlendWait > 0f)
        {
            yield return new WaitForSecondsRealtime(focusReturnBlendWait);
        }

        ResumeBackgroundParallax();
        RestorePlayer();

        focusPlaying = false;
        focusRoutine = null;
    }

    // ==================================================
    // HELPER METHODS
    // ==================================================

    private void BindGameplayTarget()
    {
        ResolveReferences();
        if (gameplayCamera == null) return;

        Transform target = GetPlayerTarget();
        if (target != null)
        {
            gameplayCamera.Follow = target;
        }
    }

    private Transform GetPlayerTarget()
    {
        if (playerCameraBinder != null && playerCameraBinder.TrackingTarget != null) return playerCameraBinder.TrackingTarget;
        PlayerMovement movement = FindComponentInMyScene<PlayerMovement>();
        return movement != null ? movement.transform : null;
    }

    private void SetPriority(CinemachineCamera cam, int priority)
    {
        if (cam == null) return;
        PrioritySettings settings = cam.Priority;
        settings.Enabled = true;
        settings.Value = priority;
        cam.Priority = settings;
    }

    private void FreezePlayer(bool shouldFreeze)
    {
        if (!shouldFreeze || playerFrozen) return;

        PlayerMovement movement = FindComponentInMyScene<PlayerMovement>();
        if (movement == null) return;

        previousMovementEnabled = movement.enabled;
        movement.enabled = false;
        playerFrozen = true;

        // 1. Lock physics and record the exact position
        Rigidbody2D rb = movement.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            frozenPlayerPosition = movement.transform.position;
            previousPlayerBodyType = rb.bodyType;

            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        // 2. Reset Animator parameters and force the idle state
        Animator animator = movement.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsGrounded", true);
            animator.Play("idle");
        }
    }

    private void RestorePlayer()
    {
        if (!playerFrozen) return;

        PlayerMovement movement = FindComponentInMyScene<PlayerMovement>();
        if (movement != null)
        {
            // 1. Snap back to the exact saved position and restore physics
            Rigidbody2D rb = movement.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                movement.transform.position = frozenPlayerPosition;
                rb.bodyType = previousPlayerBodyType;
            }

            movement.enabled = previousMovementEnabled;
        }

        playerFrozen = false;
    }

    private void PauseBackgroundParallax()
    {
        if (backgroundParallax == null) backgroundParallax = FindComponentInMyScene<BackGroundParallax>();
        if (backgroundParallax != null) backgroundParallax.PauseParallax();
    }

    private void ResumeBackgroundParallax()
    {
        if (backgroundParallax == null) backgroundParallax = FindComponentInMyScene<BackGroundParallax>();
        if (backgroundParallax != null) backgroundParallax.ResumeParallaxFromCurrentCamera();
    }

    private void ResolveReferences()
    {
        if (introPlayableDirector == null) introPlayableDirector = GetComponent<PlayableDirector>();
        if (playerCameraBinder == null) playerCameraBinder = FindComponentInMyScene<PlayerCameraBinder>();
        if (backgroundParallax == null) backgroundParallax = FindComponentInMyScene<BackGroundParallax>();
        if (gameplayCamera == null) gameplayCamera = FindCinemachineCameraByName("GameplayCamera");
        if (focusCamera == null) focusCamera = FindCinemachineCameraByName("DoorCamera");
    }

    private CinemachineCamera FindCinemachineCameraByName(string objectName)
    {
        Scene scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CinemachineCamera[] cams = root.GetComponentsInChildren<CinemachineCamera>(true);
            foreach (CinemachineCamera cam in cams)
            {
                if (string.Equals(cam.gameObject.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return cam;
                }
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

    // ==================================================
    // LEGACY LEVEL LOADER BRIDGES
    // ==================================================

    public void PrepareIntro()
    {
        ResolveReferences();
        BindGameplayTarget();

        // Optional: Pre-freeze the player before the timeline even starts
        FreezePlayer(freezePlayerDuringIntro);
    }

    public IEnumerator PlayIntroRoutine()
    {
        PlayIntroTimeline();

        // Wait here while the Timeline is playing so the LevelLoader doesn't fade in too early
        while (introPlaying)
        {
            yield return null;
        }
    }
}