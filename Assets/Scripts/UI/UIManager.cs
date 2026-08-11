using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // ==================================================
    // CANVAS GROUPS
    // ==================================================

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup mainMenuGroup;
    [SerializeField] private CanvasGroup settingsMenuGroup;
    [SerializeField] private CanvasGroup levelsMenuGroup;
    [SerializeField] private CanvasGroup backgroundGroup;


    // ==================================================
    // MAIN MENU BUTTONS
    // ==================================================

    [Header("Main Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button levelsButton;
    [SerializeField] private Button settingsButton;


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
    // MENU AUDIO
    // ==================================================

    [Header("Menu Audio")]
    [SerializeField] private AudioSource menuAudioSource;


    // ==================================================
    // LEVEL MUSIC
    // ==================================================

    [Header("Level Music")]
    [SerializeField] private AudioSource levelMusicSource;


    // ==================================================
    // UI ANIMATION
    // ==================================================

    [Header("Animation")]

    [SerializeField, Min(0.01f)]
    private float menuFadeDuration = 0.25f;

    [SerializeField, Min(0.01f)]
    private float backgroundFadeDuration = 0.4f;


    // ==================================================
    // GAME STATE
    // ==================================================

    private bool gameStarted;
    private bool menuOpen;
    private bool isTransitioning;


    // ==================================================
    // COROUTINES
    // ==================================================

    private Coroutine backgroundFadeRoutine;


    // ==================================================
    // SESSION STATE
    // ==================================================

    /*
     * Remembers whether gameplay already started
     * when the current scene is reloaded.
     *
     * This prevents Start_btn from appearing again
     * after Reset or Player Death.
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
    }


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        SetupButtonListeners();


        // ----------------------------------------------
        // MENU AUDIO
        // ----------------------------------------------

        if (menuAudioSource != null)
        {
            menuAudioSource.playOnAwake = false;
            menuAudioSource.loop = true;

            /*
             * Menu music must keep playing while
             * all gameplay audio is paused.
             */
            menuAudioSource.ignoreListenerPause = true;
        }


        // ----------------------------------------------
        // LEVEL MUSIC
        // ----------------------------------------------

        if (levelMusicSource != null)
        {
            levelMusicSource.playOnAwake = false;
            levelMusicSource.loop = true;

            /*
             * Level music is gameplay audio,
             * so it follows AudioListener.pause.
             */
            levelMusicSource.ignoreListenerPause = false;
        }


        gameStarted = sessionStarted;


        // ----------------------------------------------
        // SUB MENUS START HIDDEN
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
            StartGameplayAfterReload();
        }
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        /*
         * Before Start is pressed, ESC doesn't
         * need to control gameplay pause/resume.
         */
        if (!gameStarted || isTransitioning)
            return;


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ------------------------------------------
            // SETTINGS OPEN
            // ------------------------------------------

            if (IsSettingsOpen())
            {
                CloseSettings();
                return;
            }


            // ------------------------------------------
            // LEVELS MENU OPEN
            // ------------------------------------------

            if (IsLevelsOpen())
            {
                CloseLevels();
                return;
            }


            // ------------------------------------------
            // NORMAL PAUSE / RESUME
            // ------------------------------------------

            if (menuOpen)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }


    // ==================================================
    // BUTTON LISTENERS
    // ==================================================

    private void SetupButtonListeners()
    {
        // START GAME
        if (startButton != null)
        {
            startButton.onClick.AddListener(
                StartGame
            );
        }


        /*
         * When this button is visible the game
         * is already paused, so it RESUMES.
         */
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(
                ResumeGame
            );
        }


        // RESET LEVEL
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(
                ResetLevel
            );
        }


        // OPEN LEVELS MENU
        if (levelsButton != null)
        {
            levelsButton.onClick.AddListener(
                OpenLevels
            );
        }


        // OPEN SETTINGS
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
        // Freeze game.
        Time.timeScale = 0f;


        gameStarted = false;
        menuOpen = true;


        // ----------------------------------------------
        // BUTTONS
        // ----------------------------------------------

        /*
         * First opening:
         *
         * Start = visible
         * Pause = hidden
         * Reset = hidden
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
        // SUB MENUS
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
        // AUDIO
        // ----------------------------------------------

        PauseGameAudio();

        StopLevelMusic();

        PlayMenuAudio();
    }


    // ==================================================
    // START GAME
    // ==================================================

    private void StartGame()
    {
        if (gameStarted || isTransitioning)
            return;


        gameStarted = true;
        sessionStarted = true;
        menuOpen = false;


        // ----------------------------------------------
        // HIDE SUB MENUS
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
        // AUDIO
        // ----------------------------------------------

        StopMenuAudio();

        ResumeGameAudio();

        PlayLevelMusic();


        // ----------------------------------------------
        // GAMEPLAY
        // ----------------------------------------------

        Time.timeScale = 1f;


        // ----------------------------------------------
        // FADE UI OUT
        // ----------------------------------------------

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
    // PAUSE GAME
    // ==================================================

    private void PauseGame()
    {
        if (!gameStarted || isTransitioning)
            return;


        menuOpen = true;


        // ----------------------------------------------
        // SUB MENUS
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

        PauseGameAudio();


        // ----------------------------------------------
        // UI
        // ----------------------------------------------

        FadeBackground(
            true
        );


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
        if (!gameStarted || isTransitioning)
            return;


        /*
         * Do not resume directly while one of
         * the child menus is open.
         */
        if (IsSettingsOpen() ||
            IsLevelsOpen())
        {
            return;
        }


        menuOpen = false;


        // ----------------------------------------------
        // AUDIO
        // ----------------------------------------------

        StopMenuAudio();

        ResumeGameAudio();


        /*
         * Do NOT PlayLevelMusic() here.
         *
         * AudioListener.pause simply paused it,
         * so it continues from the same position.
         */


        // ----------------------------------------------
        // GAME
        // ----------------------------------------------

        Time.timeScale = 1f;


        // ----------------------------------------------
        // BUTTON STATE FOR NEXT PAUSE
        // ----------------------------------------------

        SetMenuButtons(
            showStart: false,
            showPause: true,
            showReset: true
        );


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
    // LEVELS MENU
    // ==================================================

    private void OpenLevels()
    {
        if (isTransitioning)
            return;


        /*
         * Make absolutely sure Settings isn't
         * visible behind the Levels screen.
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
        // DISABLE MAIN MENU INPUT
        // ----------------------------------------------

        SetCanvasInteraction(
            mainMenuGroup,
            false
        );


        // ----------------------------------------------
        // FADE MAIN MENU OUT
        // ----------------------------------------------

        yield return FadeCanvasInternal(
            mainMenuGroup,
            false
        );


        // ----------------------------------------------
        // FADE LEVELS MENU IN
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
        // FADE LEVELS OUT
        // ----------------------------------------------

        yield return FadeCanvasInternal(
            levelsMenuGroup,
            false
        );


        // ----------------------------------------------
        // RETURN TO START / PAUSE MENU
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
    // SETTINGS
    // ==================================================

    private void OpenSettings()
    {
        if (isTransitioning)
            return;


        /*
         * Make sure Levels screen isn't visible.
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
        // MAIN MENU IN
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


        StartCoroutine(
            ResetLevelRoutine()
        );
    }


    private IEnumerator ResetLevelRoutine()
    {
        isTransitioning = true;


        /*
         * Game was already started.
         */
        sessionStarted = true;


        // ----------------------------------------------
        // DISABLE MENU INPUT
        // ----------------------------------------------

        SetCanvasInteraction(
            mainMenuGroup,
            false
        );


        SetCanvasInteraction(
            settingsMenuGroup,
            false
        );


        SetCanvasInteraction(
            levelsMenuGroup,
            false
        );


        // ----------------------------------------------
        // HIDE SUB MENUS
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
        // BACKGROUND FADE OUT
        // ----------------------------------------------

        if (backgroundFadeRoutine != null)
        {
            StopCoroutine(
                backgroundFadeRoutine
            );

            backgroundFadeRoutine = null;
        }


        if (backgroundGroup != null)
        {
            yield return StartCoroutine(
                FadeBackgroundRoutine(
                    false
                )
            );
        }


        // ----------------------------------------------
        // MENU FADE OUT
        // ----------------------------------------------

        if (mainMenuGroup != null)
        {
            yield return FadeCanvasInternal(
                mainMenuGroup,
                false
            );
        }


        // ----------------------------------------------
        // AUDIO
        // ----------------------------------------------

        StopMenuAudio();

        StopLevelMusic();

        ResumeGameAudio();


        // ----------------------------------------------
        // RESTORE TIME
        // ----------------------------------------------

        Time.timeScale = 1f;


        // ----------------------------------------------
        // RELOAD
        // ----------------------------------------------

        Scene currentScene =
            SceneManager.GetActiveScene();


        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }


    // ==================================================
    // PLAYER DEATH RELOAD
    // ==================================================

    public void ReloadAfterPlayerDeath()
    {
        if (isTransitioning)
            return;


        StartCoroutine(
            ReloadAfterPlayerDeathRoutine()
        );
    }


    private IEnumerator ReloadAfterPlayerDeathRoutine()
    {
        isTransitioning = true;


        /*
         * Do not return to Start menu after death.
         */
        sessionStarted = true;


        // ----------------------------------------------
        // AUDIO
        // ----------------------------------------------

        StopMenuAudio();

        StopLevelMusic();

        ResumeGameAudio();


        // ----------------------------------------------
        // GAME TIME
        // ----------------------------------------------

        Time.timeScale = 1f;


        // ----------------------------------------------
        // ONE FRAME
        // ----------------------------------------------

        yield return null;


        // ----------------------------------------------
        // RELOAD
        // ----------------------------------------------

        Scene currentScene =
            SceneManager.GetActiveScene();


        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }


    // ==================================================
    // AFTER SCENE RELOAD
    // ==================================================

    private void StartGameplayAfterReload()
    {
        gameStarted = true;
        menuOpen = false;


        // ----------------------------------------------
        // AUDIO
        // ----------------------------------------------

        ResumeGameAudio();


        // ----------------------------------------------
        // TIME
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
        // MENUS HIDDEN
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


        // ----------------------------------------------
        // BACKGROUND HIDDEN
        // ----------------------------------------------

        SetCanvasImmediate(
            backgroundGroup,
            false
        );


        // ----------------------------------------------
        // AUDIO
        // ----------------------------------------------

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
    // CANVAS FADE INTERNAL
    // ==================================================

    /*
     * This version does not change isTransitioning.
     *
     * This allows us to fade:
     *
     * Start_Menu -> Levels_Menu
     *
     * or:
     *
     * Start_Menu -> Settings_Menu
     *
     * as one complete transition.
     */
    private IEnumerator FadeCanvasInternal(
        CanvasGroup group,
        bool show)
    {
        if (group == null)
            yield break;


        float startAlpha =
            group.alpha;


        float targetAlpha =
            show ? 1f : 0f;


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
        // NO FADE NEEDED
        // ----------------------------------------------

        if (Mathf.Approximately(
                startAlpha,
                targetAlpha
            ))
        {
            group.alpha = targetAlpha;

            yield break;
        }


        float elapsed = 0f;


        // ----------------------------------------------
        // FADE
        // ----------------------------------------------

        while (elapsed < menuFadeDuration)
        {
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


        group.alpha =
            targetAlpha;


        // ----------------------------------------------
        // FINAL INPUT STATE
        // ----------------------------------------------

        group.interactable =
            show;


        group.blocksRaycasts =
            show;
    }


    // ==================================================
    // CANVAS INTERACTION
    // ==================================================

    private void SetCanvasInteraction(
        CanvasGroup group,
        bool enabledState)
    {
        if (group == null)
            return;


        group.interactable =
            enabledState;


        group.blocksRaycasts =
            enabledState;
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
            show ? 1f : 0f;


        // ----------------------------------------------
        // INPUT
        // ----------------------------------------------

        if (show)
        {
            backgroundGroup.interactable = true;
            backgroundGroup.blocksRaycasts = true;
        }
        else
        {
            backgroundGroup.interactable = false;
            backgroundGroup.blocksRaycasts = false;
        }


        float elapsed = 0f;


        // ----------------------------------------------
        // FADE
        // ----------------------------------------------

        while (
            elapsed <
            backgroundFadeDuration
        )
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
            show ? 1f : 0f;


        group.interactable =
            show;


        group.blocksRaycasts =
            show;
    }


    // ==================================================
    // GLOBAL GAME AUDIO
    // ==================================================

    private void PauseGameAudio()
    {
        /*
         * Pause:
         *
         * Level music
         * traps
         * platforms
         * player sounds
         * levers
         * etc.
         */
        AudioListener.pause =
            true;


        /*
         * Menu music is allowed to continue.
         */
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
    // LEVEL MUSIC
    // ==================================================

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
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
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


    // ==================================================
    // LEVEL TRANSITION STATE
    // ==================================================

    public static void MarkGameplayStarted()
    {
        sessionStarted = true;
    }
}