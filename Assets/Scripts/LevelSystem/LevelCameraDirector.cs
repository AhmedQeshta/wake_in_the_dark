using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class LevelCameraDirector : MonoBehaviour
{
    [Header("Timeline Sequence")]
    [SerializeField] private PlayableDirector introPlayableDirector;
    [SerializeField] private bool playOnLevelEnter = true;
    [SerializeField] private bool playOnReload = false;
    [SerializeField] private bool freezePlayerDuringIntro = true;

    public bool PlayOnLevelEnter => playOnLevelEnter;
    public bool PlayOnReload => playOnReload;

    [Header("Camera Setup")]
    [SerializeField] private LevelCameraSetup cameraSetup;

    [Header("Mid-Gameplay Focus")]
    [SerializeField] private CinemachineCamera focusCamera;
    [SerializeField] private int inactivePriority = 0;
    [SerializeField] private int gameplayPriority = 10;
    [SerializeField] private int focusPriority = 60;
    [SerializeField, Min(0f)] private float defaultFocusDuration = 1.2f;
    [SerializeField, Min(0f)] private float focusReturnBlendWait = 0.8f;
    [SerializeField] private bool freezePlayerDuringFocus = true;

    private bool introPlaying;
    private bool focusPlaying;
    private bool playerFrozen;
    private bool previousMovementEnabled = true;
    private Coroutine focusRoutine;
    private BackGroundParallax backgroundParallax;
    private Vector3 frozenPlayerPosition;
    private RigidbodyType2D previousPlayerBodyType;

    private void Awake()
    {
        ResolveReferences();
        HookTimelineEvents();
    }

    private IEnumerator Start()
    {
        yield return null;

        if (LevelLoader.Instance == null && playOnLevelEnter)
            PlayIntroTimeline();
    }

    private void OnDestroy()
    {
        UnhookTimelineEvents();
    }

    private void HookTimelineEvents()
    {
        if (introPlayableDirector != null)
            introPlayableDirector.stopped += OnTimelineFinished;
    }

    private void UnhookTimelineEvents()
    {
        if (introPlayableDirector != null)
            introPlayableDirector.stopped -= OnTimelineFinished;
    }

    public void PlayIntroTimeline()
    {
        ResolveReferences();

        if (introPlayableDirector == null ||
            introPlayableDirector.playableAsset == null)
        {
            SkipToGameplayImmediate();
            return;
        }

        BindGameplayTarget();

        introPlaying = true;

        PauseBackgroundParallax();
        FreezePlayer(freezePlayerDuringIntro);

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

    public void SkipToGameplayImmediate()
    {
        ResolveReferences();

        if (introPlayableDirector != null &&
            introPlayableDirector.state == PlayState.Playing)
        {
            introPlayableDirector.Stop();
        }

        BindGameplayTarget();
        SetGameplayLive();
        ResumeBackgroundParallax();
        RestorePlayer();

        introPlaying = false;
    }

    private CinemachineCamera GetGameplayCamera()
    {
        ResolveReferences();

        return cameraSetup != null
            ? cameraSetup.GameplayCamera
            : null;
    }

    private void SetGameplayLive()
    {
        CinemachineCamera gameplayCamera = GetGameplayCamera();

        if (gameplayCamera == null)
            return;

        if (!gameplayCamera.gameObject.activeSelf)
            gameplayCamera.gameObject.SetActive(true);

        SetPriority(focusCamera, inactivePriority);
        SetPriority(gameplayCamera, gameplayPriority);

        gameplayCamera.Prioritize();
    }

    public void PlayFocus(float duration = -1f)
    {
        if (introPlaying || focusPlaying)
            return;

        float targetDuration =
            duration > 0f ? duration : defaultFocusDuration;

        focusRoutine =
            StartCoroutine(FocusRoutine(targetDuration));
    }

    private IEnumerator FocusRoutine(float duration)
    {
        focusPlaying = true;

        FreezePlayer(freezePlayerDuringFocus);
        PauseBackgroundParallax();

        CinemachineCamera gameplayCamera = GetGameplayCamera();

        SetPriority(gameplayCamera, inactivePriority);
        SetPriority(focusCamera, focusPriority);

        if (focusCamera != null)
            focusCamera.Prioritize();

        yield return new WaitForSecondsRealtime(duration);

        SetGameplayLive();

        if (focusReturnBlendWait > 0f)
        {
            yield return
                new WaitForSecondsRealtime(focusReturnBlendWait);
        }

        ResumeBackgroundParallax();
        RestorePlayer();

        focusPlaying = false;
        focusRoutine = null;
    }

    private void BindGameplayTarget()
    {
        ResolveReferences();

        if (cameraSetup == null)
            return;

        cameraSetup.BindScene();

        CinemachineCamera gameplayCamera =
            cameraSetup.GameplayCamera;

        Transform playerTarget =
            cameraSetup.PlayerTarget;

        if (gameplayCamera != null && playerTarget != null)
            gameplayCamera.Follow = playerTarget;
    }

    private void SetPriority(
        CinemachineCamera camera,
        int priority)
    {
        if (camera == null)
            return;

        PrioritySettings settings = camera.Priority;
        settings.Enabled = true;
        settings.Value = priority;
        camera.Priority = settings;
    }

    private void FreezePlayer(bool shouldFreeze)
    {
        if (!shouldFreeze || playerFrozen)
            return;

        PlayerMovement movement = GetPlayerMovement();

        if (movement == null)
            return;

        previousMovementEnabled = movement.enabled;
        movement.enabled = false;
        playerFrozen = true;

        Rigidbody2D rigidbody =
            movement.GetComponent<Rigidbody2D>();

        if (rigidbody != null)
        {
            frozenPlayerPosition = movement.transform.position;
            previousPlayerBodyType = rigidbody.bodyType;
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.bodyType = RigidbodyType2D.Static;
        }

        Animator animator =
            movement.GetComponentInChildren<Animator>();

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
        if (!playerFrozen)
            return;

        PlayerMovement movement = GetPlayerMovement();

        if (movement != null)
        {
            Rigidbody2D rigidbody =
                movement.GetComponent<Rigidbody2D>();

            if (rigidbody != null)
            {
                movement.transform.position =
                    frozenPlayerPosition;

                rigidbody.bodyType =
                    previousPlayerBodyType;
            }

            movement.enabled =
                previousMovementEnabled;
        }

        playerFrozen = false;
    }

    private PlayerMovement GetPlayerMovement()
    {
        ResolveReferences();

        if (cameraSetup != null &&
            cameraSetup.PlayerMovement != null)
        {
            return cameraSetup.PlayerMovement;
        }

        return FindComponentInMyScene<PlayerMovement>();
    }

    private void PauseBackgroundParallax()
    {
        ResolveReferences();

        if (backgroundParallax != null)
            backgroundParallax.PauseParallax();
    }

    private void ResumeBackgroundParallax()
    {
        ResolveReferences();

        if (backgroundParallax != null)
            backgroundParallax.ResumeParallaxFromCurrentCamera();
    }

    private void ResolveReferences()
    {
        if (introPlayableDirector == null)
            introPlayableDirector = GetComponent<PlayableDirector>();

        if (cameraSetup == null)
            cameraSetup = FindComponentInMyScene<LevelCameraSetup>();

        if (backgroundParallax == null)
            backgroundParallax = FindComponentInMyScene<BackGroundParallax>();

        if (focusCamera == null)
            focusCamera = FindCinemachineCameraByName("DoorCamera");
    }

    private CinemachineCamera
        FindCinemachineCameraByName(string objectName)
    {
        Scene scene = gameObject.scene;

        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CinemachineCamera[] cameras =
                root.GetComponentsInChildren<CinemachineCamera>(true);

            foreach (CinemachineCamera camera in cameras)
            {
                if (camera == null)
                    continue;

                if (string.Equals(
                    camera.gameObject.name,
                    objectName,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    return camera;
                }
            }
        }

        return null;
    }

    private T FindComponentInMyScene<T>()
        where T : Component
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

    public void PrepareIntro()
    {
        ResolveReferences();
        BindGameplayTarget();
        FreezePlayer(freezePlayerDuringIntro);
    }

    public IEnumerator PlayIntroRoutine()
    {
        PlayIntroTimeline();

        while (introPlaying)
            yield return null;
    }
}
