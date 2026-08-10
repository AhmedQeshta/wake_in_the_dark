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
    // SETTINGS
    // ==================================================

    [Header("Settings")]
    [SerializeField] private Button settingsBackButton;


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
     * Remembers that the player already started
     * the game when the current scene reloads.
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


        // ==============================================
        // MENU AUDIO SETUP
        // ==============================================

        if (menuAudioSource != null)
        {
            menuAudioSource.playOnAwake = false;
            menuAudioSource.loop = true;

            /*
             * Menu music must continue playing
             * while AudioListener.pause == true.
             */
            menuAudioSource.ignoreListenerPause = true;
        }


        // ==============================================
        // LEVEL MUSIC SETUP
        // ==============================================

        if (levelMusicSource != null)
        {
            levelMusicSource.playOnAwake = false;
            levelMusicSource.loop = true;

            /*
             * Level music is gameplay audio.
             *
             * It SHOULD pause when:
             *
             * AudioListener.pause = true.
             */
            levelMusicSource.ignoreListenerPause = false;
        }


        gameStarted = sessionStarted;


        /*
         * Settings always starts hidden.
         */
        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


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
         * ESC cannot pause/resume before
         * gameplay has started.
         */
        if (!gameStarted || isTransitioning)
            return;


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            /*
             * If Settings is open,
             * ESC returns to pause menu.
             */
            if (IsSettingsOpen())
            {
                CloseSettings();
                return;
            }


            /*
             * Toggle Pause / Resume.
             */
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
        // START
        if (startButton != null)
        {
            startButton.onClick.AddListener(
                StartGame
            );
        }


        /*
         * When pauseButton is visible,
         * the game is already paused.
         *
         * So clicking it resumes the game.
         */
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


        /*
         * Levels will be implemented later.
         */
        if (levelsButton != null)
        {
            levelsButton.interactable = false;
        }
    }


    // ==================================================
    // INITIAL MENU
    // ==================================================

    private void ShowInitialMenu()
    {
        /*
         * Freeze gameplay.
         */
        Time.timeScale = 0f;


        gameStarted = false;
        menuOpen = true;


        /*
         * FIRST SCREEN:
         *
         * Start = SHOW
         * Pause = HIDE
         * Reset = HIDE
         */
        SetMenuButtons(
            showStart: true,
            showPause: false,
            showReset: false
        );


        // Main menu visible.
        SetCanvasImmediate(
            mainMenuGroup,
            true
        );


        // Settings hidden.
        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        // Background visible.
        SetCanvasImmediate(
            backgroundGroup,
            true
        );


        /*
         * Pause all gameplay sound.
         */
        PauseGameAudio();


        /*
         * Level music must not play
         * on the initial menu.
         */
        StopLevelMusic();


        /*
         * Start menu music.
         */
        PlayMenuAudio();
    }


    // ==================================================
    // START GAME
    // ==================================================

    private void StartGame()
    {
        if (gameStarted)
            return;


        gameStarted = true;
        sessionStarted = true;
        menuOpen = false;


        /*
         * AFTER START:
         *
         * Start = HIDE
         * Pause = SHOW
         * Reset = SHOW
         */
        SetMenuButtons(
            showStart: false,
            showPause: true,
            showReset: true
        );


        // ----------------------------------------------
        // STOP MENU MUSIC
        // ----------------------------------------------

        StopMenuAudio();


        // ----------------------------------------------
        // RESTORE GAME AUDIO
        // ----------------------------------------------

        ResumeGameAudio();


        // ----------------------------------------------
        // START LEVEL MUSIC
        // ----------------------------------------------

        PlayLevelMusic();


        // ----------------------------------------------
        // START GAMEPLAY
        // ----------------------------------------------

        Time.timeScale = 1f;


        // ----------------------------------------------
        // FADE MENU OUT
        // ----------------------------------------------

        StartCoroutine(
            FadeCanvas(
                mainMenuGroup,
                false
            )
        );


        // ----------------------------------------------
        // FADE BACKGROUND OUT
        // ----------------------------------------------

        FadeBackground(false);
    }


    // ==================================================
    // PAUSE GAME
    // ==================================================

    private void PauseGame()
    {
        if (!gameStarted)
            return;


        menuOpen = true;


        /*
         * PAUSE MENU:
         *
         * Start = HIDE
         * Pause = SHOW
         * Reset = SHOW
         */
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
         * This automatically pauses:
         *
         * Level music
         * Player sounds
         * Trap sounds
         * Platform sounds
         * Lever sounds
         * etc.
         */
        PauseGameAudio();


        // ----------------------------------------------
        // BACKGROUND FADE IN
        // ----------------------------------------------

        FadeBackground(true);


        // ----------------------------------------------
        // MAIN MENU FADE IN
        // ----------------------------------------------

        StartCoroutine(
            FadeCanvas(
                mainMenuGroup,
                true
            )
        );


        // ----------------------------------------------
        // PLAY MENU MUSIC
        // ----------------------------------------------

        PlayMenuAudio();
    }


    // ==================================================
    // RESUME GAME
    // ==================================================

    private void ResumeGame()
    {
        if (!gameStarted)
            return;


        menuOpen = false;


        // ----------------------------------------------
        // STOP MENU MUSIC
        // ----------------------------------------------

        StopMenuAudio();


        // ----------------------------------------------
        // RESTORE GAME AUDIO
        // ----------------------------------------------

        /*
         * This resumes Level 1 music
         * from exactly where it was paused.
         *
         * Do NOT call PlayLevelMusic() here.
         */
        ResumeGameAudio();


        // ----------------------------------------------
        // RESUME GAME
        // ----------------------------------------------

        Time.timeScale = 1f;


        /*
         * Maintain button state for
         * next pause.
         */
        SetMenuButtons(
            showStart: false,
            showPause: true,
            showReset: true
        );


        // ----------------------------------------------
        // FADE MENU OUT
        // ----------------------------------------------

        StartCoroutine(
            FadeCanvas(
                mainMenuGroup,
                false
            )
        );


        // ----------------------------------------------
        // FADE BACKGROUND OUT
        // ----------------------------------------------

        FadeBackground(false);
    }


    // ==================================================
    // RESET LEVEL
    // ==================================================

    private void ResetLevel()
    {
        if (!gameStarted || isTransitioning)
            return;


        StartCoroutine(
            ResetLevelRoutine()
        );
    }


    private IEnumerator ResetLevelRoutine()
    {
        isTransitioning = true;


        /*
         * Keep game-started state after reload.
         */
        sessionStarted = true;


        // ----------------------------------------------
        // DISABLE MENU INPUT
        // ----------------------------------------------

        if (mainMenuGroup != null)
        {
            mainMenuGroup.interactable = false;
            mainMenuGroup.blocksRaycasts = false;
        }


        // ----------------------------------------------
        // FADE BACKGROUND OUT
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
        // FADE MENU OUT
        // ----------------------------------------------

        if (mainMenuGroup != null)
        {
            float startAlpha =
                mainMenuGroup.alpha;


            float elapsed = 0f;


            while (elapsed < menuFadeDuration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;


                float progress =
                    Mathf.Clamp01(
                        elapsed /
                        menuFadeDuration
                    );


                mainMenuGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        0f,
                        progress
                    );


                yield return null;
            }


            mainMenuGroup.alpha = 0f;
        }


        // ----------------------------------------------
        // STOP MENU MUSIC
        // ----------------------------------------------

        StopMenuAudio();


        // ----------------------------------------------
        // STOP LEVEL MUSIC
        // ----------------------------------------------

        /*
         * Very important:
         *
         * Level music is currently paused by
         * AudioListener.pause.
         *
         * Stop it BEFORE unpausing audio,
         * otherwise you may hear a tiny sound
         * before scene reload.
         */
        StopLevelMusic();


        // ----------------------------------------------
        // RESTORE GLOBAL AUDIO
        // ----------------------------------------------

        ResumeGameAudio();


        // ----------------------------------------------
        // RESTORE GAME TIME
        // ----------------------------------------------

        Time.timeScale = 1f;


        // ----------------------------------------------
        // RELOAD CURRENT SCENE
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
        StartCoroutine(
            ReloadAfterPlayerDeathRoutine()
        );
    }


    private IEnumerator ReloadAfterPlayerDeathRoutine()
    {
        /*
         * Player already started the game.
         *
         * Do not show Start menu after death.
         */
        sessionStarted = true;


        // ----------------------------------------------
        // STOP MENU MUSIC
        // ----------------------------------------------

        StopMenuAudio();


        // ----------------------------------------------
        // STOP CURRENT LEVEL MUSIC
        // ----------------------------------------------

        StopLevelMusic();


        // ----------------------------------------------
        // RESTORE AUDIO STATE
        // ----------------------------------------------

        ResumeGameAudio();


        // ----------------------------------------------
        // RESTORE TIME
        // ----------------------------------------------

        Time.timeScale = 1f;


        /*
         * Wait one frame so the death routine
         * can finish cleanly.
         */
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
    // AFTER LEVEL RELOAD
    // ==================================================

    private void StartGameplayAfterReload()
    {
        gameStarted = true;
        menuOpen = false;


        // ----------------------------------------------
        // RESTORE GAME AUDIO
        // ----------------------------------------------

        ResumeGameAudio();


        // ----------------------------------------------
        // START GAME
        // ----------------------------------------------

        Time.timeScale = 1f;


        // ----------------------------------------------
        // BUTTON STATE
        // ----------------------------------------------

        SetMenuButtons(
            showStart: false,
            showPause: true,
            showReset: true
        );


        // ----------------------------------------------
        // HIDE MAIN MENU
        // ----------------------------------------------

        SetCanvasImmediate(
            mainMenuGroup,
            false
        );


        // ----------------------------------------------
        // HIDE SETTINGS
        // ----------------------------------------------

        SetCanvasImmediate(
            settingsMenuGroup,
            false
        );


        // ----------------------------------------------
        // HIDE BACKGROUND
        // ----------------------------------------------

        SetCanvasImmediate(
            backgroundGroup,
            false
        );


        // ----------------------------------------------
        // MENU AUDIO OFF
        // ----------------------------------------------

        StopMenuAudio();


        // ----------------------------------------------
        // START LEVEL MUSIC FROM BEGINNING
        // ----------------------------------------------

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
    // SETTINGS
    // ==================================================

    private void OpenSettings()
    {
        if (isTransitioning)
            return;


        StartCoroutine(
            OpenSettingsRoutine()
        );
    }


    private IEnumerator OpenSettingsRoutine()
    {
        // Hide main menu.
        yield return FadeCanvas(
            mainMenuGroup,
            false
        );


        // Show settings.
        yield return FadeCanvas(
            settingsMenuGroup,
            true
        );
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
        // Hide settings.
        yield return FadeCanvas(
            settingsMenuGroup,
            false
        );


        // Return to menu.
        yield return FadeCanvas(
            mainMenuGroup,
            true
        );
    }


    private bool IsSettingsOpen()
    {
        return
            settingsMenuGroup != null &&
            settingsMenuGroup.alpha > 0.5f;
    }


    // ==================================================
    // MAIN MENU FADE
    // ==================================================

    private IEnumerator FadeCanvas(
        CanvasGroup group,
        bool show)
    {
        if (group == null)
            yield break;


        isTransitioning = true;


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


        float elapsed = 0f;


        /*
         * Unscaled time is required because
         * Time.timeScale may be 0.
         */
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


        isTransitioning = false;
    }


    // ==================================================
    // BACKGROUND FADE
    // ==================================================

    private void FadeBackground(
        bool show)
    {
        if (backgroundGroup == null)
            return;


        /*
         * Stop previous fade if one is
         * currently running.
         */
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


        backgroundFadeRoutine = null;
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
         * Pause every normal AudioSource.
         *
         * Level music pauses here too.
         */
        AudioListener.pause = true;


        /*
         * Menu music is the exception.
         */
        if (menuAudioSource != null)
        {
            menuAudioSource.ignoreListenerPause =
                true;
        }
    }


    private void ResumeGameAudio()
    {
        /*
         * Resume every AudioSource that was
         * paused by AudioListener.pause.
         */
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
    // LEVEL MUSIC
    // ==================================================

    private void PlayLevelMusic()
    {
        if (levelMusicSource == null)
            return;


        /*
         * Level music must follow the
         * normal AudioListener pause state.
         */
        levelMusicSource.ignoreListenerPause =
            false;


        /*
         * Only start if not already playing.
         */
        if (!levelMusicSource.isPlaying)
        {
            levelMusicSource.Play();
        }
    }


    private void StopLevelMusic()
    {
        if (levelMusicSource == null)
            return;


        /*
         * Stop resets the song back to 0.
         *
         * This is what we want for:
         *
         * Reset Level
         * Player Death
         */
        levelMusicSource.Stop();
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        /*
         * Never leave application frozen.
         */
        Time.timeScale = 1f;


        /*
         * Never leave application audio paused.
         */
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