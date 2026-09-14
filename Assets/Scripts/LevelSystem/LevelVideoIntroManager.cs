using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

public class LevelVideoIntroManager : MonoBehaviour
{
    // INSTANCE
    public static LevelVideoIntroManager Instance { get; private set; }

    // INTERNAL MODES
    private enum PendingLoadMode { None, AlreadyLoadedLevel, NormalLevelLoad, LevelMenuLoad }
    private enum VideoFlowMode { None, LevelIntro, GameEnding }


    // LEVEL VIDEOS

    [Header("Level Videos")]
    [SerializeField]
    private LevelVideoEntry[] levelVideos =
    {
        new LevelVideoEntry { sceneName = "Level_01" },
        new LevelVideoEntry { sceneName = "Level_02" },
        new LevelVideoEntry { sceneName = "Level_03" },
        new LevelVideoEntry { sceneName = "Level_04" },
        new LevelVideoEntry { sceneName = "Level_05" },
        new LevelVideoEntry { sceneName = "Level_06" },
        new LevelVideoEntry { sceneName = "Level_07" },
        new LevelVideoEntry { sceneName = "Level_Final" }
    };


    // GAME ENDING

    [Header("Game Ending")]
    [Tooltip("Video shown AFTER the player finishes Level_Final.")]
    [SerializeField] private VideoClip endingVideoClip;

    [Tooltip("If enabled, the Skip button is visible during the ending video. Skipping does NOT return immediately; it reveals the final menu button.")]
    [SerializeField] private bool allowEndingSkip = true;

    [Tooltip("Reset level unlock progression after the ending button is pressed.")]
    [SerializeField] private bool resetProgressAfterEnding = true;

    [Tooltip("Reset watched intro-video history after the ending button is pressed. Recommended so a new game shows all level intro videos again.")]
    [SerializeField] private bool resetIntroVideoHistoryAfterEnding = true;


    // ENDING PRESENTATION
    [Header("Ending Presentation")]

    [Tooltip("Root shown AFTER the final ending video finishes or is skipped. Recommended hierarchy: EndingScreen -> EndingBackground + Continue_End_btn.")]
    [SerializeField] private GameObject endingScreenRoot;

    [Tooltip("RawImage inside EndingScreen that displays the LOOPED ending background video.")]
    [SerializeField] private RawImage endingBackgroundVideoRawImage;

    [Tooltip("Dedicated VideoPlayer used only for the LOOPED ending background.")]
    [SerializeField] private VideoPlayer endingBackgroundVideoPlayer;

    [Tooltip("VideoClip that loops behind Continue_End_btn after the final ending video finishes.")]
    [SerializeField] private VideoClip endingBackgroundVideoClip;

    [Tooltip("Dedicated AudioSource for the ending music. Use a separate AudioSource from both VideoPlayer AudioSources.")]
    [SerializeField] private AudioSource endingMusicSource;


    [Tooltip("Music played after the ending video finishes. It keeps playing until Continue_End_btn returns to the main menu.")]
    [SerializeField] private AudioClip endingMusicClip;

    [SerializeField, Range(0f, 1f)] private float endingMusicVolume = 0.7f;


    [Tooltip("Loop the ending-screen music.")]
    [SerializeField] private bool loopEndingMusic = true;

    [Tooltip("ON = preserve the old Continue_End countdown/auto-return behavior. OFF = keep the ending screen and looping music visible until the player clicks the button.")]
    [SerializeField] private bool useContinueEndCountdown = false;

    [Tooltip("If ON, the manager automatically prefers a Continue_End_btn that is a CHILD of EndingScreen. This prevents the old duplicate button under Controls from being shown by mistake.")]
    [SerializeField] private bool autoResolveEndingButtonInsideEndingScreen = true;


    // UI

    [Header("UI")]

    [Tooltip("Root GameObject of LevelVideoIntro_Menu.")]
    [SerializeField] private GameObject videoMenuRoot;


    [Tooltip("CanvasGroup on LevelVideoIntro_Menu.")]
    [SerializeField] private CanvasGroup videoMenuCanvasGroup;


    [Tooltip("RawImage that displays the RenderTexture used by VideoPlayer.")]
    [SerializeField] private RawImage videoRawImage;

    [SerializeField] private Button skipButton;


    [Tooltip("Button shown after a normal level-intro video finishes. Keep its Text/TMP label as: دخول المرحلة")]
    [SerializeField] private Button continueButton;


    [Tooltip("Button shown only after the final game-ending video finishes or is skipped.  Keep its Text/TMP label as: العودة إلى القائمة الرئيسية")]
    [SerializeField] private Button continueEndButton;


    // BUTTON TIMING
    [Header("Button Timing")]

    [Tooltip("Delay before Skip_btn becomes visible after the video actually starts playing.")]
    [SerializeField, Min(0f)] private float skipAppearDelay = 2f;


    [Tooltip("Countdown after Continue_btn appears. At 0, the button's normal OnClick is invoked automatically.")]
    [SerializeField, Min(0f)] private float continueCountdownDuration = 5f;


    [Tooltip("Countdown after Continue_End_btn appears. At 0, the button's normal OnClick is invoked automatically.")]
    [SerializeField, Min(0f)] private float continueEndCountdownDuration = 5f;


    [Tooltip("If enabled, reaching 0 automatically invokes the same Button.onClick as a manual click.")]
    [SerializeField] private bool autoContinueWhenCountdownEnds = true;


    [Header("Countdown Text")]

    [Tooltip("TMP text placed behind/under Continue_btn. Displays 5, 4, 3, 2, 1, 0.")]
    [SerializeField] private TMP_Text continueCountdownText;


    [Tooltip("TMP text placed behind/under Continue_End_btn. Displays 5, 4, 3, 2, 1, 0.")]
    [SerializeField] private TMP_Text continueEndCountdownText;


    // VIDEO PLAYER
    [Header("Video Player")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Tooltip("AudioSource used by VideoPlayer.")]
    [SerializeField] private AudioSource videoAudioSource;


    // VIDEO AUDIO
    [Header("Video Audio")]
    [SerializeField, Range(0f, 1f)] private float videoVolume = 1f;
    private const string VideoVolumeKey = "Settings.LevelIntroVideoVolume";


    // LEVEL 1
    [Header("Initial Already-Loaded Level")]

    [Tooltip("Bootstrap already loads this level behind the Start Menu.")]
    [SerializeField] private string initialLoadedSceneName = "Level_01";


    [Tooltip("Connect UIManager.StartGameAfterIntroVideo here.")]
    [SerializeField] private UnityEvent onAlreadyLoadedLevelReady;


    // PLAY ONCE / SAVE

    [Header("Play Once")]

    [Tooltip("Each level intro is shown only once. Reset Progress clears this watched history so the intros can play again on a new game.")]
    [SerializeField] private string watchedKeyPrefix = "WakeInTheDark.LevelIntroVideoSeen.";


    // OPTIONS

    [Header("Options")]

    [Tooltip("If a level has no intro video, load the level directly.")]
    [SerializeField] private bool continueIfVideoMissing = true;


    [Tooltip("Disable the current PlayerMovement while a video is open.")]
    [SerializeField] private bool disableCurrentPlayerControlsDuringVideo = true;


    [Tooltip(
        "When the requested level is ALREADY LOADED (Level_01 behind Bootstrap), " +
        "start that level's music and keep it playing underneath the Level Video Intro. " +
        "Later levels still start their music after their scene is actually loaded."
    )]
    [SerializeField] private bool playLoadedLevelMusicDuringIntro = true;


    [SerializeField] private bool debugLogs = true;

    // REFERENCES
    [Header("References")]
    [SerializeField] private LevelLoader levelLoader;

    // STATE

    private PendingLoadMode pendingLoadMode = PendingLoadMode.None;
    private VideoFlowMode videoFlowMode = VideoFlowMode.None;
    private string pendingSceneName;
    private bool isBusy;
    private bool videoFinished;
    private PlayerMovement disabledPlayer;
    private bool disabledPlayerHadControls;
    private Coroutine skipAppearRoutine;
    private Coroutine continueCountdownRoutine;

    /*
     * True while an old level must remain silent during video playback and the following additive scene load.
     */
    private bool keepWorldAudioMutedDuringLevelLoad;


    // PUBLIC STATE

    public bool IsBusy => isBusy;
    public string PendingSceneName => pendingSceneName;
    public float VideoVolume => GameAudioMixerSettings.Instance != null ? GameAudioMixerSettings.Instance.VideoVolume : videoVolume;
    public bool IsPlayingGameEnding => isBusy && videoFlowMode == VideoFlowMode.GameEnding;
    public bool KeepWorldAudioMutedDuringLevelLoad => keepWorldAudioMutedDuringLevelLoad;


    // AWAKE

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveLevelLoader();
        ResolveVideoMenuCanvasGroup();

        videoVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VideoVolumeKey, videoVolume));

        ConfigureVideoPlayer();
        ResolveEndingPresentationReferences();
        ConfigureEndingBackgroundVideoPlayer();
        ConfigureButtons();
        ConfigureCountdownTexts();
        ConfigureEndingPresentation();
        HideVideoMenuImmediate();
    }

    // EVENTS
    private void OnEnable()
    {
        SubscribeVideoEvents();
    }


    private void OnDisable()
    {
        UnsubscribeVideoEvents();
        StopButtonTimers();
        StopEndingBackgroundVideo();
        StopEndingMusic();
    }

    // TRANSITION AUDIO ISOLATION
    private void BeginTransitionAudioIsolation(bool keepLoadedLevelMusicPlaying = false)
    {
        keepWorldAudioMutedDuringLevelLoad = true;

        /*
         * Clean transition rule:
         * 1. Stop MusicSourceA / MusicSourceB.
         * 2. Stop every currently loaded AudioSource:
         *  ==> old level ambience, traps, platforms, levers, Timeline audio, Bootstrap menu music.
         * 3. EXCEPT the VideoPlayer AudioSource.
         * 4. Keep AudioListener paused while the video / additive level transition is happening.
         * The VideoPlayer AudioSource has ignoreListenerPause = true, so only video audio remains audible.
         */
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.BeginVideoTransition(videoAudioSource, keepLoadedLevelMusicPlaying);

            return;
        }


        /*
         * Fallback if AudioManager is missing.
         * UIManager still knows how to stop menu audio and pause normal game/world audio.
         */
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PrepareForVideoPlayback();
            return;
        }


        AudioListener.pause = true;
    }


    public void NotifyLevelLoadFinished()
    {
        keepWorldAudioMutedDuringLevelLoad = false;
    }


    private void RestoreAudioAfterCancelledFlow()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.CancelVideoTransitionSilence(false);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RestoreAudioAfterCancelledVideo();
            return;
        }

        AudioListener.pause = false;
    }



    private void SubscribeVideoEvents()
    {
        if (videoPlayer == null)
            return;

        UnsubscribeVideoEvents();

        videoPlayer.prepareCompleted += HandleVideoPrepared;
        videoPlayer.loopPointReached += HandleVideoFinished;
        videoPlayer.errorReceived += HandleVideoError;
    }


    private void UnsubscribeVideoEvents()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.prepareCompleted -= HandleVideoPrepared;
        videoPlayer.loopPointReached -= HandleVideoFinished;
        videoPlayer.errorReceived -= HandleVideoError;
    }


    // BUTTONS

    private void ConfigureButtons()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipVideo);
            skipButton.onClick.AddListener(SkipVideo);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ContinueToLevel);
            continueButton.onClick.AddListener(ContinueToLevel);
        }

        if (continueEndButton != null)
        {
            continueEndButton.onClick.RemoveListener(ContinueAfterGameEnding);
            continueEndButton.onClick.AddListener(ContinueAfterGameEnding);
        }
    }


    // LEVEL 1

    public void RequestInitialLoadedLevel()
    {
        RequestLevelVideo(initialLoadedSceneName, PendingLoadMode.AlreadyLoadedLevel);
    }


    // START MENU / CONTINUE GAME
    /*
     * Used by UIManager's "Start Game" button.
     *
     * If the saved continue level is already loaded behind the menu,
     * use AlreadyLoadedLevel so the scene is not reloaded.
     *
     * If the saved continue level is a later level, play its intro
     * video first (when applicable), then let LevelLoader replace the
     * currently loaded level safely.
     */
    public void RequestStartLevel(string sceneName)
    {
        if (isBusy || string.IsNullOrWhiteSpace(sceneName))
            return;


        ResolveLevelLoader();


        bool targetIsAlreadyLoaded = levelLoader != null && !string.IsNullOrWhiteSpace(levelLoader.CurrentLevelSceneName) && string.Equals(levelLoader.CurrentLevelSceneName, sceneName, System.StringComparison.OrdinalIgnoreCase);


        PendingLoadMode mode = targetIsAlreadyLoaded ? PendingLoadMode.AlreadyLoadedLevel : PendingLoadMode.NormalLevelLoad;


        RequestLevelVideo(sceneName, mode);
    }


    // NORMAL LEVEL REQUEST

    public void RequestLevel(string sceneName)
    {
        RequestLevelVideo(sceneName, PendingLoadMode.NormalLevelLoad);
    }


    // LEVEL MENU REQUEST

    public void RequestLevelFromMenu(string sceneName)
    {
        RequestLevelVideo(sceneName, PendingLoadMode.LevelMenuLoad);
    }


    // LEVEL VIDEO REQUEST

    private void RequestLevelVideo(string sceneName, PendingLoadMode loadMode)
    {
        if (isBusy || string.IsNullOrWhiteSpace(sceneName))
            return;

        pendingSceneName = sceneName;
        pendingLoadMode = loadMode;
        videoFlowMode = VideoFlowMode.LevelIntro;
        videoFinished = false;


        // ----------------------------------------------
        // REPLAY
        // ----------------------------------------------

        /*
         * Every level intro is permanently one-time until Reset Progress.
         *
         * Death/reload, revisiting a level, and reopening the game all
         * skip an intro that has already been completed or skipped.
         */
        if (HasSeenVideo(sceneName))
        {
            BeginTransitionAudioIsolation();
            CompleteLevelIntroRequest();
            return;
        }


        LevelVideoEntry entry = FindEntry(sceneName);


        // ----------------------------------------------
        // MISSING VIDEO
        // ----------------------------------------------

        if (entry == null || entry.videoClip == null)
        {
            if (!continueIfVideoMissing)
            {
                ClearRequest();
                return;
            }

            BeginTransitionAudioIsolation();
            CompleteLevelIntroRequest();
            return;
        }


        bool keepLoadedLevelMusic =
            false;


        if (playLoadedLevelMusicDuringIntro && loadMode == PendingLoadMode.AlreadyLoadedLevel && AudioManager.Instance != null)
            keepLoadedLevelMusic = AudioManager.Instance.StartLoadedLevelMusicForIntro(sceneName);

        BeginTransitionAudioIsolation(keepLoadedLevelMusic);

        StartVideoFlow(entry.videoClip);
    }


    // GAME ENDING

    public void PlayGameEnding()
    {
        if (isBusy)
            return;


        pendingSceneName = null;
        pendingLoadMode = PendingLoadMode.None;
        videoFlowMode = VideoFlowMode.GameEnding;
        videoFinished = false;


        BeginTransitionAudioIsolation();


        if (endingVideoClip == null)
        {
            isBusy = true;
            videoFinished = true;

            DisableCurrentPlayerControls();
            ShowVideoMenu();
            ShowEndingPresentation();

            return;
        }


        StartVideoFlow(endingVideoClip);

    }


    // START VIDEO FLOW

    private void StartVideoFlow(VideoClip clip)
    {
        if (videoPlayer == null)
        {
            if (videoFlowMode == VideoFlowMode.GameEnding)
            {
                isBusy = true;
                videoFinished = true;

                DisableCurrentPlayerControls();
                ShowVideoMenu();
                ShowEndingPresentation();
            }
            else if (continueIfVideoMissing)
            {
                CompleteLevelIntroRequest();
            }
            else
            {
                ClearRequest();
            }

            return;
        }

        isBusy = true;

        DisableCurrentPlayerControls();
        HideEndingPresentationImmediate();
        ShowVideoMenu();
        SetPlayingButtonState();

        videoPlayer.Stop();

        /*
         * Set source mode before audio routing. Changing VideoPlayer.source
         * can reset audio-control state on some Unity versions.
         */
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;

        /*
         * IMPORTANT: configure controlled audio tracks BEFORE Prepare().
         */
        ConfigureVideoAudioRouting();

        videoPlayer.Prepare();
    }


    // VIDEO PREPARED

    private void HandleVideoPrepared(VideoPlayer source)
    {
        if (!isBusy || source != videoPlayer)
            return;

        ConfigurePreparedVideoAudio(source);


        source.Play();

        /*
         * Skip timing begins when the actual video starts,
         * not while VideoPlayer is still preparing.
         */
        StartSkipAppearTimer();
    }


    // VIDEO FINISHED

    private void HandleVideoFinished(VideoPlayer source)
    {
        if (!isBusy || source != videoPlayer)
            return;

        videoFinished = true;


        if (videoFlowMode == VideoFlowMode.GameEnding)
        {
            ShowEndingPresentation();
            return;
        }


        /*
         * Normal level intro:
         * keep the final video frame visible and show Continue.
         */
        ShowFinishedButton();

    }


    // VIDEO ERROR

    private void HandleVideoError(VideoPlayer source, string message)
    {
        if (!isBusy || source != videoPlayer)
            return;
        /*
         * Never block progression because a video failed.
         */
        videoFinished = true;


        if (videoFlowMode == VideoFlowMode.GameEnding)
        {
            ShowEndingPresentation();
            return;
        }


        ShowFinishedButton();
    }


    // SKIP

    public void SkipVideo()
    {
        if (!isBusy)
            return;

        StopSkipAppearTimer();

        if (videoPlayer != null)
            videoPlayer.Stop();

        // ----------------------------------------------
        // ENDING:
        // Skip -> reveal final button.
        // ----------------------------------------------

        if (videoFlowMode == VideoFlowMode.GameEnding)
        {
            videoFinished = true;
            ShowEndingPresentation();
            return;
        }

        // ----------------------------------------------
        // LEVEL INTRO:
        // Skip -> enter level immediately.
        // ----------------------------------------------

        CompleteLevelIntroRequest();
    }


    // CONTINUE BUTTON

    public void ContinueToLevel()
    {
        if (!isBusy || !videoFinished || videoFlowMode != VideoFlowMode.LevelIntro)
            return;

        StopContinueCountdown();

        CompleteLevelIntroRequest();
    }


    public void ContinueAfterGameEnding()
    {
        if (!isBusy || !videoFinished || videoFlowMode != VideoFlowMode.GameEnding)
            return;

        StopContinueCountdown();

        CompleteGameEnding();
    }


    // COMPLETE LEVEL INTRO

    private void CompleteLevelIntroRequest()
    {
        string sceneName = pendingSceneName;
        PendingLoadMode mode = pendingLoadMode;

        if (!string.IsNullOrWhiteSpace(sceneName))
            MarkVideoSeen(sceneName);


        StopVideoAndHideMenu();


        isBusy = false;
        pendingSceneName = null;
        pendingLoadMode = PendingLoadMode.None;
        videoFlowMode = VideoFlowMode.None;
        videoFinished = false;


        ResolveLevelLoader();


        switch (mode)
        {
            // ------------------------------------------
            // LEVEL 1 IS ALREADY LOADED
            // ------------------------------------------
            case PendingLoadMode.AlreadyLoadedLevel:
                /*
                 * Level_01 is already loaded, so there is no scene transition that needs to stay muted.
                 * UIManager.StartGameAfterIntroVideo() will resume gameplay audio and start Level_01 music.
                 */
                keepWorldAudioMutedDuringLevelLoad = false;
                RestoreDisabledPlayerControlsIfNeeded();
                onAlreadyLoadedLevelReady?.Invoke();
                break;


            // ------------------------------------------
            // NORMAL EXIT DOOR
            // ------------------------------------------
            case PendingLoadMode.NormalLevelLoad:
                if (levelLoader == null)
                {
                    LevelLoaderFinishes();
                    return;
                }
                /*
                 * Keep the old level silent until LevelLoader finishes loading the new level. UIManager.OnLevelLoadFinished()
                 * resumes audio and starts the new level music.
                 */
                keepWorldAudioMutedDuringLevelLoad = true;
                levelLoader.LoadLevel(sceneName);
                ClearDisabledPlayerReference();
                break;


            // ------------------------------------------
            // LEVEL MENU
            // ------------------------------------------
            case PendingLoadMode.LevelMenuLoad:
                if (levelLoader == null)
                {
                    LevelLoaderFinishes();
                    return;
                }
                keepWorldAudioMutedDuringLevelLoad = true;
                levelLoader.LoadLevelFromMenu(sceneName);
                ClearDisabledPlayerReference();
                break;


            default:
                keepWorldAudioMutedDuringLevelLoad = false;
                RestoreDisabledPlayerControlsIfNeeded();
                RestoreAudioAfterCancelledFlow();
                break;
        }
    }


    private void LevelLoaderFinishes()
    {
        keepWorldAudioMutedDuringLevelLoad = false;
        RestoreDisabledPlayerControlsIfNeeded();
        RestoreAudioAfterCancelledFlow();
    }


    // COMPLETE GAME ENDING

    private void CompleteGameEnding()
    {
        StopVideoAndHideMenu();

        /*
         * We are leaving Level_Final, so there is no need
         * to re-enable that Player before its scene is unloaded.
         */
        ClearDisabledPlayerReference();

        isBusy = false;
        pendingSceneName = null;
        pendingLoadMode = PendingLoadMode.None;
        videoFlowMode = VideoFlowMode.None;
        videoFinished = false;

        /*
         * Keep Level_Final silent while it is unloaded and Level_01 is loaded back behind the Bootstrap menu.
         */
        keepWorldAudioMutedDuringLevelLoad = true;

        // ----------------------------------------------
        // RESET NEW-GAME PROGRESSION
        // ----------------------------------------------

        bool didFullProgressReset = false;


        if (resetProgressAfterEnding && LevelProgressManager.Instance != null)
        {
            /*
             * ResetProgress now performs the complete new-game reset:
             * all PlayerPrefs, story history, intro history and settings.
             */
            LevelProgressManager.Instance.ResetProgress();

            didFullProgressReset = true;
        }


        // ----------------------------------------------
        // RESET INTRO VIDEO HISTORY
        // ----------------------------------------------

        /*
         * Only run the targeted intro reset if a full PlayerPrefs reset
         * was NOT already performed above.
         */
        if (resetIntroVideoHistoryAfterEnding && !didFullProgressReset)
            ResetAllWatchedVideos();

        // ----------------------------------------------
        // RETURN TO BOOTSTRAP MAIN MENU
        // ----------------------------------------------

        if (UIManager.Instance != null)
            UIManager.Instance.ReturnToMainMenuAfterGameEnding();
    }


    // PLAYER CONTROL

    private void DisableCurrentPlayerControls()
    {
        ClearDisabledPlayerReference();

        if (!disableCurrentPlayerControlsDuringVideo)
            return;

        disabledPlayer = FindAnyObjectByType<PlayerMovement>();
        if (disabledPlayer == null)
            return;

        disabledPlayerHadControls = disabledPlayer.ControlsEnabled;

        if (disabledPlayerHadControls)
            disabledPlayer.DisableControls();
    }


    private void RestoreDisabledPlayerControlsIfNeeded()
    {
        if (disabledPlayer != null && disabledPlayerHadControls)
            disabledPlayer.EnableControls();

        ClearDisabledPlayerReference();
    }


    private void ClearDisabledPlayerReference()
    {
        disabledPlayer = null;
        disabledPlayerHadControls = false;
    }

    // VIDEO LOOKUP
    private LevelVideoEntry FindEntry(string sceneName)
    {
        if (levelVideos == null)
            return null;

        foreach (LevelVideoEntry entry in levelVideos)
        {
            if (entry == null)
                continue;

            if (string.Equals(entry.sceneName, sceneName, System.StringComparison.OrdinalIgnoreCase))
                return entry;
        }

        return null;
    }


    // WATCHED SAVE

    public bool HasSeenVideo(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        return PlayerPrefs.GetInt(GetWatchedKey(sceneName), 0) == 1;
    }


    private void MarkVideoSeen(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        PlayerPrefs.SetInt(GetWatchedKey(sceneName), 1);
        PlayerPrefs.Save();
    }


    private string GetWatchedKey(string sceneName)
    {
        return watchedKeyPrefix + sceneName;
    }


    public void ResetAllWatchedVideos()
    {
        if (levelVideos != null)
        {
            foreach (LevelVideoEntry entry in levelVideos)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.sceneName))
                    continue;

                PlayerPrefs.DeleteKey(GetWatchedKey(entry.sceneName));
            }
        }

        PlayerPrefs.Save();
    }


    // -------------- =================================
    // ------------ --- for reset watched Videos --- ------------
    [ContextMenu("DEBUG - Reset Watched Videos")]
    private void DebugResetWatchedVideos()
    {
        ResetAllWatchedVideos();
    }


    [ContextMenu("DEBUG - Forget Level 1 Intro Video")]
    private void DebugForgetLevelOneVideo()
    {
        if (string.IsNullOrWhiteSpace(initialLoadedSceneName))
            return;
        PlayerPrefs.DeleteKey(GetWatchedKey(initialLoadedSceneName));
        PlayerPrefs.Save();
    }


    // VIDEO AUDIO

    public void SetVideoVolume(float value)
    {
        videoVolume = Mathf.Clamp01(value);


        if (GameAudioMixerSettings.Instance != null)
        {
            /*
             * Mixer owns the user's VideoVolume.
             * Keep the VideoPlayer AudioSource at full/base volume
             * so the setting is not applied twice.
             */
            GameAudioMixerSettings.Instance.SetVideoVolume(videoVolume);
        }
        else
        {
            PlayerPrefs.SetFloat(VideoVolumeKey, videoVolume);
            PlayerPrefs.Save();
        }


        ApplyVideoVolume();
    }


    public void MuteVideoAudio()
    {
        SetVideoVolume(0f);
    }


    public void SetVideoAudioFullVolume()
    {
        SetVideoVolume(1f);
    }


    private void ApplyVideoVolume()
    {
        if (videoAudioSource == null)
            return;


        /*
         * Mixer setup:
         * source stays at 1 and Video_Audio mixer group applies
         * the user's saved volume.
         *
         * Fallback:
         * old direct AudioSource behavior remains available if
         * GameAudioMixerSettings is missing.
         */
        videoAudioSource.volume = GameAudioMixerSettings.Instance != null ? 1f : videoVolume;
    }


    // UI

    private void ShowVideoMenu()
    {
        if (videoMenuRoot != null)
            videoMenuRoot.SetActive(true);

        ResolveVideoMenuCanvasGroup();

        if (videoMenuCanvasGroup != null)
        {
            videoMenuCanvasGroup.alpha = 1f;
            videoMenuCanvasGroup.interactable = true;
            videoMenuCanvasGroup.blocksRaycasts = true;
        }


        if (videoRawImage != null)
            videoRawImage.enabled = true;


        if (GameCursorManager.Instance != null)
            GameCursorManager.Instance.ShowMenuCursor();
    }


    private void SetPlayingButtonState()
    {
        StopButtonTimers();
        HideCountdownTexts();

        /*
         * Skip starts hidden.
         * HandleVideoPrepared() starts the 2-second (configurable)
         * reveal timer when the video actually begins playing.
         */
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.interactable = false;
        }

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (continueEndButton != null)
            continueEndButton.gameObject.SetActive(false);
    }


    private void ShowFinishedButton()
    {
        StopSkipAppearTimer();


        if (videoFlowMode == VideoFlowMode.GameEnding)
        {
            ShowEndingPresentation();
            return;
        }


        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.interactable = false;
        }


        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
        }


        if (continueEndButton != null)
        {
            continueEndButton.gameObject.SetActive(false);
            continueEndButton.interactable = false;
        }


        StartContinueCountdown(false);
    }


    // ENDING PRESENTATION

    private void ConfigureEndingPresentation()
    {
        ResolveEndingPresentationReferences();
        ConfigureEndingBackgroundVideoPlayer();


        if (endingMusicSource != null)
        {
            endingMusicSource.playOnAwake = false;

            endingMusicSource.loop = loopEndingMusic;

            endingMusicSource.ignoreListenerPause = true;

            endingMusicSource.volume = endingMusicVolume;
        }


        HideEndingPresentationImmediate();
    }


    private void ResolveEndingPresentationReferences()
    {
        if (endingScreenRoot == null)
            return;


        if (endingBackgroundVideoRawImage == null)
        {
            RawImage[] rawImages = endingScreenRoot.GetComponentsInChildren<RawImage>(true);

            if (rawImages != null && rawImages.Length > 0)
                endingBackgroundVideoRawImage = rawImages[0];
        }


        if (autoResolveEndingButtonInsideEndingScreen)
        {
            bool currentButtonIsInsideEndingScreen = continueEndButton != null && continueEndButton.transform.IsChildOf(endingScreenRoot.transform);


            if (!currentButtonIsInsideEndingScreen)
            {
                Button[] buttons = endingScreenRoot.GetComponentsInChildren<Button>(true);
                Button preferredButton = null;

                foreach (Button button in buttons)
                {
                    if (button == null)
                        continue;

                    if (button.name == "Continue_End_btn")
                    {
                        preferredButton = button;
                        break;
                    }

                    if (preferredButton == null)
                        preferredButton = button;
                }

                if (preferredButton != null)
                    continueEndButton = preferredButton;
            }
        }
    }


    private void ConfigureEndingBackgroundVideoPlayer()
    {
        if (endingBackgroundVideoPlayer == null)
            return;


        endingBackgroundVideoPlayer.playOnAwake = false;
        endingBackgroundVideoPlayer.isLooping = true;
        endingBackgroundVideoPlayer.waitForFirstFrame = true;
        endingBackgroundVideoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;

        /*
         * Ending background video is VISUAL ONLY.
         * Final ending music is handled by endingMusicSource.
         */
        endingBackgroundVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;


        if (endingBackgroundVideoClip != null)
            endingBackgroundVideoPlayer.clip = endingBackgroundVideoClip;


        /*
         * If the VideoPlayer already has a target RenderTexture,
         * automatically display it in the RawImage.
         */
        if (endingBackgroundVideoRawImage != null)
        {
            endingBackgroundVideoRawImage.raycastTarget = false;


            if (endingBackgroundVideoPlayer.targetTexture != null)
                endingBackgroundVideoRawImage.texture = endingBackgroundVideoPlayer.targetTexture;

        }
    }


    private void ShowEndingPresentation()
    {
        StopSkipAppearTimer();
        StopContinueCountdown();


        ResolveEndingPresentationReferences();
        ConfigureEndingBackgroundVideoPlayer();


        /*
         * Stop the FINAL story video first.
         */
        if (videoPlayer != null)
            videoPlayer.Stop();


        if (videoRawImage != null)
            videoRawImage.enabled = false;

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.interactable = false;
        }


        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.interactable = false;
        }


        /*
         * Show EndingScreen BEFORE enabling its children.
         */
        if (endingScreenRoot != null)
            endingScreenRoot.SetActive(true);


        /*
         * Start the LOOPED ending background video.
         */
        PlayEndingBackgroundVideo();


        /*
         * Continue_End_btn must render ABOVE the RawImage.
         */
        if (continueEndButton != null)
        {
            continueEndButton.gameObject.SetActive(true);
            continueEndButton.interactable = true;
            continueEndButton.transform.SetAsLastSibling();
        }

        PlayEndingMusic();

        if (useContinueEndCountdown)
            StartContinueCountdown(true);
        else
            SetCountdownText(continueEndCountdownText, 0, false);

    }


    private void PlayEndingBackgroundVideo()
    {
        if (endingBackgroundVideoRawImage != null)
        {
            endingBackgroundVideoRawImage.gameObject.SetActive(true);
            endingBackgroundVideoRawImage.enabled = true;
            endingBackgroundVideoRawImage.raycastTarget = false;
        }


        if (endingBackgroundVideoPlayer == null || endingBackgroundVideoClip == null)
            return;


        endingBackgroundVideoPlayer.Stop();
        endingBackgroundVideoPlayer.clip = endingBackgroundVideoClip;
        endingBackgroundVideoPlayer.isLooping = true;
        endingBackgroundVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;


        if (endingBackgroundVideoPlayer.targetTexture != null && endingBackgroundVideoRawImage != null)
            endingBackgroundVideoRawImage.texture = endingBackgroundVideoPlayer.targetTexture;



        endingBackgroundVideoPlayer.Play();
    }


    private void StopEndingBackgroundVideo()
    {
        if (endingBackgroundVideoPlayer != null)
            endingBackgroundVideoPlayer.Stop();



        if (endingBackgroundVideoRawImage != null)
            endingBackgroundVideoRawImage.enabled = false;

    }


    private void HideEndingPresentationImmediate()
    {
        StopEndingBackgroundVideo();
        StopEndingMusic();


        if (continueEndButton != null)
        {
            continueEndButton.gameObject.SetActive(false);
            continueEndButton.interactable = false;
        }


        if (endingScreenRoot != null)
            endingScreenRoot.SetActive(false);

    }


    private void PlayEndingMusic()
    {
        if (endingMusicSource == null || endingMusicClip == null)
            return;

        endingMusicSource.Stop();
        endingMusicSource.clip = endingMusicClip;
        endingMusicSource.loop = loopEndingMusic;
        endingMusicSource.playOnAwake = false;
        endingMusicSource.ignoreListenerPause = true;
        endingMusicSource.volume = endingMusicVolume;
        endingMusicSource.Play();
    }


    private void StopEndingMusic()
    {
        if (endingMusicSource == null)
            return;


        endingMusicSource.Stop();
    }


    // SKIP APPEAR TIMER

    private void StartSkipAppearTimer()
    {
        StopSkipAppearTimer();

        bool skipAllowed = videoFlowMode != VideoFlowMode.GameEnding || allowEndingSkip;

        if (!skipAllowed || !isBusy || videoFinished || skipButton == null)
            return;

        if (skipAppearDelay <= 0f)
        {
            ShowSkipButtonNow();
            return;
        }

        skipAppearRoutine = StartCoroutine(SkipAppearRoutine());
    }


    private IEnumerator SkipAppearRoutine()
    {
        float remaining = skipAppearDelay;

        while (remaining > 0f)
        {
            if (!isBusy || videoFinished)
            {
                skipAppearRoutine = null;
                yield break;
            }

            remaining -= Time.unscaledDeltaTime;

            yield return null;
        }

        skipAppearRoutine = null;

        if (!isBusy || videoFinished)
            yield break;

        ShowSkipButtonNow();
    }


    private void ShowSkipButtonNow()
    {
        bool skipAllowed = videoFlowMode != VideoFlowMode.GameEnding || allowEndingSkip;

        if (skipButton == null || !skipAllowed || !isBusy || videoFinished)
            return;

        skipButton.gameObject.SetActive(true);
        skipButton.interactable = true;
    }


    private void StopSkipAppearTimer()
    {
        if (skipAppearRoutine == null)
            return;

        StopCoroutine(skipAppearRoutine);
        skipAppearRoutine = null;
    }


    // CONTINUE COUNTDOWN

    private void StartContinueCountdown(bool isEnding)
    {
        StopContinueCountdown();
        HideCountdownTexts();

        Button targetButton = isEnding ? continueEndButton : continueButton;
        TMP_Text targetText = isEnding ? continueEndCountdownText : continueCountdownText;
        float duration = isEnding ? continueEndCountdownDuration : continueCountdownDuration;


        if (isEnding && !useContinueEndCountdown)
        {
            SetCountdownText(targetText, 0, false);
            return;
        }


        if (targetButton == null)
            return;

        if (duration <= 0f)
        {
            SetCountdownText(targetText, 0, true);

            if (autoContinueWhenCountdownEnds)
                targetButton.onClick.Invoke();

            return;
        }

        continueCountdownRoutine = StartCoroutine(ContinueCountdownRoutine(targetButton, targetText, duration, isEnding));
    }


    private IEnumerator ContinueCountdownRoutine(Button targetButton, TMP_Text targetText, float duration, bool isEnding)
    {
        float remaining = Mathf.Max(0f, duration);

        SetCountdownText(targetText, Mathf.CeilToInt(remaining), true);

        while (remaining > 0f)
        {
            if (!IsCountdownStillValid(targetButton, isEnding))
            {
                SetCountdownText(targetText, 0, false);

                continueCountdownRoutine = null;

                yield break;
            }

            yield return null;

            remaining -= Time.unscaledDeltaTime;

            int displaySeconds = Mathf.Max(0, Mathf.CeilToInt(remaining));

            SetCountdownText(targetText, displaySeconds, true);
        }

        /*
         * Let 0 render for one frame before auto-continuing.
         */
        SetCountdownText(targetText, 0, true);

        yield return null;

        if (!IsCountdownStillValid(targetButton, isEnding))
        {
            SetCountdownText(targetText, 0, false);
            continueCountdownRoutine = null;

            yield break;
        }

        /*
         * Clear the coroutine reference BEFORE invoking the Button.
         * ContinueToLevel / ContinueAfterGameEnding can safely call
         * StopContinueCountdown() without trying to stop this coroutine itself.
         */
        continueCountdownRoutine = null;

        SetCountdownText(targetText, 0, false);

        if (autoContinueWhenCountdownEnds)
        {
            /*
             * This is intentionally Button.onClick.Invoke().
             * It performs the exact same action as a real button click,
             * including LevelVideoIntroManager's runtime listener and
             * any additional Button OnClick events you add later.
             */
            targetButton.onClick.Invoke();
        }
    }


    private bool IsCountdownStillValid(Button targetButton, bool isEnding)
    {
        if (!isBusy || !videoFinished || targetButton == null || !targetButton.gameObject.activeInHierarchy)
            return false;

        if (isEnding)
            return videoFlowMode == VideoFlowMode.GameEnding;

        return
            videoFlowMode == VideoFlowMode.LevelIntro;
    }


    private void StopContinueCountdown()
    {
        if (continueCountdownRoutine != null)
        {
            StopCoroutine(continueCountdownRoutine);

            continueCountdownRoutine = null;
        }

        HideCountdownTexts();
    }


    private void StopButtonTimers()
    {
        StopSkipAppearTimer();
        StopContinueCountdown();
    }


    // COUNTDOWN TEXT

    private void ConfigureCountdownTexts()
    {
        if (continueCountdownText != null)
        {
            continueCountdownText.raycastTarget = false;

            continueCountdownText.gameObject.SetActive(false);
        }

        if (continueEndCountdownText != null)
        {
            continueEndCountdownText.raycastTarget = false;

            continueEndCountdownText.gameObject.SetActive(false);
        }
    }


    private void HideCountdownTexts()
    {
        if (continueCountdownText != null)
            continueCountdownText.gameObject.SetActive(false);

        if (continueEndCountdownText != null)
            continueEndCountdownText.gameObject.SetActive(false);
    }


    private void SetCountdownText(TMP_Text textComponent, int seconds, bool visible)
    {
        if (textComponent == null)
            return;

        textComponent.raycastTarget = false;
        textComponent.text = Mathf.Max(0, seconds).ToString();
        textComponent.gameObject.SetActive(visible);
    }


    private void StopVideoAndHideMenu()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        HideVideoMenuImmediate();
    }


    private void HideVideoMenuImmediate()
    {
        StopButtonTimers();
        HideCountdownTexts();
        HideEndingPresentationImmediate();

        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (continueEndButton != null)
            continueEndButton.gameObject.SetActive(false);


        ResolveVideoMenuCanvasGroup();

        if (videoMenuCanvasGroup != null)
        {
            videoMenuCanvasGroup.alpha = 0f;
            videoMenuCanvasGroup.interactable = false;
            videoMenuCanvasGroup.blocksRaycasts = false;
        }

        if (videoMenuRoot != null)
            videoMenuRoot.SetActive(false);
    }


    private void ResolveVideoMenuCanvasGroup()
    {
        if (videoMenuCanvasGroup != null || videoMenuRoot == null)
            return;

        videoMenuCanvasGroup = videoMenuRoot.GetComponent<CanvasGroup>();
    }


    // LEVEL LOADER

    private void ResolveLevelLoader()
    {
        if (levelLoader != null)
            return;

        levelLoader = LevelLoader.Instance;

        if (levelLoader == null)
            levelLoader = FindAnyObjectByType<LevelLoader>();
    }


    // VIDEO PLAYER CONFIG

    private void ConfigureVideoPlayer()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;


        /*
         * Bootstrap's menu uses Time.timeScale = 0.
         */
        videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;


        ConfigureVideoAudioRouting();
    }


    private void ConfigureVideoAudioRouting()
    {
        if (videoPlayer == null)
            return;


        /*
         * Direct mode bypasses the AudioSource and therefore bypasses
         * the Video_Audio AudioMixerGroup.
         */
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;


        /*
         * Unity only decodes/routes the audio tracks controlled by the
         * VideoPlayer. If this is 0, the embedded video audio is silent.
         *
         * Configure this BEFORE Prepare().
         */
        videoPlayer.controlledAudioTrackCount = 1;


        videoPlayer.EnableAudioTrack(0, true);


        if (videoAudioSource == null)
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    "LevelVideoIntroManager: Video Audio Source is not assigned.",
                    this
                );
            }

            return;
        }

        videoAudioSource.enabled = true;
        videoAudioSource.mute = false;
        videoAudioSource.playOnAwake = false;
        videoAudioSource.loop = false;
        videoAudioSource.spatialBlend = 0f;

        /*
         * World audio is paused during videos, but video audio must remain
         * audible.
         */
        videoAudioSource.ignoreListenerPause = true;


        videoPlayer.SetTargetAudioSource(0, videoAudioSource);


        ApplyVideoVolume();
    }


    private void ConfigurePreparedVideoAudio(
        VideoPlayer source)
    {
        if (source == null || source.audioTrackCount == 0 || videoAudioSource == null)
            return;


        source.audioOutputMode = VideoAudioOutputMode.AudioSource;


        source.EnableAudioTrack(0, true);
        source.SetTargetAudioSource(0, videoAudioSource);


        videoAudioSource.ignoreListenerPause = true;

        videoAudioSource.mute = false;


        ApplyVideoVolume();

    }


    // CLEAR REQUEST

    private void ClearRequest()
    {
        StopVideoAndHideMenu();
        RestoreDisabledPlayerControlsIfNeeded();

        isBusy = false;
        videoFinished = false;
        pendingSceneName = null;
        pendingLoadMode = PendingLoadMode.None;
        videoFlowMode = VideoFlowMode.None;
        keepWorldAudioMutedDuringLevelLoad = false;

        RestoreAudioAfterCancelledFlow();
    }


    // VALIDATE

    private void OnValidate()
    {
        videoVolume = Mathf.Clamp01(videoVolume);
        endingMusicVolume = Mathf.Clamp01(endingMusicVolume);

        skipAppearDelay = Mathf.Max(0f, skipAppearDelay);
        continueCountdownDuration = Mathf.Max(0f, continueCountdownDuration);
        continueEndCountdownDuration = Mathf.Max(0f, continueEndCountdownDuration);

        if (string.IsNullOrWhiteSpace(initialLoadedSceneName))
            initialLoadedSceneName = "Level_01";

        if (string.IsNullOrWhiteSpace(watchedKeyPrefix))
            watchedKeyPrefix = "WakeInTheDark.LevelIntroVideoSeen.";


        if (endingScreenRoot != null)
        {
            ResolveEndingPresentationReferences();
        }

    }


    // CLEANUP

    private void OnDestroy()
    {
        StopButtonTimers();
        StopEndingBackgroundVideo();
        StopEndingMusic();

        if (skipButton != null)
            skipButton.onClick.RemoveListener(SkipVideo);

        if (continueButton != null)
            continueButton.onClick.RemoveListener(ContinueToLevel);

        if (continueEndButton != null)
            continueEndButton.onClick.RemoveListener(ContinueAfterGameEnding);

        UnsubscribeVideoEvents();

        if (Instance == this)
            Instance = null;
    }
}