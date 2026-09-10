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
    [SerializeField] private CanvasGroup mainMenuGroup;
    [SerializeField] private CanvasGroup settingsMenuGroup;
    [SerializeField] private CanvasGroup levelsMenuGroup;
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private CanvasGroup howToPlayMenuGroup;


    // ==================================================
    // MAIN MENU BUTTONS
    // ==================================================

    [Header("Main Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button levelsButton;
    [SerializeField] private Button settingsButton;

    [SerializeField] private Button howToPlayButton;

    // ==================================================
    // SETTINGS MENU
    // ==================================================

    [Header("Settings Menu")]
    [SerializeField] private Button settingsBackButton;


    // ==================================================
    // LEVELS MENU
    // ==================================================

    [Header("Levels Menu")]
    [SerializeField] private Button levelsBackButton;




    // ==================================================
    // HOW TO PLAY MENU
    // ==================================================

    [Header("How To Play Menu")]
    [SerializeField] private Button howToPlayBackButton;


    // ==================================================
    // MENU AUDIO
    // ==================================================

    [Header("Menu Audio")]
    [Tooltip("Music used only while the Start / Pause menu is visible.")]
    [SerializeField] private AudioSource menuAudioSource;

    // ==================================================
    // UI ANIMATION
    // ==================================================

    [Header("UI Animation")]
    [SerializeField, Min(0.01f)] private float menuFadeDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float backgroundFadeDuration = 0.4f;


    // ==================================================
    // GAME ENDING
    // ==================================================

    [Header("Game Ending")]
    [Tooltip("Level that should be loaded behind the Bootstrap main menu after the game ending.")]
    [SerializeField] private string initialMenuLevelSceneName = "Level_01";


    // ==================================================
    // STATE
    // ==================================================

    private bool gameStarted;
    private bool menuOpen;
    private bool isTransitioning;


    /*
     * True only while Level_Final is being replaced by Level_01 behind the Bootstrap Start Menu.
     */
    private bool returningToMainMenuAfterEnding;
    private Coroutine backgroundFadeRoutine;

    // ==================================================
    // SESSION
    // ==================================================
    /*
     * Used mainly if Bootstrap itself is reloaded.
     *
     * During normal additive level changes, UIManager remains alive inside Bootstrap.
     */
    private static bool sessionStarted;


    // ==================================================
    // RESET STATIC STATE
    // ==================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
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
        // SINGLE INSTANCE

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // BUTTONS

        SetupButtonListeners();


        // MENU AUDIO

        if (menuAudioSource != null)
        {
            menuAudioSource.playOnAwake = false;
            menuAudioSource.loop = true;
            /*
             * Gameplay uses:
             * AudioListener.pause = true, when the pause menu opens.
             * 
             * Menu music must continue playing.
             */
            menuAudioSource.ignoreListenerPause = true;
        }


        // GAME STATE
        gameStarted = sessionStarted;


        // SUBMENUS HIDDEN
        SetCanvasImmediate(settingsMenuGroup, false);
        SetCanvasImmediate(howToPlayMenuGroup, false);

        SetCanvasImmediate(levelsMenuGroup, false);


        // INITIAL STATE

        if (!gameStarted)
            ShowInitialMenu();
        else
            StartGameplayStateImmediate();

    }

    // ==================================================
    // START
    // ==================================================
    private void Start()
    {
        /*
         * Awake order between UIManager and AudioManager is not guaranteed.
         * By Start(), both should normally exist.
         */

        if (gameStarted)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.StartGameplayMusic();
        }
        else
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.StopGameplayMusic(true);
        }
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        /*
         * ESC does not control pause before gameplay has started.
         */
        if (!gameStarted || isTransitioning || !Input.GetKeyDown(KeyCode.Escape))
            return;

        // SETTINGS OPEN
        if (IsSettingsOpen())
        {
            CloseSettings();
            return;
        }

        // LEVELS OPEN
        if (IsLevelsOpen())
        {
            CloseLevels();
            return;
        }


        // PAUSE / RESUME
        if (menuOpen)
            ResumeGame();
        else
            PauseGame();
    }


    // ==================================================
    // BUTTON LISTENERS
    // ==================================================

    private void SetupButtonListeners()
    {
        // START
        /*
         * Level_01 is already loaded behind the Bootstrap menu.
         * The Start button must request the Level 1 intro video FIRST.
         *
         * Do not call StartGame() directly here, otherwise gameplay starts immediately and the Level 1 video is bypassed.
         */
        if (startButton != null)
            startButton.onClick.AddListener(StartGameWithIntroVideo);

        // RESUME
        if (pauseButton != null)
            pauseButton.onClick.AddListener(ResumeGame);

        // RESET
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetLevel);

        // LEVELS
        if (levelsButton != null)
            levelsButton.onClick.AddListener(OpenLevels);

        // SETTINGS
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        // HOW TO PLAY
        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(OpenHowToPlayButton);

        // SETTINGS BACK
        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(CloseSettings);

        // LEVELS BACK
        if (levelsBackButton != null)
            levelsBackButton.onClick.AddListener(CloseLevels);

        // HOW TO PLAY BACK
        if (howToPlayBackButton != null)
            howToPlayBackButton.onClick.AddListener(CloseHowToPlay);
    }


    // ==================================================
    // INITIAL MENU
    // ==================================================

    private void ShowInitialMenu()
    {
        // CURSOR
        if (GameCursorManager.Instance != null)
            GameCursorManager.Instance.ShowMenuCursor();

        // STATE
        gameStarted = false;
        menuOpen = true;
        isTransitioning = false;

        // FREEZE GAME
        Time.timeScale = 0f;

        /*
         * ==> BUTTONS
         * First launch: Start visible, Resume hidden, Reset hidden
         */
        SetMenuButtons(showStart: true, showPause: false, showReset: false);

        // MAIN MENU
        SetCanvasImmediate(mainMenuGroup, true);

        // SUBMENUS
        SetCanvasImmediate(settingsMenuGroup, false);
        SetCanvasImmediate(levelsMenuGroup, false);

        // BACKGROUND
        SetCanvasImmediate(backgroundGroup, true);

        // GAME AUDIO
        PauseGameAudio();

        /*
         * Level music should not play behind the first Start menu.
         */
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopGameplayMusic(true);

        // MENU MUSIC
        PlayMenuAudio();
    }


    // ==================================================
    // START / CONTINUE GAME WITH INTRO VIDEO
    // ==================================================

    private void StartGameWithIntroVideo()
    {
        if (gameStarted || isTransitioning)
            return;



        /*
         * Bootstrap initially loads Level_01 behind the Start Menu.
         *
         * However, the Start button is now a CONTINUE button:
         *
         * New game
         *     -> Level_01
         *
         * Finish Level_01
         *     -> saved continue level = Level_02
         *
         * Finish Level_03, close game, reopen
         *     -> saved continue level = Level_04
         *
         * Wait for Bootstrap's initial additive level load to finish first.
         * This prevents a fast Start click from racing LevelLoader.
         */
        StartCoroutine(StartGameFromSavedProgressRoutine());
    }


    private IEnumerator StartGameFromSavedProgressRoutine()
    {
        LevelLoader loader = LevelLoader.Instance;


        while (loader != null && loader.IsLoading)
        {
            yield return null;
        }


        if (gameStarted || isTransitioning)
            yield break;

        string continueSceneName = ResolveSavedContinueSceneName();


        if (string.IsNullOrWhiteSpace(continueSceneName))
            continueSceneName = initialMenuLevelSceneName;


        // ----------------------------------------------
        // NORMAL PATH: VIDEO MANAGER EXISTS
        // ----------------------------------------------

        if (LevelVideoIntroManager.Instance != null)
        {
            LevelVideoIntroManager.Instance.RequestStartLevel(continueSceneName);
            yield break;
        }


        // ----------------------------------------------
        // FALLBACK: NO VIDEO MANAGER
        // ----------------------------------------------

        StartSavedLevelWithoutVideo(continueSceneName);
    }


    private string ResolveSavedContinueSceneName()
    {
        if (LevelProgressManager.Instance != null)
        {
            string savedSceneName = LevelProgressManager.Instance.GetContinueLevelSceneName();

            if (!string.IsNullOrWhiteSpace(savedSceneName))
                return savedSceneName;
        }


        /*
         * If progression manager is unexpectedly missing,
         * prefer the level that Bootstrap already has loaded.
         */
        if (LevelLoader.Instance != null && !string.IsNullOrWhiteSpace(LevelLoader.Instance.CurrentLevelSceneName))
            return LevelLoader.Instance.CurrentLevelSceneName;

        return string.IsNullOrWhiteSpace(initialMenuLevelSceneName) ? "Level_01" : initialMenuLevelSceneName;
    }


    private void StartSavedLevelWithoutVideo(string sceneName)
    {
        LevelLoader loader = LevelLoader.Instance;


        if (loader != null && !string.IsNullOrWhiteSpace(sceneName) && !string.Equals(loader.CurrentLevelSceneName, sceneName, System.StringComparison.OrdinalIgnoreCase))
        { /*  * Later saved level:  * unload the current Level_XX and load the saved one  * through the existing additive LevelLoader.  */
            loader.LoadLevel(sceneName);
            return;
        }


        /*
         * Saved level is already loaded (normally Level_01).
         */
        StartGame();
    }


    // ==================================================
    // START GAME AFTER ALREADY-LOADED LEVEL VIDEO
    // ==================================================
    /*
     * Assign this public method to:
     * LevelVideoIntroManager -> On Already Loaded Level Ready
     */
    public void StartGameAfterIntroVideo()
    {
        StartGame();
    }


    // ==================================================
    // START GAME
    // ==================================================
    private void StartGame()
    {
        // CURSOR
        if (GameCursorManager.Instance != null)
            GameCursorManager.Instance.HideGameplayCursor();

        if (gameStarted || isTransitioning)
            return;


        // STATE
        gameStarted = true;
        sessionStarted = true;
        menuOpen = false;


        // SUBMENUS
        SetCanvasImmediate(settingsMenuGroup, false);
        SetCanvasImmediate(levelsMenuGroup, false);


        /*
         * BUTTONS
         * From now on: Start hidden, Resume visible, Reset visible
         */
        SetMenuButtons(showStart: false, showPause: true, showReset: true);

        // MENU AUDIO
        StopMenuAudio();

        // GAME AUDIO
        ResumeGameAudio();

        // LEVEL MUSIC
        if (AudioManager.Instance != null)
            AudioManager.Instance.StartGameplayMusic();

        // GAMEPLAY
        Time.timeScale = 1f;

        // ==================================================
        // LEVEL 1 CINEMACHINE INTRO
        // ==================================================
        if (LevelLoader.Instance != null)
            LevelLoader.Instance.PlayCurrentLevelIntro();

        // MENU FADE OUT
        StartCoroutine(FadeCanvas(mainMenuGroup, false));

        // BACKGROUND FADE OUT
        FadeBackground(false);
    }


    // ==================================================
    // PAUSE GAME
    // ==================================================

    private void PauseGame()
    {
        // CURSOR
        if (GameCursorManager.Instance != null)
            GameCursorManager.Instance.ShowMenuCursor();

        if (!gameStarted || isTransitioning)
            return;

        // STATE
        menuOpen = true;

        // SUBMENUS
        SetCanvasImmediate(settingsMenuGroup, false);
        SetCanvasImmediate(levelsMenuGroup, false);


        // BUTTONS
        SetMenuButtons(showStart: false, showPause: true, showReset: true);


        // FREEZE GAME
        Time.timeScale = 0f;

        /*
         *  PAUSE GAME AUDIO
         * AudioManager music has:
         * ignoreListenerPause = false ,therefore it pauses automatically.
         */
        PauseGameAudio();

        // BACKGROUND
        FadeBackground(true);

        // MENU
        StartCoroutine(FadeCanvas(mainMenuGroup, true));

        // MENU MUSIC
        PlayMenuAudio();
    }


    // ==================================================
    // RESUME GAME
    // ==================================================

    private void ResumeGame()
    {
        // CURSOR
        if (GameCursorManager.Instance != null)
            GameCursorManager.Instance.HideGameplayCursor();

        /*
         * If user is inside Settings/Levels, Back should be used first, and game not Started or not Transitioning.
        */
        if (!gameStarted || isTransitioning || IsSettingsOpen() || IsLevelsOpen())
            return;

        // STATE
        menuOpen = false;

        // MENU MUSIC
        StopMenuAudio();

        // GAME AUDIO
        ResumeGameAudio();

        // GAME
        Time.timeScale = 1f;

        // BUTTONS
        SetMenuButtons(showStart: false, showPause: true, showReset: true);

        // MAIN MENU OUT
        StartCoroutine(FadeCanvas(mainMenuGroup, false));

        // BACKGROUND OUT
        FadeBackground(false);
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
        SetCanvasImmediate(settingsMenuGroup, false);
        StartCoroutine(OpenLevelsRoutine());
    }


    private IEnumerator OpenLevelsRoutine()
    {
        isTransitioning = true;

        // MAIN MENU OUT
        yield return FadeCanvasInternal(mainMenuGroup, false);

        // LEVELS IN
        yield return FadeCanvasInternal(levelsMenuGroup, true);

        isTransitioning = false;
    }


    // ==================================================
    // CLOSE LEVELS
    // ==================================================
    private void CloseLevels()
    {
        if (isTransitioning)
            return;

        StartCoroutine(CloseLevelsRoutine());
    }


    private IEnumerator CloseLevelsRoutine()
    {
        isTransitioning = true;

        // LEVELS OUT
        yield return FadeCanvasInternal(levelsMenuGroup, false);

        // START / PAUSE MENU BACK
        yield return FadeCanvasInternal(mainMenuGroup, true);

        isTransitioning = false;
    }


    // ==================================================
    // CLOSE HOW TO PLAY
    // ==================================================
    private void CloseHowToPlay()
    {
        if (isTransitioning)
            return;

        StartCoroutine(CloseHowToPlayRoutine());
    }


    private IEnumerator CloseHowToPlayRoutine()
    {
        isTransitioning = true;

        // HOW TO PLAY OUT
        yield return FadeCanvasInternal(howToPlayMenuGroup, false);

        // START / PAUSE MENU BACK
        yield return FadeCanvasInternal(mainMenuGroup, true);

        isTransitioning = false;
    }



    // ==================================================
    // LEVELS OPEN CHECK
    // ==================================================
    private bool IsLevelsOpen()
    {
        return levelsMenuGroup != null && levelsMenuGroup.alpha > 0.5f;
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
        SetCanvasImmediate(levelsMenuGroup, false);
        StartCoroutine(OpenSettingsRoutine());
    }


    private IEnumerator OpenSettingsRoutine()
    {
        isTransitioning = true;

        // MAIN MENU OUT
        yield return FadeCanvasInternal(mainMenuGroup, false);

        // SETTINGS IN
        yield return FadeCanvasInternal(settingsMenuGroup, true);

        isTransitioning = false;
    }


    // ==================================================
    // OPEN HOW TO PLAY
    // ==================================================

    private void OpenHowToPlayButton()
    {
        if (isTransitioning)
            return;

        /*
         * Levels can't remain underneath.
         */
        SetCanvasImmediate(levelsMenuGroup, false);
        StartCoroutine(OpenHowToPlayRoutine());
    }

    private IEnumerator OpenHowToPlayRoutine()
    {
        isTransitioning = true;

        // MAIN MENU OUT
        yield return FadeCanvasInternal(mainMenuGroup, false);

        // HOW TO PLAY IN
        yield return FadeCanvasInternal(howToPlayMenuGroup, true);

        isTransitioning = false;
    }



    // ==================================================
    // CLOSE SETTINGS
    // ==================================================
    private void CloseSettings()
    {
        if (isTransitioning)
            return;

        StartCoroutine(CloseSettingsRoutine());
    }


    private IEnumerator CloseSettingsRoutine()
    {
        isTransitioning = true;

        // SETTINGS OUT
        yield return FadeCanvasInternal(settingsMenuGroup, false);

        // MAIN MENU BACK
        yield return FadeCanvasInternal(mainMenuGroup, true);

        isTransitioning = false;
    }


    // ==================================================
    // SETTINGS OPEN CHECK
    // ==================================================

    private bool IsSettingsOpen()
    {
        return settingsMenuGroup != null && settingsMenuGroup.alpha > 0.5f;
    }


    // ==================================================
    // RESET LEVEL
    // ==================================================

    private void ResetLevel()
    {
        if (!gameStarted || isTransitioning || LevelLoader.Instance == null)
            return;


        /*
         * Note:
         * DO NOT use: SceneManager.LoadScene(...)
         * Bootstrap must stay alive. LevelLoader unloads/reloads only the active Level_XX scene.
         */
        LevelLoader.Instance.ReloadCurrentLevel();
    }


    // ==================================================
    // FINAL PLAYER DEATH
    // ==================================================

    public void ReloadAfterPlayerDeath()
    {
        if (isTransitioning || LevelLoader.Instance == null)
            return;

        /*
         * Same architecture as Reset:
         * Bootstrap stays current level reloads
         */
        LevelLoader.Instance.ReloadCurrentLevel();
    }


    // ==================================================
    // PREPARE FOR LEVEL CHANGE
    // ==================================================

    /*
     * Called by LevelLoader BEFORE:
     * Level_01 → Level_02
     * Levels menu selection
     * Reset
     * final death reload
     */
    public void PrepareForLevelChange()
    {
        // CURSOR
        if (GameCursorManager.Instance != null)
            GameCursorManager.Instance.HideGameplayCursor();

        // SESSION
        sessionStarted = true;
        gameStarted = true;
        menuOpen = false;
        isTransitioning = true;

        // STOP OLD UI COROUTINES
        StopAllCoroutines();

        backgroundFadeRoutine = null;

        // ==================================================
        // HIDE ALL MENUS IMMEDIATELY
        // ==================================================
        SetCanvasImmediate(mainMenuGroup, false);
        SetCanvasImmediate(settingsMenuGroup, false);
        SetCanvasImmediate(levelsMenuGroup, false);
        SetCanvasImmediate(backgroundGroup, false);

        // BUTTONS
        SetMenuButtons(showStart: false, showPause: true, showReset: true);
        // MENU MUSIC

        StopMenuAudio();


        // GAME AUDIO

        bool keepWorldAudioMuted = returningToMainMenuAfterEnding || (LevelVideoIntroManager.Instance != null && LevelVideoIntroManager.Instance.KeepWorldAudioMutedDuringLevelLoad);

        /*
         * A level-intro / ending video already silenced the
         * previous level. Keep it silent until the new level
         * has completely loaded.
        */
        if (keepWorldAudioMuted)
            PauseGameAudio();
        else
            ResumeGameAudio();


        // GAME TIME
        Time.timeScale = 1f;
    }


    // ==================================================
    // LEVEL LOAD FINISHED
    // ==================================================

    /*
     * Called by LevelLoader after:
     * Level_02 loaded
     * Level_03 loaded
     * level reload completed
     * This means we return DIRECTLY to gameplay.
     */
    public void OnLevelLoadFinished()
    {

        if (returningToMainMenuAfterEnding)
        {
            isTransitioning = false;
            Time.timeScale = 1f;
            PauseGameAudio();

            if (AudioManager.Instance != null)
                AudioManager.Instance.StopGameplayMusic(true);

            if (LevelVideoIntroManager.Instance != null)
                LevelVideoIntroManager.Instance.NotifyLevelLoadFinished();

            return;
        }

        // CURSOR
        if (GameCursorManager.Instance != null)
            GameCursorManager.Instance.HideGameplayCursor();

        // SESSION
        sessionStarted = true;
        gameStarted = true;
        menuOpen = false;
        isTransitioning = false;

        // GAME
        Time.timeScale = 1f;

        // AUDIO LISTENER
        ResumeGameAudio();


        // ==================================================
        // MENUS HIDDEN
        // ==================================================
        SetCanvasImmediate(mainMenuGroup, false);
        SetCanvasImmediate(settingsMenuGroup, false);
        SetCanvasImmediate(levelsMenuGroup, false);
        SetCanvasImmediate(backgroundGroup, false);

        // BUTTONS FOR NEXT PAUSE
        SetMenuButtons(showStart: false, showPause: true, showReset: true);

        // MENU MUSIC OFF
        StopMenuAudio();


        // ==================================================
        // CURRENT LEVEL MUSIC
        // ==================================================
        /*
         * AudioManager already detected the new
         * LevelMusicSettings when the scene loaded.
         * This tells it gameplay is active.
         * It will: Level 1 music → Level 2 music using the configured crossfade.
         */
        if (AudioManager.Instance != null)
            AudioManager.Instance.StartGameplayMusic();

        if (LevelVideoIntroManager.Instance != null)
            LevelVideoIntroManager.Instance.NotifyLevelLoadFinished();
    }


    // ==================================================
    // RETURN TO MAIN MENU AFTER GAME ENDING
    // ==================================================

    public void ReturnToMainMenuAfterGameEnding()
    {
        returningToMainMenuAfterEnding = true;
        /*
         * The Bootstrap scene remains alive for the whole game.
         * To begin a clean new game:
         * 1. unload Level_Final,
         * 2. load Level_01 behind the menu,
         * 3. restore the initial Bootstrap menu state.
         */

        if (LevelLoader.Instance == null)
        {
            sessionStarted = false;
            returningToMainMenuAfterEnding = false;

            if (LevelVideoIntroManager.Instance != null)
                LevelVideoIntroManager.Instance.NotifyLevelLoadFinished();


            ShowInitialMenu();
            return;
        }

        if (string.IsNullOrWhiteSpace(initialMenuLevelSceneName))
            initialMenuLevelSceneName = "Level_01";



        /*
         * If Level_01 is already the active level, no scene transition is necessary.
         */
        if (string.Equals(LevelLoader.Instance.CurrentLevelSceneName, initialMenuLevelSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            sessionStarted = false;
            returningToMainMenuAfterEnding = false;

            if (LevelVideoIntroManager.Instance != null)
                LevelVideoIntroManager.Instance.NotifyLevelLoadFinished();

            ShowInitialMenu();
            return;
        }


        /*
         * LoadLevelFromMenu already performs the normal safe additive unload/load flow and keeps Bootstrap alive.
         * It temporarily enters gameplay state while loading.
         * The coroutine below waits until LevelLoader finishes, then restores the actual Start Menu state.
         */
        LevelLoader.Instance.LoadLevelFromMenu(initialMenuLevelSceneName);
        StartCoroutine(ReturnToMainMenuAfterEndingRoutine());
    }


    private IEnumerator ReturnToMainMenuAfterEndingRoutine()
    {
        /*
         * StartCoroutine in LevelLoader sets IsLoading on its
         * first execution, but yield once so the transition is
         * unquestionably underway.
         */
        yield return null;

        LevelLoader loader = LevelLoader.Instance;


        while (loader != null && loader.IsLoading)
            yield return null;



        /*
         * This is a NEW GAME session now.
         * Level_01 stays loaded behind the menu exactly like the original Bootstrap startup architecture.
         */
        sessionStarted = false;
        gameStarted = false;
        menuOpen = true;
        isTransitioning = false;
        ShowInitialMenu();
        returningToMainMenuAfterEnding = false;
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
        // CURSOR
        if (GameCursorManager.Instance != null)
            GameCursorManager.Instance.HideGameplayCursor();


        gameStarted = true;
        menuOpen = false;
        isTransitioning = false;

        // GAME
        Time.timeScale = 1f;
        ResumeGameAudio();

        // BUTTONS
        SetMenuButtons(showStart: false, showPause: true, showReset: true);

        // UI
        SetCanvasImmediate(mainMenuGroup, false);
        SetCanvasImmediate(settingsMenuGroup, false);
        SetCanvasImmediate(levelsMenuGroup, false);
        SetCanvasImmediate(backgroundGroup, false);


        // MENU MUSIC
        StopMenuAudio();

        // LEVEL MUSIC
        if (AudioManager.Instance != null)
            AudioManager.Instance.StartGameplayMusic();

    }


    // ==================================================
    // BUTTON VISIBILITY
    // ==================================================

    private void SetMenuButtons(bool showStart, bool showPause, bool showReset)
    {
        if (startButton != null)
            startButton.gameObject.SetActive(showStart);

        if (pauseButton != null)
            pauseButton.gameObject.SetActive(showPause);

        if (resetButton != null)
            resetButton.gameObject.SetActive(showReset);
    }


    // ==================================================
    // STANDARD CANVAS FADE
    // ==================================================
    private IEnumerator FadeCanvas(CanvasGroup group, bool show)
    {
        if (group == null)
            yield break;

        isTransitioning = true;
        yield return FadeCanvasInternal(group, show);
        isTransitioning = false;
    }


    // ==================================================
    // INTERNAL CANVAS FADE
    // ==================================================
    private IEnumerator FadeCanvasInternal(CanvasGroup group, bool show)
    {
        if (group == null)
            yield break;

        float startAlpha = group.alpha;
        float targetAlpha = show ? 1f : 0f;


        // INPUT
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


        // ALREADY THERE
        if (Mathf.Approximately(startAlpha, targetAlpha))
        {
            group.alpha = targetAlpha;
            group.interactable = show;
            group.blocksRaycasts = show;
            yield break;
        }


        // FADE
        float elapsed = 0f;

        while (elapsed < menuFadeDuration)
        {
            /*
             * UI must animate even when Time.timeScale = 0.
             */
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / menuFadeDuration);
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            yield return null;
        }


        // FINAL
        group.alpha = targetAlpha;
        group.interactable = show;
        group.blocksRaycasts = show;
    }


    // ==================================================
    // BACKGROUND FADE
    // ==================================================
    private void FadeBackground(bool show)
    {
        if (backgroundGroup == null)
            return;

        if (backgroundFadeRoutine != null)
            StopCoroutine(backgroundFadeRoutine);

        backgroundFadeRoutine = StartCoroutine(FadeBackgroundRoutine(show));
    }


    private IEnumerator FadeBackgroundRoutine(bool show)
    {
        if (backgroundGroup == null)
            yield break;

        float startAlpha = backgroundGroup.alpha;
        float targetAlpha = show ? 1f : 0f;

        // INPUT
        backgroundGroup.interactable = show;
        backgroundGroup.blocksRaycasts = show;

        // FADE
        float elapsed = 0f;

        while (elapsed < backgroundFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / backgroundFadeDuration);
            backgroundGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }


        // FINAL
        backgroundGroup.alpha = targetAlpha;
        backgroundGroup.interactable = show;
        backgroundGroup.blocksRaycasts = show;
        backgroundFadeRoutine = null;
    }


    // ==================================================
    // CANVAS IMMEDIATE
    // ==================================================

    private void SetCanvasImmediate(CanvasGroup group, bool show)
    {
        if (group == null)
            return;
        group.alpha = show ? 1f : 0f;
        group.interactable = show;
        group.blocksRaycasts = show;
    }


    // ==================================================
    // VIDEO / TRANSITION AUDIO
    // ==================================================

    public void PrepareForVideoPlayback()
    {
        /*
         * The previous level must be silent while its intro/ending video is displayed.
         * Menu music uses ignoreListenerPause = true, so it must also be stopped explicitly.
         */

        StopMenuAudio();
        PauseGameAudio();


        if (AudioManager.Instance != null)
            AudioManager.Instance.StopGameplayMusic(true);

    }


    public void RestoreAudioAfterCancelledVideo()
    {
        if (gameStarted)
        {
            StopMenuAudio();
            ResumeGameAudio();

            if (AudioManager.Instance != null)
                AudioManager.Instance.StartGameplayMusic();

            return;
        }


        /*
         * Still on the initial Bootstrap menu.
         */
        PauseGameAudio();

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopGameplayMusic(true);

        PlayMenuAudio();
    }


    // ==================================================
    // GAME AUDIO
    // ==================================================
    private void PauseGameAudio()
    {
        /*
         * Pauses:
         * Level music, player sounds, trap sounds, lever sounds, platform sounds, etc.
         */
        AudioListener.pause = true;

        /*
         * Menu music ignores the listener pause.
         */
        if (menuAudioSource != null)
            menuAudioSource.ignoreListenerPause = true;
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

        menuAudioSource.ignoreListenerPause = true;

        if (!menuAudioSource.isPlaying)
            menuAudioSource.Play();

    }


    private void StopMenuAudio()
    {
        if (menuAudioSource == null)
            return;

        if (menuAudioSource.isPlaying)
            menuAudioSource.Stop();
    }


    // ==================================================
    // STATIC GAMEPLAY STATE
    // ==================================================
    public static void MarkGameplayStarted()
    {
        sessionStarted = true;

        if (Instance != null)
            Instance.gameStarted = true;

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
         * Ensure Editor / scene shutdown doesn't remain stuck in paused state.
         */
        Time.timeScale = 1f;

        AudioListener.pause = false;
    }


    // ==================================================
    // VALIDATION
    // ==================================================
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(initialMenuLevelSceneName))
            initialMenuLevelSceneName = "Level_01";

        menuFadeDuration = Mathf.Max(0.01f, menuFadeDuration);
        backgroundFadeDuration = Mathf.Max(0.01f, backgroundFadeDuration);
    }
}