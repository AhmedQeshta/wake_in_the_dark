using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

public class LevelVideoIntroManager : MonoBehaviour
{
  // ==================================================
  // INSTANCE
  // ==================================================

  public static LevelVideoIntroManager Instance { get; private set; }
  private enum PendingLoadMode
  {
    None,
    AlreadyLoadedLevel,
    NormalLevelLoad,
    LevelMenuLoad
  }


  // ==================================================
  // LEVEL VIDEOS
  // ==================================================

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


  // ==================================================
  // UI
  // ==================================================

  [Header("UI")]

  [Tooltip("Root GameObject of LevelVideoIntro_Menu.")]
  [SerializeField]
  private GameObject videoMenuRoot;


  [Tooltip(
      "CanvasGroup on LevelVideoIntro_Menu. " +
      "Used to make the video UI visible/clickable when opened and hidden when closed."
  )]
  [SerializeField]
  private CanvasGroup videoMenuCanvasGroup;


  [Tooltip("RawImage that displays the RenderTexture used by VideoPlayer.")]
  [SerializeField]
  private RawImage videoRawImage;


  [SerializeField]
  private Button skipButton;


  [SerializeField]
  private Button continueButton;


  // ==================================================
  // VIDEO PLAYER
  // ==================================================

  [Header("Video Player")]

  [SerializeField]
  private VideoPlayer videoPlayer;


  [Tooltip(
      "Optional AudioSource used by the VideoPlayer. " +
      "Assign it here only for convenient validation/debugging."
  )]
  [SerializeField]
  private AudioSource videoAudioSource;


  // ==================================================
  // VIDEO AUDIO
  // ==================================================

  [Header("Video Audio")]

  [Tooltip(
      "User volume for all level-intro videos. " +
      "0 = mute, 1 = full volume."
  )]
  [SerializeField, Range(0f, 1f)]
  private float videoVolume =
      1f;


  private const string
      VideoVolumeKey =
          "Settings.LevelIntroVideoVolume";


  // ==================================================
  // LEVEL 1 / ALREADY LOADED LEVEL
  // ==================================================

  [Header("Initial Already-Loaded Level")]

  [Tooltip(
      "Bootstrap already loads this level behind the Start Menu. " +
      "The intro video is shown, then this level continues without loading it again."
  )]
  [SerializeField]
  private string initialLoadedSceneName =
      "Level_01";


  [Tooltip(
      "Called after the initial already-loaded level video is skipped " +
      "or the Continue button is pressed. In the Inspector, connect this " +
      "to the SAME UIManager method that the Start button currently calls."
  )]
  [SerializeField]
  private UnityEvent onAlreadyLoadedLevelReady;


  // ==================================================
  // REPLAY / SAVE
  // ==================================================

  [Header("Replay")]

  [Tooltip(
      "OFF = video is mandatory the first time, but future replays skip it. " +
      "ON = show the video every time the level is entered."
  )]
  [SerializeField]
  private bool showVideoOnReplay = false;


  [Tooltip(
      "PlayerPrefs prefix used to remember that a level intro video " +
      "has already been watched or skipped."
  )]
  [SerializeField]
  private string watchedKeyPrefix =
      "WakeInTheDark.LevelIntroVideoSeen.";


  // ==================================================
  // OPTIONS
  // ==================================================

  [Header("Options")]

  [Tooltip(
      "If a level has no video assigned, continue to the level immediately " +
      "instead of blocking progression."
  )]
  [SerializeField]
  private bool continueIfVideoMissing = true;


  [Tooltip(
      "Disable the currently loaded PlayerMovement while the video is open. " +
      "This stops keyboard input from moving the player behind the full-screen UI."
  )]
  [SerializeField]
  private bool disableCurrentPlayerControlsDuringVideo = true;


  [Tooltip("Useful Console messages while setting the system up.")]
  [SerializeField]
  private bool debugLogs = true;


  // ==================================================
  // REFERENCES
  // ==================================================

  [Header("References")]

  [SerializeField]
  private LevelLoader levelLoader;


  // ==================================================
  // STATE
  // ==================================================

  private PendingLoadMode pendingLoadMode =
      PendingLoadMode.None;


  private string pendingSceneName;


  private bool isBusy;


  private bool videoFinished;


  private PlayerMovement disabledPlayer;


  private bool disabledPlayerHadControls;


  // ==================================================
  // PUBLIC STATE
  // ==================================================

  public bool IsBusy =>
      isBusy;


  public string PendingSceneName =>
      pendingSceneName;


  public float VideoVolume =>
      videoVolume;


  // ==================================================
  // AWAKE
  // ==================================================

  private void Awake()
  {
    if (Instance != null &&
        Instance != this)
    {
      Debug.LogWarning(
          "Duplicate LevelVideoIntroManager found. Removing duplicate.",
          this
      );

      Destroy(gameObject);

      return;
    }


    Instance =
        this;


    ResolveLevelLoader();


    ResolveVideoMenuCanvasGroup();


    videoVolume =
        Mathf.Clamp01(
            PlayerPrefs.GetFloat(
                VideoVolumeKey,
                videoVolume
            )
        );


    ConfigureVideoPlayer();


    ConfigureButtons();


    HideVideoMenuImmediate();
  }


  // ==================================================
  // VIDEO PLAYER EVENTS
  // ==================================================

  private void OnEnable()
  {
    SubscribeVideoEvents();
  }


  private void OnDisable()
  {
    UnsubscribeVideoEvents();
  }


  private void SubscribeVideoEvents()
  {
    if (videoPlayer == null)
      return;


    videoPlayer.prepareCompleted -=
        HandleVideoPrepared;


    videoPlayer.loopPointReached -=
        HandleVideoFinished;


    videoPlayer.errorReceived -=
        HandleVideoError;


    videoPlayer.prepareCompleted +=
        HandleVideoPrepared;


    videoPlayer.loopPointReached +=
        HandleVideoFinished;


    videoPlayer.errorReceived +=
        HandleVideoError;
  }


  private void UnsubscribeVideoEvents()
  {
    if (videoPlayer == null)
      return;


    videoPlayer.prepareCompleted -=
        HandleVideoPrepared;


    videoPlayer.loopPointReached -=
        HandleVideoFinished;


    videoPlayer.errorReceived -=
        HandleVideoError;
  }


  // ==================================================
  // BUTTONS
  // ==================================================

  private void ConfigureButtons()
  {
    if (skipButton != null)
    {
      skipButton.onClick.RemoveListener(
          SkipVideo
      );


      skipButton.onClick.AddListener(
          SkipVideo
      );
    }


    if (continueButton != null)
    {
      continueButton.onClick.RemoveListener(
          ContinueToLevel
      );


      continueButton.onClick.AddListener(
          ContinueToLevel
      );
    }
  }


  // ==================================================
  // PUBLIC REQUEST - INITIAL LEVEL 1
  // ==================================================

  /*
   * Connect the Bootstrap Start button to this method.
   *
   * Do NOT also keep the old Start action directly on the button.
   * Instead connect the old Start action to:
   *
   * On Already Loaded Level Ready
   *
   * in this component.
   */
  public void RequestInitialLoadedLevel()
  {
    if (debugLogs)
    {
      LevelVideoEntry entry =
          FindEntry(
              initialLoadedSceneName
          );


      Debug.Log(
          "LevelVideoIntroManager: Level 1 request received. " +
          "Scene=" + initialLoadedSceneName +
          " | HasEntry=" + (entry != null) +
          " | HasClip=" + (entry != null && entry.videoClip != null) +
          " | Seen=" + HasSeenVideo(initialLoadedSceneName) +
          " | ShowOnReplay=" + showVideoOnReplay,
          this
      );
    }


    RequestVideo(
        initialLoadedSceneName,
        PendingLoadMode.AlreadyLoadedLevel
    );
  }


  // ==================================================
  // PUBLIC REQUEST - NORMAL PROGRESSION
  // ==================================================

  /*
   * Used by LevelExitDoor.
   *
   * Example:
   * Level_01 exit -> RequestLevel("Level_02")
   */
  public void RequestLevel(
      string sceneName)
  {
    RequestVideo(
        sceneName,
        PendingLoadMode.NormalLevelLoad
    );
  }


  // ==================================================
  // PUBLIC REQUEST - LEVELS MENU
  // ==================================================

  /*
   * Used by LevelSelectButton.
   */
  public void RequestLevelFromMenu(
      string sceneName)
  {
    RequestVideo(
        sceneName,
        PendingLoadMode.LevelMenuLoad
    );
  }


  // ==================================================
  // REQUEST
  // ==================================================

  private void RequestVideo(
      string sceneName,
      PendingLoadMode loadMode)
  {
    if (isBusy)
    {
      Debug.LogWarning(
          "LevelVideoIntroManager: A level video request is already active.",
          this
      );

      return;
    }


    if (string.IsNullOrWhiteSpace(
            sceneName))
    {
      Debug.LogError(
          "LevelVideoIntroManager: Scene name is empty.",
          this
      );

      return;
    }


    pendingSceneName =
        sceneName;


    pendingLoadMode =
        loadMode;


    videoFinished =
        false;


    LevelVideoEntry entry =
        FindEntry(
            sceneName
        );


    // ----------------------------------------------
    // REPLAY CAN BYPASS THE VIDEO
    // ----------------------------------------------

    if (!showVideoOnReplay &&
        HasSeenVideo(
            sceneName))
    {
      if (debugLogs)
      {
        Debug.Log(
            "LevelVideoIntroManager: Intro already seen for " +
            sceneName +
            ". Continuing directly.",
            this
        );
      }


      CompleteRequest();

      return;
    }


    // ----------------------------------------------
    // NO ENTRY / NO CLIP
    // ----------------------------------------------

    if (entry == null ||
        entry.videoClip == null)
    {
      string message =
          "LevelVideoIntroManager: No intro video is assigned for '" +
          sceneName +
          "'.";


      if (!continueIfVideoMissing)
      {
        Debug.LogError(
            message +
            " Progression is blocked because Continue If Video Missing is OFF.",
            this
        );


        ClearRequest();

        return;
      }


      Debug.LogWarning(
          message +
          " Continuing directly.",
          this
      );


      CompleteRequest();

      return;
    }


    if (videoPlayer == null)
    {
      Debug.LogError(
          "LevelVideoIntroManager: VideoPlayer is not assigned.",
          this
      );


      if (continueIfVideoMissing)
      {
        CompleteRequest();
      }
      else
      {
        ClearRequest();
      }


      return;
    }


    // ----------------------------------------------
    // OPEN VIDEO
    // ----------------------------------------------

    isBusy =
        true;


    DisableCurrentPlayerControls();


    ShowVideoMenu();


    SetPlayingButtonState();


    videoPlayer.Stop();


    videoPlayer.clip =
        entry.videoClip;


    /*
     * Prepare first so a black/empty first frame is less noticeable.
     * Playback begins in HandleVideoPrepared().
     */
    videoPlayer.Prepare();


    if (debugLogs)
    {
      Debug.Log(
          "LevelVideoIntroManager: Preparing intro for " +
          sceneName +
          ".",
          this
      );
    }
  }


  // ==================================================
  // VIDEO PREPARED
  // ==================================================

  private void HandleVideoPrepared(
      VideoPlayer source)
  {
    if (!isBusy ||
        source != videoPlayer)
    {
      return;
    }


    source.Play();


    if (debugLogs)
    {
      Debug.Log(
          "LevelVideoIntroManager: Playing intro for " +
          pendingSceneName +
          ".",
          this
      );
    }
  }


  // ==================================================
  // VIDEO FINISHED
  // ==================================================

  private void HandleVideoFinished(
      VideoPlayer source)
  {
    if (!isBusy ||
        source != videoPlayer)
    {
      return;
    }


    videoFinished =
        true;


    /*
     * Keep the final frame visible.
     * The player now chooses when to continue.
     */
    if (skipButton != null)
    {
      skipButton.gameObject.SetActive(
          false
      );
    }


    if (continueButton != null)
    {
      continueButton.gameObject.SetActive(
          true
      );


      continueButton.interactable =
          true;
    }


    if (debugLogs)
    {
      Debug.Log(
          "LevelVideoIntroManager: Intro finished for " +
          pendingSceneName +
          ". Waiting for Continue.",
          this
      );
    }
  }


  // ==================================================
  // VIDEO ERROR
  // ==================================================

  private void HandleVideoError(
      VideoPlayer source,
      string message)
  {
    if (!isBusy ||
        source != videoPlayer)
    {
      return;
    }


    Debug.LogError(
        "LevelVideoIntroManager: Video error for '" +
        pendingSceneName +
        "': " +
        message,
        this
    );


    /*
     * Never permanently lock the player because a browser/device
     * cannot play one particular video.
     *
     * Show Continue so progression is still possible.
     */
    videoFinished =
        true;


    if (skipButton != null)
    {
      skipButton.gameObject.SetActive(
          false
      );
    }


    if (continueButton != null)
    {
      continueButton.gameObject.SetActive(
          true
      );


      continueButton.interactable =
          true;
    }
  }


  // ==================================================
  // SKIP
  // ==================================================

  public void SkipVideo()
  {
    if (!isBusy)
      return;


    if (videoPlayer != null)
    {
      videoPlayer.Stop();
    }


    if (debugLogs)
    {
      Debug.Log(
          "LevelVideoIntroManager: Intro skipped for " +
          pendingSceneName +
          ".",
          this
      );
    }


    CompleteRequest();
  }


  // ==================================================
  // CONTINUE
  // ==================================================

  public void ContinueToLevel()
  {
    if (!isBusy)
      return;


    /*
     * Continue is intended only after loopPointReached/error.
     * The Inspector button is hidden while the video is playing,
     * but keep this guard for safety.
     */
    if (!videoFinished)
      return;


    CompleteRequest();
  }


  // ==================================================
  // COMPLETE REQUEST
  // ==================================================

  private void CompleteRequest()
  {
    string sceneName =
        pendingSceneName;


    PendingLoadMode mode =
        pendingLoadMode;


    if (!string.IsNullOrWhiteSpace(
            sceneName))
    {
      MarkVideoSeen(
          sceneName
      );
    }


    if (videoPlayer != null)
    {
      videoPlayer.Stop();
    }


    HideVideoMenuImmediate();


    /*
     * Clear busy state BEFORE invoking/loading.
     * This allows downstream UI/scene systems to make new requests safely.
     */
    isBusy =
        false;


    pendingSceneName =
        null;


    pendingLoadMode =
        PendingLoadMode.None;


    videoFinished =
        false;


    ResolveLevelLoader();


    switch (mode)
    {
      // ------------------------------------------
      // LEVEL 1 IS ALREADY LOADED BY BOOTSTRAP
      // ------------------------------------------

      case PendingLoadMode.AlreadyLoadedLevel:

        RestoreDisabledPlayerControlsIfNeeded();


        if (debugLogs)
        {
          Debug.Log(
              "LevelVideoIntroManager: Initial video complete. " +
              "Continuing already-loaded level.",
              this
          );
        }


        onAlreadyLoadedLevelReady
            ?.Invoke();


        break;


      // ------------------------------------------
      // NORMAL EXIT DOOR PROGRESSION
      // ------------------------------------------

      case PendingLoadMode.NormalLevelLoad:

        if (levelLoader == null)
        {
          Debug.LogError(
              "LevelVideoIntroManager: LevelLoader was not found.",
              this
          );

          RestoreDisabledPlayerControlsIfNeeded();

          return;
        }


        levelLoader.LoadLevel(
            sceneName
        );


        ClearDisabledPlayerReference();


        break;


      // ------------------------------------------
      // LEVELS MENU
      // ------------------------------------------

      case PendingLoadMode.LevelMenuLoad:

        if (levelLoader == null)
        {
          Debug.LogError(
              "LevelVideoIntroManager: LevelLoader was not found.",
              this
          );

          RestoreDisabledPlayerControlsIfNeeded();

          return;
        }


        levelLoader.LoadLevelFromMenu(
            sceneName
        );


        ClearDisabledPlayerReference();


        break;


      default:

        RestoreDisabledPlayerControlsIfNeeded();

        break;
    }
  }


  // ==================================================
  // PLAYER CONTROL DURING VIDEO
  // ==================================================

  private void DisableCurrentPlayerControls()
  {
    ClearDisabledPlayerReference();


    if (!disableCurrentPlayerControlsDuringVideo)
      return;


    disabledPlayer =
        FindAnyObjectByType
            <PlayerMovement>();


    if (disabledPlayer == null)
      return;


    disabledPlayerHadControls =
        disabledPlayer.ControlsEnabled;


    if (disabledPlayerHadControls)
    {
      disabledPlayer.DisableControls();
    }
  }


  private void RestoreDisabledPlayerControlsIfNeeded()
  {
    if (disabledPlayer != null &&
        disabledPlayerHadControls)
    {
      disabledPlayer.EnableControls();
    }


    ClearDisabledPlayerReference();
  }


  private void ClearDisabledPlayerReference()
  {
    disabledPlayer =
        null;


    disabledPlayerHadControls =
        false;
  }


  // ==================================================
  // VIDEO LOOKUP
  // ==================================================

  private LevelVideoEntry FindEntry(
      string sceneName)
  {
    if (levelVideos == null)
      return null;


    foreach (
        LevelVideoEntry entry
        in levelVideos)
    {
      if (entry == null)
        continue;


      if (string.Equals(
              entry.sceneName,
              sceneName,
              StringComparison.OrdinalIgnoreCase))
      {
        return entry;
      }
    }


    return null;
  }


  // ==================================================
  // WATCHED SAVE
  // ==================================================

  public bool HasSeenVideo(
      string sceneName)
  {
    if (string.IsNullOrWhiteSpace(
            sceneName))
    {
      return false;
    }


    return
        PlayerPrefs.GetInt(
            GetWatchedKey(
                sceneName
            ),
            0
        ) == 1;
  }


  private void MarkVideoSeen(
      string sceneName)
  {
    if (string.IsNullOrWhiteSpace(
            sceneName))
    {
      return;
    }


    PlayerPrefs.SetInt(
        GetWatchedKey(
            sceneName
        ),
        1
    );


    PlayerPrefs.Save();
  }


  private string GetWatchedKey(
      string sceneName)
  {
    return
        watchedKeyPrefix +
        sceneName;
  }


  // ==================================================
  // VIDEO AUDIO CONTROL
  // ==================================================

  public void SetVideoVolume(
      float value)
  {
    videoVolume =
        Mathf.Clamp01(
            value
        );


    ApplyVideoVolume();


    PlayerPrefs.SetFloat(
        VideoVolumeKey,
        videoVolume
    );


    PlayerPrefs.Save();
  }


  public void MuteVideoAudio()
  {
    SetVideoVolume(
        0f
    );
  }


  public void SetVideoAudioFullVolume()
  {
    SetVideoVolume(
        1f
    );
  }


  private void ApplyVideoVolume()
  {
    if (videoAudioSource == null)
      return;


    videoAudioSource.volume =
        videoVolume;
  }


  // ==================================================
  // RESET VIDEO HISTORY
  // ==================================================

  public void ResetAllWatchedVideos()
  {
    if (levelVideos != null)
    {
      foreach (
          LevelVideoEntry entry
          in levelVideos)
      {
        if (entry == null ||
            string.IsNullOrWhiteSpace(
                entry.sceneName))
        {
          continue;
        }


        PlayerPrefs.DeleteKey(
            GetWatchedKey(
                entry.sceneName
            )
        );
      }
    }


    PlayerPrefs.Save();


    Debug.Log(
        "LevelVideoIntroManager: Watched-video history reset.",
        this
    );
  }


  [ContextMenu("DEBUG - Reset Watched Videos")]
  private void DebugResetWatchedVideos()
  {
    ResetAllWatchedVideos();
  }


  // ==================================================
  // UI STATE
  // ==================================================

  private void ShowVideoMenu()
  {
    if (videoMenuRoot != null)
    {
      videoMenuRoot.SetActive(
          true
      );
    }


    ResolveVideoMenuCanvasGroup();


    /*
     * IMPORTANT:
     *
     * SetActive(true) is NOT enough when a CanvasGroup
     * has Alpha = 0.
     *
     * The old setup had:
     *
     * Alpha          = 0
     * Interactable   = OFF
     * Blocks Raycasts = OFF
     *
     * which made the VideoPlayer run correctly while
     * the whole UI stayed invisible and unclickable.
     */
    if (videoMenuCanvasGroup != null)
    {
      videoMenuCanvasGroup.alpha =
          1f;


      videoMenuCanvasGroup.interactable =
          true;


      videoMenuCanvasGroup.blocksRaycasts =
          true;
    }


    if (videoRawImage != null)
    {
      videoRawImage.enabled =
          true;
    }


    if (GameCursorManager.Instance != null)
    {
      GameCursorManager.Instance
          .ShowMenuCursor();
    }
  }


  private void SetPlayingButtonState()
  {
    if (skipButton != null)
    {
      skipButton.gameObject.SetActive(
          true
      );


      skipButton.interactable =
          true;
    }


    if (continueButton != null)
    {
      continueButton.gameObject.SetActive(
          false
      );
    }
  }


  private void HideVideoMenuImmediate()
  {
    if (skipButton != null)
    {
      skipButton.gameObject.SetActive(
          false
      );
    }


    if (continueButton != null)
    {
      continueButton.gameObject.SetActive(
          false
      );
    }


    ResolveVideoMenuCanvasGroup();


    if (videoMenuCanvasGroup != null)
    {
      videoMenuCanvasGroup.alpha =
          0f;


      videoMenuCanvasGroup.interactable =
          false;


      videoMenuCanvasGroup.blocksRaycasts =
          false;
    }


    if (videoMenuRoot != null)
    {
      videoMenuRoot.SetActive(
          false
      );
    }
  }


  // ==================================================
  // VIDEO MENU CANVAS GROUP
  // ==================================================

  private void ResolveVideoMenuCanvasGroup()
  {
    if (videoMenuCanvasGroup != null)
      return;


    if (videoMenuRoot == null)
      return;


    videoMenuCanvasGroup =
        videoMenuRoot.GetComponent
            <CanvasGroup>();
  }


  // ==================================================
  // LEVEL LOADER
  // ==================================================

  private void ResolveLevelLoader()
  {
    if (levelLoader != null)
      return;


    levelLoader =
        LevelLoader.Instance;


    if (levelLoader == null)
    {
      levelLoader =
          FindAnyObjectByType
              <LevelLoader>();
    }
  }


  // ==================================================
  // VIDEO PLAYER CONFIGURATION
  // ==================================================

  private void ConfigureVideoPlayer()
  {
    if (videoPlayer == null)
      return;


    videoPlayer.playOnAwake =
        false;


    videoPlayer.isLooping =
        false;


    videoPlayer.waitForFirstFrame =
        true;


    /*
     * The Bootstrap Start Menu pauses gameplay with:
     *
     * Time.timeScale = 0
     *
     * Level 1's intro video must still advance while the
     * game is paused, so VideoPlayer must use unscaled time.
     */
    videoPlayer.timeUpdateMode =
        VideoTimeUpdateMode.UnscaledGameTime;


    /*
     * Menu/game pause can also pause the AudioListener.
     * Intro-video audio should remain audible.
     */
    if (videoAudioSource != null)
    {
      videoAudioSource.playOnAwake =
          false;


      videoAudioSource.loop =
          false;


      videoAudioSource.ignoreListenerPause =
          true;


      ApplyVideoVolume();
    }


    /*
     * Audio output routing itself remains configurable
     * in the Inspector.
     */
  }


  // ==================================================
  // CLEAR REQUEST
  // ==================================================

  private void ClearRequest()
  {
    if (videoPlayer != null)
    {
      videoPlayer.Stop();
    }


    HideVideoMenuImmediate();


    RestoreDisabledPlayerControlsIfNeeded();


    isBusy =
        false;


    videoFinished =
        false;


    pendingSceneName =
        null;


    pendingLoadMode =
        PendingLoadMode.None;
  }


  // ==================================================
  // DEBUG - LEVEL 1 VIDEO
  // ==================================================

  [ContextMenu("DEBUG - Forget Level 1 Intro Video")]
  private void DebugForgetLevelOneVideo()
  {
    if (string.IsNullOrWhiteSpace(
            initialLoadedSceneName))
    {
      return;
    }


    PlayerPrefs.DeleteKey(
        GetWatchedKey(
            initialLoadedSceneName
        )
    );


    PlayerPrefs.Save();


    Debug.Log(
        "LevelVideoIntroManager: Forgot watched state for " +
        initialLoadedSceneName +
        ".",
        this
    );
  }


  // ==================================================
  // VALIDATE
  // ==================================================

  private void OnValidate()
  {
    videoVolume =
        Mathf.Clamp01(
            videoVolume
        );


    if (string.IsNullOrWhiteSpace(
            initialLoadedSceneName))
    {
      initialLoadedSceneName =
          "Level_01";
    }


    if (string.IsNullOrWhiteSpace(
            watchedKeyPrefix))
    {
      watchedKeyPrefix =
          "WakeInTheDark.LevelIntroVideoSeen.";
    }
  }


  // ==================================================
  // CLEANUP
  // ==================================================

  private void OnDestroy()
  {
    if (skipButton != null)
    {
      skipButton.onClick.RemoveListener(
          SkipVideo
      );
    }


    if (continueButton != null)
    {
      continueButton.onClick.RemoveListener(
          ContinueToLevel
      );
    }


    UnsubscribeVideoEvents();


    if (Instance == this)
    {
      Instance =
          null;
    }
  }
}