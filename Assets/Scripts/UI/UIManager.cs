using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // ==================================================
    // INSTANCE
    // ==================================================

    public static UIManager Instance { get; private set; }


    // ==================================================
    // CANVAS GROUPS
    // ==================================================

    [Header("Canvas Groups")]

    [SerializeField]
    private CanvasGroup mainMenuGroup;

    [SerializeField]
    private CanvasGroup settingsMenuGroup;

    [SerializeField]
    private CanvasGroup levelsMenuGroup;

    [SerializeField]
    private CanvasGroup backgroundGroup;


    // ==================================================
    // MAIN MENU BUTTONS
    // ==================================================

    [Header("Main Menu Buttons")]

    [SerializeField]
    private Button startButton;

    [SerializeField]
    private Button pauseButton;

    [SerializeField]
    private Button resetButton;

    [SerializeField]
    private Button levelsButton;

    [SerializeField]
    private Button settingsButton;


    // ==================================================
    // SETTINGS MENU
    // ==================================================

    [Header("Settings Menu")]

    [SerializeField]
    private Button settingsBackButton;


    // ==================================================
    // LEVELS MENU
    // ==================================================

    [Header("Levels Menu")]

    [SerializeField]
    private Button levelsBackButton;


    // ==================================================
    // AUDIO
    // ==================================================

    [Header("Menu Audio")]

    [SerializeField]
    private AudioSource menuAudioSource;


    [Header("Level Music")]

    [SerializeField]
    private AudioSource levelMusicSource;


    // ==================================================
    // ANIMATION
    // ==================================================

    [Header("UI Animation")]

    [SerializeField, Min(0.01f)]
    private float menuFadeDuration =
        0.25f;

    [SerializeField, Min(0.01f)]
    private float backgroundFadeDuration =
        0.4f;


    // ==================================================
    // STATE
    // ==================================================

    private bool gameStarted;

    private bool menuOpen;

    private bool isTransitioning;


    private Coroutine backgroundFadeRoutine;


    // ==================================================
    // SESSION
    // ==================================================

    private static bool sessionStarted;


    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        sessionStarted =
            false;


        Instance =
            null;
    }


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogError(
                "Duplicate UIManager found.",
                this
            );


            Destroy(gameObject);

            return;
        }


        Instance =
            this;


        SetupButtonListeners();


        // ----------------------------------------------
        // MENU AUDIO
        // ----------------------------------------------

        if (menuAudioSource != null)
        {
            menuAudioSource.playOnAwake =
                false;

            menuAudioSource.loop =
                true;

            menuAudioSource.ignoreListenerPause =
                true;
        }


        // ----------------------------------------------
        // LEVEL MUSIC
        // ----------------------------------------------

        if (levelMusicSource != null)
        {
            levelMusicSource.playOnAwake =
                false;

            levelMusicSource.loop =
                true;

            levelMusicSource.ignoreListenerPause =
                false;
        }


        gameStarted =
            sessionStarted;


        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        SetCanvasImmediate(
            levelsMenuGroup,
            false
        );


        if (!gameStarted)
        {
            ShowInitialMenu();
        }
        else
        {
            StartGameplayStateImmediate();
        }
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        if (!gameStarted ||
            isTransitioning)
        {
            return;
        }


        if (!Input.GetKeyDown(
                KeyCode.Escape))
        {
            return;
        }


        if (IsSettingsOpen())
        {
            CloseSettings();

            return;
        }


        if (IsLevelsOpen())
        {
            CloseLevels();

            return;
        }


        if (menuOpen)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }


    // ==================================================
    // LISTENERS
    // ==================================================

    private void SetupButtonListeners()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(
                StartGame
            );
        }


        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(
                ResumeGame
            );
        }


        if (resetButton != null)
        {
            resetButton.onClick.AddListener(
                ResetLevel
            );
        }


        if (levelsButton != null)
        {
            levelsButton.onClick.AddListener(
                OpenLevels
            );
        }


        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(
                OpenSettings
            );
        }


        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(
                CloseSettings
            );
        }


        if (levelsBackButton != null)
        {
            levelsBackButton.onClick.AddListener(
                CloseLevels
            );
        }
    }


    // ==================================================
    // INITIAL MENU
    // ==================================================

    private void ShowInitialMenu()
    {
        gameStarted =
            false;


        menuOpen =
            true;


        isTransitioning =
            false;


        Time.timeScale =
            0f;


        SetMenuButtons(
            true,
            false,
            false
        );


        SetCanvasImmediate(
            mainMenuGroup,
            true
        );


        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        SetCanvasImmediate(
            levelsMenuGroup,
            false
        );


        SetCanvasImmediate(
            backgroundGroup,
            true
        );


        PauseGameAudio();


        StopLevelMusic();


        PlayMenuAudio();
    }


    // ==================================================
    // START
    // ==================================================

    private void StartGame()
    {
        if (gameStarted ||
            isTransitioning)
        {
            return;
        }


        gameStarted =
            true;


        sessionStarted =
            true;


        menuOpen =
            false;


        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        SetCanvasImmediate(
            levelsMenuGroup,
            false
        );


        SetMenuButtons(
            false,
            true,
            true
        );


        StopMenuAudio();


        ResumeGameAudio();


        PlayLevelMusic();


        Time.timeScale =
            1f;


        // Start wide camera → Player camera.
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance
                .PlayCurrentLevelIntro();
        }

        StartCoroutine(
            FadeCanvas(
                mainMenuGroup,
                false
            )
        );


        FadeBackground(
            false
        );
    }


    // ==================================================
    // PAUSE
    // ==================================================

    private void PauseGame()
    {
        if (!gameStarted ||
            isTransitioning)
        {
            return;
        }


        menuOpen =
            true;


        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        SetCanvasImmediate(
            levelsMenuGroup,
            false
        );


        SetMenuButtons(
            false,
            true,
            true
        );


        Time.timeScale =
            0f;


        PauseGameAudio();


        FadeBackground(
            true
        );


        StartCoroutine(
            FadeCanvas(
                mainMenuGroup,
                true
            )
        );


        PlayMenuAudio();
    }


    // ==================================================
    // RESUME
    // ==================================================

    private void ResumeGame()
    {
        if (!gameStarted ||
            isTransitioning)
        {
            return;
        }


        if (IsSettingsOpen() ||
            IsLevelsOpen())
        {
            return;
        }


        menuOpen =
            false;


        StopMenuAudio();


        ResumeGameAudio();


        Time.timeScale =
            1f;


        SetMenuButtons(
            false,
            true,
            true
        );


        StartCoroutine(
            FadeCanvas(
                mainMenuGroup,
                false
            )
        );


        FadeBackground(
            false
        );
    }


    // ==================================================
    // OPEN LEVELS
    // ==================================================

    private void OpenLevels()
    {
        if (isTransitioning)
            return;


        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        StartCoroutine(
            OpenLevelsRoutine()
        );
    }


    private IEnumerator OpenLevelsRoutine()
    {
        isTransitioning =
            true;


        yield return FadeCanvasInternal(
            mainMenuGroup,
            false
        );


        yield return FadeCanvasInternal(
            levelsMenuGroup,
            true
        );


        isTransitioning =
            false;
    }


    // ==================================================
    // CLOSE LEVELS
    // ==================================================

    private void CloseLevels()
    {
        if (isTransitioning)
            return;


        StartCoroutine(
            CloseLevelsRoutine()
        );
    }


    private IEnumerator CloseLevelsRoutine()
    {
        isTransitioning =
            true;


        yield return FadeCanvasInternal(
            levelsMenuGroup,
            false
        );


        yield return FadeCanvasInternal(
            mainMenuGroup,
            true
        );


        isTransitioning =
            false;
    }


    private bool IsLevelsOpen()
    {
        return
            levelsMenuGroup != null &&
            levelsMenuGroup.alpha > 0.5f;
    }


    // ==================================================
    // SETTINGS
    // ==================================================

    private void OpenSettings()
    {
        if (isTransitioning)
            return;


        SetCanvasImmediate(
            levelsMenuGroup,
            false
        );


        StartCoroutine(
            OpenSettingsRoutine()
        );
    }


    private IEnumerator OpenSettingsRoutine()
    {
        isTransitioning =
            true;


        yield return FadeCanvasInternal(
            mainMenuGroup,
            false
        );


        yield return FadeCanvasInternal(
            settingsMenuGroup,
            true
        );


        isTransitioning =
            false;
    }


    private void CloseSettings()
    {
        if (isTransitioning)
            return;


        StartCoroutine(
            CloseSettingsRoutine()
        );
    }


    private IEnumerator CloseSettingsRoutine()
    {
        isTransitioning =
            true;


        yield return FadeCanvasInternal(
            settingsMenuGroup,
            false
        );


        yield return FadeCanvasInternal(
            mainMenuGroup,
            true
        );


        isTransitioning =
            false;
    }


    private bool IsSettingsOpen()
    {
        return
            settingsMenuGroup != null &&
            settingsMenuGroup.alpha > 0.5f;
    }


    // ==================================================
    // RESET LEVEL
    // ==================================================

    private void ResetLevel()
    {
        if (!gameStarted ||
            isTransitioning)
        {
            return;
        }


        if (LevelLoader.Instance == null)
        {
            Debug.LogError(
                "UIManager: LevelLoader not found.",
                this
            );

            return;
        }


        /*
         * IMPORTANT:
         *
         * No SceneManager.LoadScene here.
         *
         * Bootstrap stays alive and only
         * Level_XX gets unloaded/reloaded.
         */
        LevelLoader.Instance
            .ReloadCurrentLevel();
    }


    // ==================================================
    // FINAL PLAYER DEATH
    // ==================================================

    public void ReloadAfterPlayerDeath()
    {
        if (isTransitioning)
            return;


        if (LevelLoader.Instance == null)
        {
            Debug.LogError(
                "UIManager: LevelLoader not found during death reload.",
                this
            );

            return;
        }


        LevelLoader.Instance
            .ReloadCurrentLevel();
    }


    // ==================================================
    // PREPARE LEVEL CHANGE
    // ==================================================

    /*
     * Called by LevelLoader BEFORE:
     *
     * Level menu switch
     * door transition
     * reset
     * final death
     */
    public void PrepareForLevelChange()
    {
        sessionStarted =
            true;


        gameStarted =
            true;


        menuOpen =
            false;


        isTransitioning =
            true;


        /*
         * Stop old UI fades.
         */
        StopAllCoroutines();


        backgroundFadeRoutine =
            null;


        // ----------------------------------------------
        // HIDE EVERYTHING IMMEDIATELY
        // ----------------------------------------------

        SetCanvasImmediate(
            mainMenuGroup,
            false
        );


        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        SetCanvasImmediate(
            levelsMenuGroup,
            false
        );


        SetCanvasImmediate(
            backgroundGroup,
            false
        );


        SetMenuButtons(
            false,
            true,
            true
        );


        // ----------------------------------------------
        // RESTORE GAME STATE
        // ----------------------------------------------

        StopMenuAudio();


        ResumeGameAudio();


        Time.timeScale =
            1f;
    }


    // ==================================================
    // LEVEL FINISHED LOADING
    // ==================================================

    public void OnLevelLoadFinished()
    {
        sessionStarted =
            true;


        gameStarted =
            true;


        menuOpen =
            false;


        isTransitioning =
            false;


        Time.timeScale =
            1f;


        ResumeGameAudio();


        SetCanvasImmediate(
            mainMenuGroup,
            false
        );


        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        SetCanvasImmediate(
            levelsMenuGroup,
            false
        );


        SetCanvasImmediate(
            backgroundGroup,
            false
        );


        SetMenuButtons(
            false,
            true,
            true
        );


        StopMenuAudio();


        PlayLevelMusic();
    }


    // ==================================================
    // GAMEPLAY STATE
    // ==================================================

    private void StartGameplayStateImmediate()
    {
        gameStarted =
            true;


        menuOpen =
            false;


        isTransitioning =
            false;


        Time.timeScale =
            1f;


        ResumeGameAudio();


        SetMenuButtons(
            false,
            true,
            true
        );


        SetCanvasImmediate(
            mainMenuGroup,
            false
        );


        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        SetCanvasImmediate(
            levelsMenuGroup,
            false
        );


        SetCanvasImmediate(
            backgroundGroup,
            false
        );


        StopMenuAudio();


        PlayLevelMusic();
    }


    // ==================================================
    // BUTTON VISIBILITY
    // ==================================================

    private void SetMenuButtons(
        bool showStart,
        bool showPause,
        bool showReset)
    {
        if (startButton != null)
        {
            startButton.gameObject.SetActive(
                showStart
            );
        }


        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(
                showPause
            );
        }


        if (resetButton != null)
        {
            resetButton.gameObject.SetActive(
                showReset
            );
        }
    }


    // ==================================================
    // CANVAS FADE
    // ==================================================

    private IEnumerator FadeCanvas(
        CanvasGroup group,
        bool show)
    {
        if (group == null)
            yield break;


        isTransitioning =
            true;


        yield return FadeCanvasInternal(
            group,
            show
        );


        isTransitioning =
            false;
    }


    private IEnumerator FadeCanvasInternal(
        CanvasGroup group,
        bool show)
    {
        if (group == null)
            yield break;


        float start =
            group.alpha;


        float target =
            show ? 1f : 0f;


        if (show)
        {
            group.interactable =
                true;

            group.blocksRaycasts =
                true;
        }
        else
        {
            group.interactable =
                false;

            group.blocksRaycasts =
                false;
        }


        if (Mathf.Approximately(
                start,
                target))
        {
            group.alpha =
                target;

            yield break;
        }


        float elapsed =
            0f;


        while (elapsed <
               menuFadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    menuFadeDuration
                );


            group.alpha =
                Mathf.Lerp(
                    start,
                    target,
                    t
                );


            yield return null;
        }


        group.alpha =
            target;


        group.interactable =
            show;


        group.blocksRaycasts =
            show;
    }


    // ==================================================
    // BACKGROUND
    // ==================================================

    private void FadeBackground(
        bool show)
    {
        if (backgroundGroup == null)
            return;


        if (backgroundFadeRoutine != null)
        {
            StopCoroutine(
                backgroundFadeRoutine
            );
        }


        backgroundFadeRoutine =
            StartCoroutine(
                FadeBackgroundRoutine(
                    show
                )
            );
    }


    private IEnumerator FadeBackgroundRoutine(
        bool show)
    {
        if (backgroundGroup == null)
            yield break;


        float start =
            backgroundGroup.alpha;


        float target =
            show ? 1f : 0f;


        backgroundGroup.interactable =
            show;


        backgroundGroup.blocksRaycasts =
            show;


        float elapsed =
            0f;


        while (elapsed <
               backgroundFadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    backgroundFadeDuration
                );


            backgroundGroup.alpha =
                Mathf.Lerp(
                    start,
                    target,
                    t
                );


            yield return null;
        }


        backgroundGroup.alpha =
            target;


        backgroundGroup.interactable =
            show;


        backgroundGroup.blocksRaycasts =
            show;


        backgroundFadeRoutine =
            null;
    }


    // ==================================================
    // CANVAS IMMEDIATE
    // ==================================================

    private void SetCanvasImmediate(
        CanvasGroup group,
        bool show)
    {
        if (group == null)
            return;


        group.alpha =
            show ? 1f : 0f;


        group.interactable =
            show;


        group.blocksRaycasts =
            show;
    }


    // ==================================================
    // AUDIO
    // ==================================================

    private void PauseGameAudio()
    {
        AudioListener.pause =
            true;


        if (menuAudioSource != null)
        {
            menuAudioSource.ignoreListenerPause =
                true;
        }
    }


    private void ResumeGameAudio()
    {
        AudioListener.pause =
            false;
    }


    private void PlayMenuAudio()
    {
        if (menuAudioSource == null)
            return;


        menuAudioSource.ignoreListenerPause =
            true;


        if (!menuAudioSource.isPlaying)
        {
            menuAudioSource.Play();
        }
    }


    private void StopMenuAudio()
    {
        if (menuAudioSource == null)
            return;


        if (menuAudioSource.isPlaying)
        {
            menuAudioSource.Stop();
        }
    }


    private void PlayLevelMusic()
    {
        if (levelMusicSource == null)
            return;


        levelMusicSource.ignoreListenerPause =
            false;


        if (!levelMusicSource.isPlaying)
        {
            levelMusicSource.Play();
        }
    }


    private void StopLevelMusic()
    {
        if (levelMusicSource == null)
            return;


        levelMusicSource.Stop();
    }


    // ==================================================
    // STATIC
    // ==================================================

    public static void MarkGameplayStarted()
    {
        sessionStarted =
            true;


        if (Instance != null)
        {
            Instance.gameStarted =
                true;
        }
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        if (Instance != this)
            return;


        Instance =
            null;


        Time.timeScale =
            1f;


        AudioListener.pause =
            false;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        menuFadeDuration =
            Mathf.Max(
                0.01f,
                menuFadeDuration
            );


        backgroundFadeDuration =
            Mathf.Max(
                0.01f,
                backgroundFadeDuration
            );
    }
}