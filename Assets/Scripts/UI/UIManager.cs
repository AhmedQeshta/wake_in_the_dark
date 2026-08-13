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
    // MENU AUDIO
    // ==================================================

    [Header("Menu Audio")]

    [Tooltip(
        "Music used only while the Start / Pause menu is visible."
    )]
    [SerializeField]
    private AudioSource menuAudioSource;


    // ==================================================
    // UI ANIMATION
    // ==================================================

    [Header("UI Animation")]

    [SerializeField, Min(0.01f)]
    private float menuFadeDuration = 0.25f;

    [SerializeField, Min(0.01f)]
    private float backgroundFadeDuration = 0.4f;


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

    /*
     * Used mainly if Bootstrap itself is reloaded.
     *
     * During normal additive level changes,
     * UIManager remains alive inside Bootstrap.
     */
    private static bool sessionStarted;


    // ==================================================
    // RESET STATIC STATE
    // ==================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        sessionStarted = false;

        Instance = null;
    }


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        // ----------------------------------------------
        // SINGLE INSTANCE
        // ----------------------------------------------

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


        Instance = this;


        // ----------------------------------------------
        // BUTTONS
        // ----------------------------------------------

        SetupButtonListeners();


        // ----------------------------------------------
        // MENU AUDIO
        // ----------------------------------------------

        if (menuAudioSource != null)
        {
            menuAudioSource.playOnAwake = false;

            menuAudioSource.loop = true;


            /*
             * IMPORTANT:
             *
             * Gameplay uses:
             *
             * AudioListener.pause = true
             *
             * when the pause menu opens.
             *
             * Menu music must continue playing.
             */
            menuAudioSource.ignoreListenerPause = true;
        }


        // ----------------------------------------------
        // GAME STATE
        // ----------------------------------------------

        gameStarted = sessionStarted;


        // ----------------------------------------------
        // SUBMENUS HIDDEN
        // ----------------------------------------------

        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        SetCanvasImmediate(
            levelsMenuGroup,
            false
        );


        // ----------------------------------------------
        // INITIAL STATE
        // ----------------------------------------------

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
    // START
    // ==================================================

    private void Start()
    {
        /*
         * Awake order between UIManager and
         * AudioManager is not guaranteed.
         *
         * By Start(), both should normally exist.
         */


        if (gameStarted)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance
                    .StartGameplayMusic();
            }
        }
        else
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance
                    .StopGameplayMusic(
                        true
                    );
            }
        }
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        /*
         * ESC does not control pause before
         * gameplay has started.
         */
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


        // ----------------------------------------------
        // SETTINGS OPEN
        // ----------------------------------------------

        if (IsSettingsOpen())
        {
            CloseSettings();

            return;
        }


        // ----------------------------------------------
        // LEVELS OPEN
        // ----------------------------------------------

        if (IsLevelsOpen())
        {
            CloseLevels();

            return;
        }


        // ----------------------------------------------
        // PAUSE / RESUME
        // ----------------------------------------------

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
    // BUTTON LISTENERS
    // ==================================================

    private void SetupButtonListeners()
    {
        // START
        if (startButton != null)
        {
            startButton.onClick.AddListener(
                StartGame
            );
        }


        // RESUME
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(
                ResumeGame
            );
        }


        // RESET
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(
                ResetLevel
            );
        }


        // LEVELS
        if (levelsButton != null)
        {
            levelsButton.onClick.AddListener(
                OpenLevels
            );
        }


        // SETTINGS
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(
                OpenSettings
            );
        }


        // SETTINGS BACK
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(
                CloseSettings
            );
        }


        // LEVELS BACK
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
        // ----------------------------------------------
        // STATE
        // ----------------------------------------------

        gameStarted = false;

        menuOpen = true;

        isTransitioning = false;


        // ----------------------------------------------
        // FREEZE GAME
        // ----------------------------------------------

        Time.timeScale = 0f;


        // ----------------------------------------------
        // BUTTONS
        // ----------------------------------------------

        /*
         * First launch:
         *
         * Start visible
         * Resume hidden
         * Reset hidden
         */
        SetMenuButtons(
            showStart: true,
            showPause: false,
            showReset: false
        );


        // ----------------------------------------------
        // MAIN MENU
        // ----------------------------------------------

        SetCanvasImmediate(
            mainMenuGroup,
            true
        );


        // ----------------------------------------------
        // SUBMENUS
        // ----------------------------------------------

        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        SetCanvasImmediate(
            levelsMenuGroup,
            false
        );


        // ----------------------------------------------
        // BACKGROUND
        // ----------------------------------------------

        SetCanvasImmediate(
            backgroundGroup,
            true
        );


        // ----------------------------------------------
        // GAME AUDIO
        // ----------------------------------------------

        PauseGameAudio();


        /*
         * Level music should not play behind
         * the first Start menu.
         */
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance
                .StopGameplayMusic(
                    true
                );
        }


        // ----------------------------------------------
        // MENU MUSIC
        // ----------------------------------------------

        PlayMenuAudio();
    }


    // ==================================================
    // START GAME
    // ==================================================

    private void StartGame()
    {
        if (gameStarted ||
            isTransitioning)
        {
            return;
        }


        // ----------------------------------------------
        // STATE
        // ----------------------------------------------

        gameStarted = true;

        sessionStarted = true;

        menuOpen = false;


        // ----------------------------------------------
        // SUBMENUS
        // ----------------------------------------------

        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        SetCanvasImmediate(
            levelsMenuGroup,
            false
        );


        // ----------------------------------------------
        // BUTTONS
        // ----------------------------------------------

        /*
         * From now on:
         *
         * Start hidden
         * Resume visible
         * Reset visible
         */
        SetMenuButtons(
            showStart: false,
            showPause: true,
            showReset: true
        );


        // ----------------------------------------------
        // MENU AUDIO
        // ----------------------------------------------

        StopMenuAudio();


        // ----------------------------------------------
        // GAME AUDIO
        // ----------------------------------------------

        ResumeGameAudio();


        // ----------------------------------------------
        // LEVEL MUSIC
        // ----------------------------------------------

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance
                .StartGameplayMusic();
        }


        // ----------------------------------------------
        // GAMEPLAY
        // ----------------------------------------------

        Time.timeScale = 1f;


        // ==================================================
        // LEVEL 1 CINEMACHINE INTRO
        // ==================================================

        /*
         * Level_01 was already loaded behind
         * the Start menu.
         *
         * Once Start is pressed:
         *
         * IntroCamera
         *      ↓
         * GameplayCamera / Player
         */
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance
                .PlayCurrentLevelIntro();
        }


        // ----------------------------------------------
        // MENU FADE OUT
        // ----------------------------------------------

        StartCoroutine(
            FadeCanvas(
                mainMenuGroup,
                false
            )
        );


        // ----------------------------------------------
        // BACKGROUND FADE OUT
        // ----------------------------------------------

        FadeBackground(
            false
        );
    }


    // ==================================================
    // PAUSE GAME
    // ==================================================

    private void PauseGame()
    {
        if (!gameStarted ||
            isTransitioning)
        {
            return;
        }


        // ----------------------------------------------
        // STATE
        // ----------------------------------------------

        menuOpen = true;


        // ----------------------------------------------
        // SUBMENUS
        // ----------------------------------------------

        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        SetCanvasImmediate(
            levelsMenuGroup,
            false
        );


        // ----------------------------------------------
        // BUTTONS
        // ----------------------------------------------

        SetMenuButtons(
            showStart: false,
            showPause: true,
            showReset: true
        );


        // ----------------------------------------------
        // FREEZE GAME
        // ----------------------------------------------

        Time.timeScale = 0f;


        // ----------------------------------------------
        // PAUSE GAME AUDIO
        // ----------------------------------------------

        /*
         * AudioManager music has:
         *
         * ignoreListenerPause = false
         *
         * therefore it pauses automatically.
         */
        PauseGameAudio();


        // ----------------------------------------------
        // BACKGROUND
        // ----------------------------------------------

        FadeBackground(
            true
        );


        // ----------------------------------------------
        // MENU
        // ----------------------------------------------

        StartCoroutine(
            FadeCanvas(
                mainMenuGroup,
                true
            )
        );


        // ----------------------------------------------
        // MENU MUSIC
        // ----------------------------------------------

        PlayMenuAudio();
    }


    // ==================================================
    // RESUME GAME
    // ==================================================

    private void ResumeGame()
    {
        if (!gameStarted ||
            isTransitioning)
        {
            return;
        }


        /*
         * If user is inside Settings/Levels,
         * Back should be used first.
         */
        if (IsSettingsOpen() ||
            IsLevelsOpen())
        {
            return;
        }


        // ----------------------------------------------
        // STATE
        // ----------------------------------------------

        menuOpen = false;


        // ----------------------------------------------
        // MENU MUSIC
        // ----------------------------------------------

        StopMenuAudio();


        // ----------------------------------------------
        // GAME AUDIO
        // ----------------------------------------------

        ResumeGameAudio();


        /*
         * AudioManager's track was paused,
         * not stopped.
         *
         * It resumes from the same timestamp.
         */


        // ----------------------------------------------
        // GAME
        // ----------------------------------------------

        Time.timeScale = 1f;


        // ----------------------------------------------
        // BUTTONS
        // ----------------------------------------------

        SetMenuButtons(
            showStart: false,
            showPause: true,
            showReset: true
        );


        // ----------------------------------------------
        // MAIN MENU OUT
        // ----------------------------------------------

        StartCoroutine(
            FadeCanvas(
                mainMenuGroup,
                false
            )
        );


        // ----------------------------------------------
        // BACKGROUND OUT
        // ----------------------------------------------

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


        /*
         * Settings can't remain visible underneath.
         */
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
        isTransitioning = true;


        // ----------------------------------------------
        // MAIN MENU OUT
        // ----------------------------------------------

        yield return FadeCanvasInternal(
            mainMenuGroup,
            false
        );


        // ----------------------------------------------
        // LEVELS IN
        // ----------------------------------------------

        yield return FadeCanvasInternal(
            levelsMenuGroup,
            true
        );


        isTransitioning = false;
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
        isTransitioning = true;


        // ----------------------------------------------
        // LEVELS OUT
        // ----------------------------------------------

        yield return FadeCanvasInternal(
            levelsMenuGroup,
            false
        );


        // ----------------------------------------------
        // START / PAUSE MENU BACK
        // ----------------------------------------------

        yield return FadeCanvasInternal(
            mainMenuGroup,
            true
        );


        isTransitioning = false;
    }


    // ==================================================
    // LEVELS OPEN CHECK
    // ==================================================

    private bool IsLevelsOpen()
    {
        return
            levelsMenuGroup != null &&
            levelsMenuGroup.alpha > 0.5f;
    }


    // ==================================================
    // OPEN SETTINGS
    // ==================================================

    private void OpenSettings()
    {
        if (isTransitioning)
            return;


        /*
         * Levels can't remain underneath.
         */
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
        isTransitioning = true;


        // ----------------------------------------------
        // MAIN MENU OUT
        // ----------------------------------------------

        yield return FadeCanvasInternal(
            mainMenuGroup,
            false
        );


        // ----------------------------------------------
        // SETTINGS IN
        // ----------------------------------------------

        yield return FadeCanvasInternal(
            settingsMenuGroup,
            true
        );


        isTransitioning = false;
    }


    // ==================================================
    // CLOSE SETTINGS
    // ==================================================

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
        isTransitioning = true;


        // ----------------------------------------------
        // SETTINGS OUT
        // ----------------------------------------------

        yield return FadeCanvasInternal(
            settingsMenuGroup,
            false
        );


        // ----------------------------------------------
        // MAIN MENU BACK
        // ----------------------------------------------

        yield return FadeCanvasInternal(
            mainMenuGroup,
            true
        );


        isTransitioning = false;
    }


    // ==================================================
    // SETTINGS OPEN CHECK
    // ==================================================

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
                "UIManager: LevelLoader.Instance is null.",
                this
            );

            return;
        }


        /*
         * IMPORTANT:
         *
         * DO NOT use:
         *
         * SceneManager.LoadScene(...)
         *
         * Bootstrap must stay alive.
         *
         * LevelLoader unloads/reloads only
         * the active Level_XX scene.
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
                "UIManager: LevelLoader not found " +
                "during player death reload.",
                this
            );

            return;
        }


        /*
         * Same architecture as Reset:
         *
         * Bootstrap stays
         * current level reloads
         */
        LevelLoader.Instance
            .ReloadCurrentLevel();
    }


    // ==================================================
    // PREPARE FOR LEVEL CHANGE
    // ==================================================

    /*
     * Called by LevelLoader BEFORE:
     *
     * Level_01 → Level_02
     * Levels menu selection
     * Reset
     * final death reload
     */
    public void PrepareForLevelChange()
    {
        // ----------------------------------------------
        // SESSION
        // ----------------------------------------------

        sessionStarted = true;

        gameStarted = true;

        menuOpen = false;

        isTransitioning = true;


        // ----------------------------------------------
        // STOP OLD UI COROUTINES
        // ----------------------------------------------

        StopAllCoroutines();


        backgroundFadeRoutine = null;


        // ==================================================
        // HIDE ALL MENUS IMMEDIATELY
        // ==================================================

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


        // ----------------------------------------------
        // BUTTONS
        // ----------------------------------------------

        SetMenuButtons(
            showStart: false,
            showPause: true,
            showReset: true
        );


        // ----------------------------------------------
        // MENU MUSIC
        // ----------------------------------------------

        StopMenuAudio();


        // ----------------------------------------------
        // GAME AUDIO
        // ----------------------------------------------

        ResumeGameAudio();


        // ----------------------------------------------
        // GAME TIME
        // ----------------------------------------------

        Time.timeScale = 1f;
    }


    // ==================================================
    // LEVEL LOAD FINISHED
    // ==================================================

    /*
     * Called by LevelLoader after:
     *
     * Level_02 loaded
     * Level_03 loaded
     * level reload completed
     *
     * This means we return DIRECTLY to gameplay.
     */
    public void OnLevelLoadFinished()
    {
        // ----------------------------------------------
        // SESSION
        // ----------------------------------------------

        sessionStarted = true;

        gameStarted = true;

        menuOpen = false;

        isTransitioning = false;


        // ----------------------------------------------
        // GAME
        // ----------------------------------------------

        Time.timeScale = 1f;


        // ----------------------------------------------
        // AUDIO LISTENER
        // ----------------------------------------------

        ResumeGameAudio();


        // ==================================================
        // MENUS HIDDEN
        // ==================================================

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


        // ----------------------------------------------
        // BUTTONS FOR NEXT PAUSE
        // ----------------------------------------------

        SetMenuButtons(
            showStart: false,
            showPause: true,
            showReset: true
        );


        // ----------------------------------------------
        // MENU MUSIC OFF
        // ----------------------------------------------

        StopMenuAudio();


        // ==================================================
        // CURRENT LEVEL MUSIC
        // ==================================================

        /*
         * AudioManager already detected the new
         * LevelMusicSettings when the scene loaded.
         *
         * This tells it gameplay is active.
         *
         * It will:
         *
         * Level 1 music → Level 2 music
         *
         * using the configured crossfade.
         */
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance
                .StartGameplayMusic();
        }
    }


    // ==================================================
    // START GAMEPLAY IMMEDIATE
    // ==================================================

    /*
     * Mainly used if Bootstrap/UIManager itself
     * is recreated after gameplay had already started.
     */
    private void StartGameplayStateImmediate()
    {
        gameStarted = true;

        menuOpen = false;

        isTransitioning = false;


        // ----------------------------------------------
        // GAME
        // ----------------------------------------------

        Time.timeScale = 1f;


        ResumeGameAudio();


        // ----------------------------------------------
        // BUTTONS
        // ----------------------------------------------

        SetMenuButtons(
            showStart: false,
            showPause: true,
            showReset: true
        );


        // ----------------------------------------------
        // UI
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


        // ----------------------------------------------
        // MENU MUSIC
        // ----------------------------------------------

        StopMenuAudio();


        // ----------------------------------------------
        // LEVEL MUSIC
        // ----------------------------------------------

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance
                .StartGameplayMusic();
        }
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
    // STANDARD CANVAS FADE
    // ==================================================

    private IEnumerator FadeCanvas(
        CanvasGroup group,
        bool show)
    {
        if (group == null)
            yield break;


        isTransitioning = true;


        yield return FadeCanvasInternal(
            group,
            show
        );


        isTransitioning = false;
    }


    // ==================================================
    // INTERNAL CANVAS FADE
    // ==================================================

    private IEnumerator FadeCanvasInternal(
        CanvasGroup group,
        bool show)
    {
        if (group == null)
            yield break;


        float startAlpha =
            group.alpha;


        float targetAlpha =
            show
                ? 1f
                : 0f;


        // ----------------------------------------------
        // INPUT
        // ----------------------------------------------

        if (show)
        {
            group.interactable = true;

            group.blocksRaycasts = true;
        }
        else
        {
            group.interactable = false;

            group.blocksRaycasts = false;
        }


        // ----------------------------------------------
        // ALREADY THERE
        // ----------------------------------------------

        if (Mathf.Approximately(
                startAlpha,
                targetAlpha))
        {
            group.alpha = targetAlpha;

            group.interactable = show;

            group.blocksRaycasts = show;

            yield break;
        }


        // ----------------------------------------------
        // FADE
        // ----------------------------------------------

        float elapsed = 0f;


        while (elapsed <
               menuFadeDuration)
        {
            /*
             * UI must animate even when
             * Time.timeScale = 0.
             */
            elapsed +=
                Time.unscaledDeltaTime;


            float progress =
                Mathf.Clamp01(
                    elapsed /
                    menuFadeDuration
                );


            group.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    progress
                );


            yield return null;
        }


        // ----------------------------------------------
        // FINAL
        // ----------------------------------------------

        group.alpha = targetAlpha;

        group.interactable = show;

        group.blocksRaycasts = show;
    }


    // ==================================================
    // BACKGROUND FADE
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


        float startAlpha =
            backgroundGroup.alpha;


        float targetAlpha =
            show
                ? 1f
                : 0f;


        // ----------------------------------------------
        // INPUT
        // ----------------------------------------------

        backgroundGroup.interactable =
            show;


        backgroundGroup.blocksRaycasts =
            show;


        // ----------------------------------------------
        // FADE
        // ----------------------------------------------

        float elapsed = 0f;


        while (elapsed <
               backgroundFadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float progress =
                Mathf.Clamp01(
                    elapsed /
                    backgroundFadeDuration
                );


            backgroundGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    progress
                );


            yield return null;
        }


        // ----------------------------------------------
        // FINAL
        // ----------------------------------------------

        backgroundGroup.alpha =
            targetAlpha;


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
            show
                ? 1f
                : 0f;


        group.interactable =
            show;


        group.blocksRaycasts =
            show;
    }


    // ==================================================
    // GAME AUDIO
    // ==================================================

    private void PauseGameAudio()
    {
        /*
         * Pauses:
         *
         * Level music
         * player sounds
         * trap sounds
         * lever sounds
         * platform sounds
         * etc.
         */
        AudioListener.pause = true;


        /*
         * Menu music ignores the listener pause.
         */
        if (menuAudioSource != null)
        {
            menuAudioSource.ignoreListenerPause =
                true;
        }
    }


    private void ResumeGameAudio()
    {
        AudioListener.pause = false;
    }


    // ==================================================
    // MENU MUSIC
    // ==================================================

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


    // ==================================================
    // STATIC GAMEPLAY STATE
    // ==================================================

    public static void MarkGameplayStarted()
    {
        sessionStarted = true;


        if (Instance != null)
        {
            Instance.gameStarted = true;
        }
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        if (Instance != this)
            return;


        Instance = null;


        /*
         * Ensure Editor / scene shutdown doesn't
         * remain stuck in paused state.
         */
        Time.timeScale = 1f;


        AudioListener.pause = false;
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